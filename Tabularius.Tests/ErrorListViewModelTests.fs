// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

module ErrorListViewModelTests

open System
open System.Threading.Tasks
open JetBrains.Collections.Viewable
open JetBrains.Lifetimes
open Serilog.Core
open Serilog.Events
open Serilog.Parsing
open Tabularius
open Tabularius.ViewModels
open Xunit

let private createCollector () = ErrorCollector(Lifetime.Eternal, SynchronousScheduler.Instance)

[<Fact>]
let ``DetailText shows exception info when present``(): Task = task {
    let collector = createCollector()
    ErrorCollectorTests.addErrorWithException(collector, "Something failed", Exception("boom"))
    do! collector.WaitForSettle()
    let vm = ErrorListViewModel(collector)
    let detail = vm.DetailText
    Assert.Contains("Something failed", detail)
    Assert.Contains("System.Exception", detail)
    Assert.Contains("boom", detail)
}

[<Fact>]
let ``DetailText shows nested exception``(): Task = task {
    let collector = createCollector()
    let inner = InvalidOperationException("inner problem")
    let outer = Exception("outer problem", inner)
    ErrorCollectorTests.addErrorWithException(collector, "Error occurred", outer)
    do! collector.WaitForSettle()
    let vm = ErrorListViewModel(collector)
    let detail = vm.DetailText
    Assert.Contains("outer problem", detail)
    Assert.Contains("--- Inner Exception ---", detail)
    Assert.Contains("inner problem", detail)
    Assert.Contains("System.InvalidOperationException", detail)
}

[<Fact>]
let ``DetailText shows environment stack trace when no exception``(): Task = task {
    let collector = createCollector()
    let parser = MessageTemplateParser()
    (collector :> ILogEventSink).Emit(
        LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null, parser.Parse("Log message without exception"), Array.empty)
    )
    do! collector.WaitForSettle()
    let vm = ErrorListViewModel(collector)
    let detail = vm.DetailText
    Assert.Contains("Log message without exception", detail)
    // No exception info, so environment stack trace should be shown
    Assert.True(detail.Length > "Log message without exception".Length)
}
