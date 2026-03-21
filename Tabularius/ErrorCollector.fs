// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius

open System
open System.Collections.Concurrent
open System.Collections.ObjectModel
open System.Threading.Channels
open System.Threading.Tasks
open CommunityToolkit.Mvvm.ComponentModel
open JetBrains.Collections.Viewable
open JetBrains.Lifetimes
open Serilog.Core
open Serilog.Events

type ErrorEntry(message: string, stackTrace: string, firstOccurrence: DateTimeOffset) =
    inherit ObservableObject()

    let mutable count = 1
    let mutable lastOccurrence = firstOccurrence

    member _.Message: string = message
    member _.StackTrace: string = stackTrace
    member _.FirstOccurrence: DateTimeOffset = firstOccurrence

    member this.Count
        with get () = count
        and set value =
            count <- value
            this.OnPropertyChanged(nameof this.Count)

    member this.LastOccurrence
        with get () = lastOccurrence
        and set value =
            lastOccurrence <- value
            this.OnPropertyChanged(nameof this.LastOccurrence)

type ErrorCollector(lifetime: Lifetime, scheduler: IScheduler) =
    let errors = Channel.CreateUnbounded<ErrorEntry>()
    let index = ConcurrentDictionary<string, ErrorEntry>()
    let observableErrors = ObservableCollection<ErrorEntry>()

    let addError(entryToAdd: ErrorEntry) =
        let key = ErrorCollector.MakeKey(entryToAdd.Message, entryToAdd.StackTrace)

        let entry = index.GetOrAdd(key, fun _ -> entryToAdd)
        let addedNew = Object.ReferenceEquals(entry, entryToAdd)
        scheduler.InvokeOrQueue(lifetime, fun () ->
            if addedNew then
                observableErrors.Add(entry)
            else
                entry.Count <- entry.Count + 1
                if entryToAdd.LastOccurrence > entry.LastOccurrence then
                    entry.LastOccurrence <- entryToAdd.LastOccurrence
        )

    do (
        lifetime.StartAsync(TaskScheduler.Default, fun() -> task {
            let ct = lifetime.ToCancellationToken()
            while not ct.IsCancellationRequested do
                let! entry = errors.Reader.ReadAsync(ct)
                addError entry

            return ()
        }) |> ignore
    )

    [<ThreadStatic; DefaultValue>]
    static val mutable private processing: bool

    static member private MakeKey(message: string, stackTrace: string) =
        message + "\x00" + stackTrace

    member _.Errors: ObservableCollection<ErrorEntry> = observableErrors

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

                let entry = ErrorEntry(message, stackTrace, logEvent.Timestamp)
                errors.Writer.TryWrite entry |> ignore
