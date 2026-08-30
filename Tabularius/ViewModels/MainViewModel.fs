// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius.ViewModels

open System
open System.Threading.Tasks
open CommunityToolkit.Mvvm.ComponentModel
open JetBrains.Threading
open Serilog
open Tabularius
open Tabularius.DesignTime
open Tabularius.Interop
open Tabularius.Resources
open Tabularius.Settings
open TruePath
open TruePath.SystemIo

type MainViewModel(
    errorCollector: ErrorCollector,
    config: Configuration.TabulariusConfiguration,
    windowService: IWindowService,
    activityHost: IBackgroundActivityHost,
    hledger: IHledgerApi
) =
    inherit ObservableObject()

    let mutable balanceReport: BalanceReportViewModel | null = null

    new() = MainViewModel(
        ErrorCollector.DesignTime,
        Configuration.TabulariusConfiguration.Default,
        DesignTimeWindowService(),
        DesignTimeBackgroundActivityHost(),
        HledgerDesignTimeApi()
    )

    member _.Status = StatusViewModel(errorCollector, config, windowService, activityHost)

    member internal this.LoadJournal(path: AbsolutePath): Task =
        activityHost.StartActivity(fun progress ct -> task {
            progress.ReportText(Localization.Status_LoadingJournal)
            try
                let! balanceReport = hledger.BalanceReport(path, ct)
                this.BalanceReport <- BalanceReportViewModel balanceReport
            with
            | ex ->
                Log.Logger.Error(ex, "Cannot load journal from file {Path}.", path.Value)
                do! windowService.ShowErrorMessage(
                    String.Format(Localization.MainWindow_CannotLoadJournal, path.FileName),
                    ex,
                    Localization.General_SeeErrorList
                )
        })

    member this.OpenJournal(): unit =
        (task {
            match! windowService.ChooseJournalFile() with
            | ValueNone -> ()
            | ValueSome path -> do! this.LoadJournal(path)
        }).NoAwait()

    member _.Exit(): unit = windowService.Shutdown()

    member internal this.ReloadFromState(state: State): Task =
        task {
            match state.LastOpenedFile with
            | ValueNone -> ()
            | ValueSome path when not (path.ExistsFile()) ->
                Log.Logger.Information(
                    "Last opened file {Path} no longer exists; skipping reload.", path.Value)
            | ValueSome path -> do! this.LoadJournal(path)
        }

    member this.ReloadLastOpenedFile(): unit =
        (task {
            let! state = State.LoadFromFile config.StatePath
            do! this.ReloadFromState(state)
        }).NoAwait()

    member this.BalanceReport
        with get(): BalanceReportViewModel | null = balanceReport
        and set value =
            if this.SetProperty(&balanceReport, value, nameof this.BalanceReport) then
                this.OnPropertyChanged(nameof this.IsJournalLoaded)

    member _.IsJournalLoaded: bool = not (isNull balanceReport)
