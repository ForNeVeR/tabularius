// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius.ViewModels

open System
open System.Collections.ObjectModel
open CommunityToolkit.Mvvm.ComponentModel
open Tabularius

type StatusViewModel(errorCollector: ErrorCollector, config: Configuration.TabulariusConfiguration, windowService: IWindowService, activityHost: IBackgroundActivityHost) as this =
    inherit ObservableObject()

    do errorCollector.Errors.CollectionChanged.Add(fun _ ->
        this.NotifyErrorCountChanged()
    )

    new() = StatusViewModel(ErrorCollector.DesignTime, Configuration.TabulariusConfiguration.Default, DesignTimeWindowService(), DesignTimeBackgroundActivityHost())

    member _.IsErrorDiagnosticMode: bool = config.ErrorDiagnosticMode
    member _.Errors: ObservableCollection<ErrorEntry> = errorCollector.Errors
    member _.ErrorCount: int = errorCollector.Errors.Count
    member _.Activities = activityHost.Activities
    member _.ThrowError(): unit =
        raise <| Exception("This is an error", InvalidOperationException("Inner exception"))
    member _.ShowErrorList(): unit = windowService.ShowErrorList(errorCollector)

    member private this.NotifyErrorCountChanged() =
        this.OnPropertyChanged(nameof this.ErrorCount)
