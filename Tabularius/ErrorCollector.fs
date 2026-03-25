// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius

open System
open System.Collections.Generic
open System.Collections.ObjectModel
open System.Threading.Tasks
open CommunityToolkit.Mvvm.ComponentModel
open JetBrains.Collections.Viewable
open JetBrains.Lifetimes
open Serilog.Core
open Serilog.Events

type ErrorEntry(message: string, stackTrace: string, firstOccurrence: DateTimeOffset, scheduler: IScheduler) =
    inherit ObservableObject()

    let mutable count = 1
    let mutable lastOccurrence = firstOccurrence

    member _.Message: string = message
    member _.StackTrace: string = stackTrace
    member _.FirstOccurrence: DateTimeOffset = firstOccurrence

    member this.Count
        with get () =
            scheduler.AssertThread()
            count
        and set value =
            scheduler.AssertThread()
            count <- value
            this.OnPropertyChanged(nameof this.Count)

    member this.LastOccurrence
        with get () =
            scheduler.AssertThread()
            lastOccurrence
        and set value =
            scheduler.AssertThread()
            lastOccurrence <- value
            this.OnPropertyChanged(nameof this.LastOccurrence)

type private Message =
    | Add of ErrorEntry
    | Clear
    | WaitForSettle of AsyncReplyChannel<unit>

type ErrorCollector(lifetime: Lifetime, scheduler: IScheduler) =
    // Only accessed in the processor flow:
    let index = Dictionary<_, ErrorEntry>()
    // Only accessed from the scheduler:
    let observableErrors = ObservableCollection<ErrorEntry>()

    let scheduleOnUiThread(ct, action) =
        Task.Factory.StartNew(
            action = Action(action),
            cancellationToken = ct,
            creationOptions = TaskCreationOptions.RunContinuationsAsynchronously,
            scheduler = scheduler.AsTaskScheduler()
        )

    let clearErrors ct =
        index.Clear()
        scheduleOnUiThread(ct, observableErrors.Clear)

    let addError(entryToAdd: ErrorEntry, ct) =
        let key = struct(entryToAdd.Message, entryToAdd.StackTrace)

        let entry, addedNew =
            match index.TryGetValue key with
            | true, entry -> entry, false
            | false, _ ->
                index.Add(key, entryToAdd)
                entryToAdd, true

        scheduleOnUiThread(ct, fun () ->
            if addedNew then
                observableErrors.Add(entry)
            else
                entry.Count <- entry.Count + 1
                if entryToAdd.LastOccurrence > entry.LastOccurrence then
                    entry.LastOccurrence <- entryToAdd.LastOccurrence
        )

    let processor = MailboxProcessor.Start(
        body = (fun inbox ->
            async {
                let! ct = Async.CancellationToken
                while lifetime.IsAlive do
                    match! inbox.Receive() with
                    | Add error ->
                        do! Async.AwaitTask(addError(error, ct))
                    | Clear ->
                        do! Async.AwaitTask(clearErrors(ct))
                    | WaitForSettle replyChannel ->
                        replyChannel.Reply()
            }),
        cancellationToken = lifetime.ToCancellationToken()
    )

    member _.Errors: ObservableCollection<ErrorEntry> = observableErrors

    member _.Clear(): unit = processor.Post(Clear)

    member _.WaitForSettle(): Task =
        processor.PostAndAsyncReply WaitForSettle
        |> Async.StartAsTask
        :> Task

    static member DesignTime: ErrorCollector = ErrorCollector(Lifetime.Eternal, SynchronousScheduler.Instance)

    interface ILogEventSink with
        member this.Emit(logEvent: LogEvent) =
            if logEvent.Level < LogEventLevel.Error then
                ()
            else
                let message = logEvent.RenderMessage()
                let stackTrace =
                    logEvent.Exception
                    |> Option.ofObj
                    |> Option.map _.StackTrace
                    |> Option.bind Option.ofObj
                    |> Option.defaultWith(fun() -> Environment.StackTrace)

                let entry = ErrorEntry(message, stackTrace, logEvent.Timestamp, scheduler)
                processor.Post(Add entry)
