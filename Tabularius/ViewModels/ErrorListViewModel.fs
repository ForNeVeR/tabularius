// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius.ViewModels

open System
open System.Collections.ObjectModel
open CommunityToolkit.Mvvm.ComponentModel
open Tabularius

type ErrorListViewModel(errors: ObservableCollection<ErrorEntry>) =
    inherit ObservableObject()

    let mutable selectedError: ErrorEntry voption = ValueNone

    new() = ErrorListViewModel(ErrorCollector.DesignTime.Errors)

    member _.Errors: ObservableCollection<ErrorEntry> = errors

    member this.SelectedError
        with get(): ErrorEntry | null =
            match selectedError with
            | ValueSome e -> e
            | ValueNone -> null
        and set(value: ErrorEntry | null) =
            selectedError <-
                match value with
                | NonNull e -> ValueSome e
                | _ -> ValueNone
            this.OnPropertyChanged(nameof this.SelectedError)
            this.OnPropertyChanged(nameof this.DetailText)

    member _.DetailText: string =
        match selectedError with
        | ValueNone -> ""
        | ValueSome entry ->
            let msg = if String.IsNullOrWhiteSpace(entry.Message) then "" else entry.Message
            let trace = entry.StackTrace
            if String.IsNullOrWhiteSpace(trace) then msg
            else msg + "\n\n" + trace
