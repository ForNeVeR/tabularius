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

type ExceptionInfo = {
    TypeName: string
    Message: string
    StackTrace: string
    InnerExceptions: ExceptionInfo list
}

module ExceptionInfo =
    let rec fromException (ex: Exception) : ExceptionInfo =
        let inners =
            match ex with
            | :? AggregateException as agg ->
                agg.InnerExceptions |> Seq.map fromException |> Seq.toList
            | _ ->
                ex.InnerException
                |> Option.ofObj
                |> Option.map(fun inner -> [fromException inner])
                |> Option.defaultValue []
        { TypeName = ex.GetType().FullName |> Option.ofObj |> Option.defaultValue (ex.GetType().Name)
          Message = ex.Message
          StackTrace = ex.StackTrace |> Option.ofObj |> Option.defaultValue ""
          InnerExceptions = inners }

    let rec formatAsText (info: ExceptionInfo) : string =
        let header = $"{info.TypeName}: {info.Message}"
        let parts = [
            header
            if not(String.IsNullOrWhiteSpace info.StackTrace) then info.StackTrace
            for i, inner in info.InnerExceptions |> List.indexed do
                let label =
                    if info.InnerExceptions.Length > 1 then $"--- Inner Exception [{i + 1}] ---"
                    else "--- Inner Exception ---"
                label
                formatAsText inner
        ]
        String.concat "\n" parts

type ErrorEntry(message: string, exceptionInfo: ExceptionInfo voption, environmentStackTrace: string, firstOccurrence: DateTimeOffset, scheduler: IScheduler) =
    inherit ObservableObject()

    let mutable count = 1
    let mutable lastOccurrence = firstOccurrence

    member _.Message: string = message
    member _.ExceptionInfo: ExceptionInfo voption = exceptionInfo
    member _.EnvironmentStackTrace: string = environmentStackTrace
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
        let key = struct(entryToAdd.Message, entryToAdd.ExceptionInfo)

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
                let exceptionInfo =
                    logEvent.Exception
                    |> Option.ofObj
                    |> Option.map ExceptionInfo.fromException
                    |> Option.toValueOption
                let environmentStackTrace = Environment.StackTrace

                let entry = ErrorEntry(message, exceptionInfo, environmentStackTrace, logEvent.Timestamp, scheduler)
                processor.Post(Add entry)
