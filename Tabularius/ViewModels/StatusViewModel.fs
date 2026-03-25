// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius.ViewModels

open System
open System.Collections.ObjectModel
open System.Threading.Tasks
open CommunityToolkit.Mvvm.ComponentModel
open JetBrains.Threading
open Tabularius

type StatusViewModel(
    errorCollector: ErrorCollector,
    config: Configuration.TabulariusConfiguration,
    windowService: IWindowService,
    activityHost: IBackgroundActivityHost
) as this =
    inherit ObservableObject()

    do errorCollector.Errors.CollectionChanged.Add(fun _ ->
        this.NotifyErrorCountChanged()
    )

    new() = StatusViewModel(ErrorCollector.DesignTime, Configuration.TabulariusConfiguration.Default, DesignTimeWindowService(), DesignTimeBackgroundActivityHost())

    member _.Errors: ObservableCollection<ErrorEntry> = errorCollector.Errors
    member _.ErrorCount: int = errorCollector.Errors.Count
    member _.Activities = activityHost.Activities
    member _.ShowErrorList(): unit = windowService.ShowErrorList(errorCollector)

    member _.IsDiagnosticMode: bool = config.DiagnosticMode
    member _.ThrowError(): unit =
        raise <| Exception("This is an error", InvalidOperationException("Inner exception"))
    member _.SpawnActivity(): unit =
        activityHost.StartActivity(fun progress ct -> task {
            for i in 1..10 do
                do! Task.Delay(1000, ct)
                progress.ReportPercentage(float i * 10.0)
                progress.ReportText(sprintf "Loading %d%%" (i*10))
        }).NoAwait()

    member private this.NotifyErrorCountChanged() =
        this.OnPropertyChanged(nameof this.ErrorCount)
