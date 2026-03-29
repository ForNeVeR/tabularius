// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius.ViewModels

open System
open CommunityToolkit.Mvvm.ComponentModel
open JetBrains.Threading
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
                let! transactions = hledger.VerifyJournal(path, ct)
                this.JournalInfo <- String.Format(Localization.MainWindow_JournalInfo, transactions)
        }).NoAwait()

    member this.JournalInfo
        with get(): string | null = journalInfo
        and set value = this.SetProperty(&journalInfo, value, nameof this.JournalInfo) |> ignore
