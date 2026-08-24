// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

module Tabularius.Tests.MainViewModelTests

open System
open System.Threading
open System.Threading.Tasks
open JetBrains.Collections.Viewable
open JetBrains.Lifetimes
open Tabularius
open Tabularius.Data
open Tabularius.Interop
open Tabularius.Settings
open Tabularius.ViewModels
open TruePath
open Xunit

type private FakeHledgerApi(result: Result<BalanceReport, exn>) =
    member val BalanceReportCallCount = 0 with get, set
    interface IHledgerApi with
        member _.VerifyJournal(_journalPath: AbsolutePath, _cancellationToken: CancellationToken) =
            Task.FromResult 0

        member this.BalanceReport(_journalPath: AbsolutePath, _cancellationToken: CancellationToken) =
            this.BalanceReportCallCount <- this.BalanceReportCallCount + 1
            match result with
            | Ok report -> Task.FromResult report
            | Error ex -> Task.FromException<BalanceReport>(ex)

type private FakeWindowService() =
    member val ShowErrorMessageCallCount = 0 with get, set
    interface IWindowService with
        member this.ShowErrorMessage(_, _, _) =
            this.ShowErrorMessageCallCount <- this.ShowErrorMessageCallCount + 1
            Task.CompletedTask
        member _.ShowErrorList _ = ()
        member _.ChooseJournalFile() = Task.FromResult ValueNone

/// Renders the quantity without a decimal separator, to keep the expectations locale-independent.
let private reportItem account indentationSteps commodity quantity = {
    AccountName = account
    IndentationSteps = indentationSteps
    Amount = {
        Entries = [|
            {
                Commodity = commodity
                Value = {
                    Commodity = commodity
                    Quantity = quantity
                    Style = {
                        CommoditySide = Side.R
                        CommoditySpaced = true
                        Precision = Precision 0uy
                    }
                }
            }
        |]
    }
}

let private reportOf items: BalanceReport = {
    Items = items
    Totals = { Entries = Array.empty }
}

let private emptyReport = reportOf Array.empty

let private createViewModel (windowService: IWindowService) (hledger: IHledgerApi) =
    let errorCollector = ErrorCollector(Lifetime.Eternal, SynchronousScheduler.Instance)
    let config = Configuration.TabulariusConfiguration.Default
    let activityHost = BackgroundActivityHost(SynchronousScheduler.Instance)
    MainViewModel(errorCollector, config, windowService, activityHost, hledger)

[<Fact>]
let ``Existing valid file sets JournalInfo``(): Task = task {
    let path = Temporary.CreateTempFile()
    let windowService = FakeWindowService()
    let hledger = FakeHledgerApi(Ok(reportOf [| reportItem "assets:ing" 0 "BTC" 9900m |]))
    let vm = createViewModel windowService hledger

    do! vm.ReloadFromState({ LastOpenedFile = ValueSome path })

    Assert.Equal(" assets:ing 9900BTC", vm.JournalInfo)
    Assert.Equal(1, hledger.BalanceReportCallCount)
    Assert.Equal(0, windowService.ShowErrorMessageCallCount)
}

[<Fact>]
let ``No stored path does nothing``(): Task = task {
    let windowService = FakeWindowService()
    let hledger = FakeHledgerApi(Ok emptyReport)
    let vm = createViewModel windowService hledger

    do! vm.ReloadFromState({ LastOpenedFile = ValueNone })

    Assert.Null(vm.JournalInfo)
    Assert.Equal(0, hledger.BalanceReportCallCount)
    Assert.Equal(0, windowService.ShowErrorMessageCallCount)
}

[<Fact>]
let ``Missing file on disk is silently skipped``(): Task = task {
    let path = Temporary.SystemTempDirectory() / $"nonexistent{Guid.NewGuid()}.journal"
    let windowService = FakeWindowService()
    let hledger = FakeHledgerApi(Ok emptyReport)
    let vm = createViewModel windowService hledger

    do! vm.ReloadFromState({ LastOpenedFile = ValueSome path })

    Assert.Null(vm.JournalInfo)
    Assert.Equal(0, hledger.BalanceReportCallCount)
    Assert.Equal(0, windowService.ShowErrorMessageCallCount)
}

[<Fact>]
let ``LoadJournal renders the balance report into JournalInfo``(): Task = task {
    let path = Temporary.CreateTempFile()
    let windowService = FakeWindowService()
    let report = reportOf [|
        reportItem "assets:ing" 0 "BTC" 9900m
        reportItem "expenses:goods" 1 "BTC" 100m
    |]
    let hledger = FakeHledgerApi(Ok report)
    let vm = createViewModel windowService hledger

    do! vm.LoadJournal path

    Assert.Equal(" assets:ing 9900BTC\n   expenses:goods 100BTC", vm.JournalInfo)
    Assert.Equal(1, hledger.BalanceReportCallCount)
    Assert.Equal(0, windowService.ShowErrorMessageCallCount)
}

[<Fact>]
let ``LoadJournal on an empty report yields an empty JournalInfo``(): Task = task {
    let path = Temporary.CreateTempFile()
    let windowService = FakeWindowService()
    let hledger = FakeHledgerApi(Ok emptyReport)
    let vm = createViewModel windowService hledger

    do! vm.LoadJournal path

    Assert.Equal("", vm.JournalInfo)
    Assert.Equal(1, hledger.BalanceReportCallCount)
    Assert.Equal(0, windowService.ShowErrorMessageCallCount)
}

[<Fact>]
let ``LoadJournal failure keeps JournalInfo empty and reports an error``(): Task = task {
    let path = Temporary.CreateTempFile()
    let windowService = FakeWindowService()
    let hledger = FakeHledgerApi(Error(Exception("cannot read the journal")))
    let vm = createViewModel windowService hledger

    do! vm.LoadJournal path

    Assert.Null vm.JournalInfo
    Assert.Equal(1, hledger.BalanceReportCallCount)
    Assert.Equal(1, windowService.ShowErrorMessageCallCount)
}

[<Fact>]
let ``Existing invalid journal shows error message``(): Task = task {
    let path = Temporary.CreateTempFile()
    let windowService = FakeWindowService()
    let hledger = FakeHledgerApi(Error(Exception("invalid journal")))
    let vm = createViewModel windowService hledger

    do! vm.ReloadFromState({ LastOpenedFile = ValueSome path })

    Assert.Null(vm.JournalInfo)
    Assert.Equal(1, hledger.BalanceReportCallCount)
    Assert.Equal(1, windowService.ShowErrorMessageCallCount)
}
