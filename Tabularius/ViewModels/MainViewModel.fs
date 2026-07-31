// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius.ViewModels

open System
open CommunityToolkit.Mvvm.ComponentModel
open JetBrains.Threading
open Serilog
open Tabularius
open Tabularius.DesignTime
open Tabularius.Interop
open Tabularius.Resources

type MainViewModel(
    errorCollector: ErrorCollector,
    config: Configuration.TabulariusConfiguration,
    windowService: IWindowService,
    activityHost: IBackgroundActivityHost,
    hledger: IHledgerApi
) =
    inherit ObservableObject()

    let mutable journalInfo: string | null = null

    new() = MainViewModel(
        ErrorCollector.DesignTime,
        Configuration.TabulariusConfiguration.Default,
        DesignTimeWindowService(),
        DesignTimeBackgroundActivityHost(),
        HledgerDesignTimeApi()
    )

    member _.Status = StatusViewModel(errorCollector, config, windowService, activityHost)

    member this.OpenJournal(): unit =
        activityHost.StartActivity(fun progress ct -> task {
            progress.ReportText(Localization.Status_LoadingJournal)
            match! windowService.ChooseJournalFile() with
            | ValueNone -> ()
            | ValueSome path ->
                try
                    let! transactions = hledger.VerifyJournal(path, ct)
                    this.JournalInfo <- String.Format(Localization.MainWindow_JournalInfo, transactions)
                with
                | ex ->
                    Log.Logger.Error(ex, "Cannot load journal from file {Path}.", path.Value)
                    do! windowService.ShowErrorMessage(
                        String.Format(Localization.MainWindow_CannotLoadJournal, path.FileName),
                        ex,
                        Localization.General_SeeErrorList
                    )
        }).NoAwait()

    member this.JournalInfo
        with get(): string | null = journalInfo
        and set value = this.SetProperty(&journalInfo, value, nameof this.JournalInfo) |> ignore
