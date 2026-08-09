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
open Tabularius.Interop
open Tabularius.Resources
open Tabularius.Settings
open Tabularius.ViewModels
open TruePath
open Xunit

type private FakeHledgerApi(result: Result<int, exn>) =
    member val VerifyCallCount = 0 with get, set
    interface IHledgerApi with
        member this.VerifyJournal(_journalPath: AbsolutePath, _cancellationToken: CancellationToken) =
            this.VerifyCallCount <- this.VerifyCallCount + 1
            match result with
            | Ok transactions -> Task.FromResult transactions
            | Error ex -> Task.FromException<int>(ex)

type private FakeWindowService() =
    member val ShowErrorMessageCallCount = 0 with get, set
    interface IWindowService with
        member this.ShowErrorMessage(_, _, _) =
            this.ShowErrorMessageCallCount <- this.ShowErrorMessageCallCount + 1
            Task.CompletedTask
        member _.ShowErrorList _ = ()
        member _.ChooseJournalFile() = Task.FromResult ValueNone

let private createViewModel (windowService: IWindowService) (hledger: IHledgerApi) =
    let errorCollector = ErrorCollector(Lifetime.Eternal, SynchronousScheduler.Instance)
    let config = Configuration.TabulariusConfiguration.Default
    let activityHost = BackgroundActivityHost(SynchronousScheduler.Instance)
    MainViewModel(errorCollector, config, windowService, activityHost, hledger)

[<Fact>]
let ``Existing valid file sets JournalInfo``(): Task = task {
    let path = Temporary.CreateTempFile()
    let windowService = FakeWindowService()
    let hledger = FakeHledgerApi(Ok 42)
    let vm = createViewModel windowService hledger

    do! vm.ReloadFromState({ LastOpenedFile = ValueSome path })

    Assert.Equal(String.Format(Localization.MainWindow_JournalInfo, 42), vm.JournalInfo)
    Assert.Equal(1, hledger.VerifyCallCount)
    Assert.Equal(0, windowService.ShowErrorMessageCallCount)
}

[<Fact>]
let ``No stored path does nothing``(): Task = task {
    let windowService = FakeWindowService()
    let hledger = FakeHledgerApi(Ok 42)
    let vm = createViewModel windowService hledger

    do! vm.ReloadFromState({ LastOpenedFile = ValueNone })

    Assert.Null(vm.JournalInfo)
    Assert.Equal(0, hledger.VerifyCallCount)
    Assert.Equal(0, windowService.ShowErrorMessageCallCount)
}

[<Fact>]
let ``Missing file on disk is silently skipped``(): Task = task {
    let path = Temporary.SystemTempDirectory() / $"nonexistent{Guid.NewGuid()}.journal"
    let windowService = FakeWindowService()
    let hledger = FakeHledgerApi(Ok 42)
    let vm = createViewModel windowService hledger

    do! vm.ReloadFromState({ LastOpenedFile = ValueSome path })

    Assert.Null(vm.JournalInfo)
    Assert.Equal(0, hledger.VerifyCallCount)
    Assert.Equal(0, windowService.ShowErrorMessageCallCount)
}

[<Fact>]
let ``Existing invalid journal shows error message``(): Task = task {
    let path = Temporary.CreateTempFile()
    let windowService = FakeWindowService()
    let hledger = FakeHledgerApi(Error(Exception("invalid journal")))
    let vm = createViewModel windowService hledger

    do! vm.ReloadFromState({ LastOpenedFile = ValueSome path })

    Assert.Null(vm.JournalInfo)
    Assert.Equal(1, hledger.VerifyCallCount)
    Assert.Equal(1, windowService.ShowErrorMessageCallCount)
}
