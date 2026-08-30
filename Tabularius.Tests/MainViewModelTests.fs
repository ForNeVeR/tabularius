// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

module Tabularius.Tests.MainViewModelTests

open System
open System.ComponentModel
open System.Threading
open System.Threading.Tasks
open JetBrains.Collections.Viewable
open JetBrains.Lifetimes
open Tabularius
open Tabularius.Data
open Tabularius.Interop
open Tabularius.Settings
open Tabularius.Tests.BalanceReportBuilders
open Tabularius.ViewModels
open TruePath
open Xunit

type private FakeHledgerApi(result: Result<BalanceReport, exn>) =
    member val BalanceReportCallCount = 0 with get, set
    interface IHledgerApi with
        member this.BalanceReport(_journalPath: AbsolutePath, _cancellationToken: CancellationToken) =
            this.BalanceReportCallCount <- this.BalanceReportCallCount + 1
            match result with
            | Ok report -> Task.FromResult report
            | Error ex -> Task.FromException<BalanceReport>(ex)

type private FakeWindowService() =
    member val ShowErrorMessageCallCount = 0 with get, set
    member val ShowAboutCallCount = 0 with get, set
    member val ShutdownCallCount = 0 with get, set
    interface IWindowService with
        member this.ShowErrorMessage(_, _, _) =
            this.ShowErrorMessageCallCount <- this.ShowErrorMessageCallCount + 1
            Task.CompletedTask
        member _.ShowErrorList _ = ()
        member this.ShowAbout() =
            this.ShowAboutCallCount <- this.ShowAboutCallCount + 1
        member _.ChooseJournalFile() = Task.FromResult ValueNone
        member this.Shutdown() =
            this.ShutdownCallCount <- this.ShutdownCallCount + 1

let private createViewModel (windowService: IWindowService) (hledger: IHledgerApi) =
    let errorCollector = ErrorCollector(Lifetime.Eternal, SynchronousScheduler.Instance)
    let config = Configuration.TabulariusConfiguration.Default
    let activityHost = BackgroundActivityHost(SynchronousScheduler.Instance)
    MainViewModel(errorCollector, config, windowService, activityHost, hledger)

[<Fact>]
let ``Existing valid file sets BalanceReport``(): Task = task {
    let path = Temporary.CreateTempFile()
    let windowService = FakeWindowService()
    let hledger = FakeHledgerApi(Ok(reportOf [| reportItem "assets:ing" 0 [| amount "BTC" 9900m |] |]))
    let vm = createViewModel windowService hledger

    do! vm.ReloadFromState({ LastOpenedFile = ValueSome path })

    Assert.NotNull vm.BalanceReport
    Assert.NotEmpty (nonNull vm.BalanceReport).Entries
    Assert.Equal(1, hledger.BalanceReportCallCount)
    Assert.Equal(0, windowService.ShowErrorMessageCallCount)
}

[<Fact>]
let ``No stored path does nothing``(): Task = task {
    let windowService = FakeWindowService()
    let hledger = FakeHledgerApi(Ok emptyReport)
    let vm = createViewModel windowService hledger

    do! vm.ReloadFromState({ LastOpenedFile = ValueNone })

    Assert.Null(vm.BalanceReport)
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

    Assert.Null(vm.BalanceReport)
    Assert.Equal(0, hledger.BalanceReportCallCount)
    Assert.Equal(0, windowService.ShowErrorMessageCallCount)
}

[<Fact>]
let ``LoadJournal on an empty file yields an empty BalanceReport``(): Task = task {
    let path = Temporary.CreateTempFile()
    let windowService = FakeWindowService()
    let hledger = FakeHledgerApi(Ok emptyReport)
    let vm = createViewModel windowService hledger

    do! vm.LoadJournal path

    Assert.NotNull vm.BalanceReport
    Assert.Empty (nonNull vm.BalanceReport).Entries
    Assert.True vm.IsJournalLoaded
    Assert.Equal(1, hledger.BalanceReportCallCount)
    Assert.Equal(0, windowService.ShowErrorMessageCallCount)
}

[<Fact>]
let ``LoadJournal failure keeps BalanceReport unloaded and reports an error``(): Task = task {
    let path = Temporary.CreateTempFile()
    let windowService = FakeWindowService()
    let hledger = FakeHledgerApi(Error(Exception("cannot read the journal")))
    let vm = createViewModel windowService hledger

    do! vm.LoadJournal path

    Assert.Null vm.BalanceReport
    Assert.False vm.IsJournalLoaded
    Assert.Equal(1, hledger.BalanceReportCallCount)
    Assert.Equal(1, windowService.ShowErrorMessageCallCount)
}

[<Fact>]
let ``Exit requests a shutdown from the window service``() =
    let windowService = FakeWindowService()
    let hledger = FakeHledgerApi(Ok emptyReport)
    let vm = createViewModel windowService hledger

    vm.Exit()

    Assert.Equal(1, windowService.ShutdownCallCount)

[<Fact>]
let ``ShowAbout requests the About window from the window service``() =
    let windowService = FakeWindowService()
    let hledger = FakeHledgerApi(Ok emptyReport)
    let vm = createViewModel windowService hledger

    vm.ShowAbout()

    Assert.Equal(1, windowService.ShowAboutCallCount)

[<Fact>]
let ``IsJournalLoaded is false before any journal is loaded``() =
    let windowService = FakeWindowService()
    let hledger = FakeHledgerApi(Ok emptyReport)
    let vm = createViewModel windowService hledger

    Assert.False vm.IsJournalLoaded

[<Fact>]
let ``Setting BalanceReport notifies IsJournalLoaded``(): unit =
    let windowService = FakeWindowService()
    let hledger = FakeHledgerApi(Ok emptyReport)
    let vm = createViewModel windowService hledger
    let changedProperties = ResizeArray<string>()
    (vm :> INotifyPropertyChanged).PropertyChanged.Add(fun args ->
        match args.PropertyName with
        | null -> ()
        | name -> changedProperties.Add name)

    vm.BalanceReport <- BalanceReportViewModel emptyReport

    Assert.Contains(nameof vm.BalanceReport, changedProperties)
    Assert.Contains(nameof vm.IsJournalLoaded, changedProperties)

[<Fact>]
let ``Existing invalid journal shows error message``(): Task = task {
    let path = Temporary.CreateTempFile()
    let windowService = FakeWindowService()
    let hledger = FakeHledgerApi(Error(Exception("invalid journal")))
    let vm = createViewModel windowService hledger

    do! vm.ReloadFromState({ LastOpenedFile = ValueSome path })

    Assert.Null(vm.BalanceReport)
    Assert.Equal(1, hledger.BalanceReportCallCount)
    Assert.Equal(1, windowService.ShowErrorMessageCallCount)
}
