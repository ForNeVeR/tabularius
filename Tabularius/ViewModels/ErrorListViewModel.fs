// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius.ViewModels

open System
open System.Collections.ObjectModel
open CommunityToolkit.Mvvm.ComponentModel
open Tabularius
open Tabularius.Resources

type ErrorListViewModel(errorCollector: ErrorCollector) =
    inherit ObservableObject()

    let mutable selectedError: ErrorEntry voption =
        errorCollector.Errors |> Seq.tryHead |> Option.toValueOption

    new() = ErrorListViewModel(ErrorCollector.DesignTime)

    member _.Errors: ObservableCollection<ErrorEntry> = errorCollector.Errors
    member _.ClearErrors(): unit = errorCollector.Clear()

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
            let header =
                $"{Localization.ErrorList_Occurrences} {entry.Count}\n{Localization.ErrorList_LastOccurrence} {entry.LastOccurrence}"
            let msg = if String.IsNullOrWhiteSpace(entry.Message) then "" else entry.Message
            let body =
                match entry.ExceptionInfo with
                | ValueSome info -> msg + "\n\n" + ExceptionInfo.formatAsText info
                | ValueNone ->
                    if String.IsNullOrWhiteSpace(entry.EnvironmentStackTrace) then msg
                    else msg + "\n\n" + entry.EnvironmentStackTrace
            header + "\n\n" + body
