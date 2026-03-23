// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius.ViewModels

open System
open System.Collections.ObjectModel
open CommunityToolkit.Mvvm.ComponentModel
open Tabularius

type StatusViewModel(errorCollector: ErrorCollector, config: Configuration.TabulariusConfiguration) as this =
    inherit ObservableObject()

    do errorCollector.Errors.CollectionChanged.Add(fun _ ->
        this.NotifyErrorCountChanged()
    )

    new() = StatusViewModel(ErrorCollector.DesignTime, Configuration.TabulariusConfiguration.Default)

    member _.IsErrorDiagnosticMode: bool = config.ErrorDiagnosticMode
    member _.Errors: ObservableCollection<ErrorEntry> = errorCollector.Errors
    member _.ErrorCount: int = errorCollector.Errors.Count
    member _.ThrowError(): unit = raise <| Exception("This is an error")

    member private this.NotifyErrorCountChanged() =
        this.OnPropertyChanged(nameof this.ErrorCount)
