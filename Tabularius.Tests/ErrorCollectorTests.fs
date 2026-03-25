// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

module ErrorCollectorTests

open System
open System.Threading.Tasks
open JetBrains.Collections.Viewable
open JetBrains.Lifetimes
open Serilog.Core
open Serilog.Events
open Serilog.Parsing
open Tabularius
open Xunit

let private createCollector () = ErrorCollector(Lifetime.Eternal, SynchronousScheduler.Instance)

let private parser = MessageTemplateParser()

let addError(collector: ErrorCollector, message: string, stackTrace: string option, time: DateTimeOffset) =
    let ex = {
        new Exception() with
            member _.StackTrace = defaultArg stackTrace ""
    }
    (collector :> ILogEventSink).Emit(
        LogEvent(time, LogEventLevel.Error, ex, parser.Parse(message), Array.empty)
    )

let addErrorNow(collector: ErrorCollector, message: string, stackTrace: string option) =
    addError(collector, message, stackTrace, DateTimeOffset.UtcNow)

let addErrorWithException(collector: ErrorCollector, message: string, ex: Exception) =
    (collector :> ILogEventSink).Emit(
        LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, ex, parser.Parse(message), Array.empty)
    )

[<Fact>]
let ``AddError creates a new entry for a unique error``(): Task = task {
    let collector = createCollector()
    addErrorNow(collector, "Something failed", None)
    do! collector.WaitForSettle()
    Assert.Equal(1, collector.Errors.Count)
    Assert.Equal("Something failed", collector.Errors[0].Message)
    Assert.Equal(1, collector.Errors[0].Count)
}

[<Fact>]
let ``AddError deduplicates errors with same message and stack trace``(): Task = task {
    let collector = createCollector ()
    addErrorNow(collector, "Something failed", Some "at Foo.Bar()")
    addErrorNow(collector, "Something failed", Some "at Foo.Bar()")
    do! collector.WaitForSettle()
    Assert.Equal(2, collector.Errors[0].Count)
}

[<Fact>]
let ``AddError does not deduplicate errors with different messages``(): Task = task {
    let collector = createCollector ()
    addErrorNow(collector, "Error A", None)
    addErrorNow(collector, "Error B", None)
    do! collector.WaitForSettle()
    Assert.Equal(2, collector.Errors.Count)
}

[<Fact>]
let ``AddError does not deduplicate errors with same message but different stack traces``(): Task = task {
    let collector = createCollector ()
    addErrorNow(collector, "Something failed", Some "at Foo.Bar()")
    addErrorNow(collector, "Something failed", Some "at Baz.Qux()")
    do! collector.WaitForSettle()
    Assert.Equal(2, collector.Errors.Count)
}

[<Fact>]
let ``AddError does not deduplicate errors with same stack trace but different messages``(): Task = task {
    let collector = createCollector ()
    addErrorNow(collector, "Error A", Some "at Foo.Bar()")
    addErrorNow(collector, "Error B", Some "at Foo.Bar()")
    do! collector.WaitForSettle()
    Assert.Equal(2, collector.Errors.Count)
}

[<Fact>]
let ``AddError updates LastOccurrence on duplicate``(): Task = task {
    let collector = createCollector ()
    let t1 = DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
    let t2 = DateTimeOffset(2026, 1, 1, 0, 1, 0, TimeSpan.Zero)
    addError(collector, "Error", None, t1)
    addError(collector, "Error", None, t2)
    do! collector.WaitForSettle()
    Assert.Equal(t1, collector.Errors[0].FirstOccurrence)
    Assert.Equal(t2, collector.Errors[0].LastOccurrence)
}

[<Fact>]
let ``AddError preserves FirstOccurrence on duplicate``(): Task = task {
    let collector = createCollector ()
    let t1 = DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
    let t2 = DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero)
    addError(collector, "Error", None, t1)
    addError(collector, "Error", None, t2)
    do! collector.WaitForSettle()
    Assert.Equal(t1, collector.Errors[0].FirstOccurrence)
}

[<Fact>]
let ``AddError deduplicates no-exception errors correctly``(): Task = task {
    let collector = createCollector ()
    addErrorNow(collector, "Timeout", None)
    addErrorNow(collector, "Timeout", None)
    addErrorNow(collector, "Timeout", None)
    do! collector.WaitForSettle()
    Assert.Equal(3, collector.Errors[0].Count)
}

[<Fact>]
let ``Emit via ILogEventSink collects Error-level events``(): Task = task {
    let collector = createCollector ()
    let sink = collector :> ILogEventSink
    let logEvent =
        LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Error,
            null,
            parser.Parse("Test error"),
            Array.empty)
    sink.Emit(logEvent)
    do! collector.WaitForSettle()
    Assert.Equal(1, collector.Errors.Count)
    Assert.Equal("Test error", collector.Errors[0].Message)
}

[<Fact>]
let ``Emit via ILogEventSink ignores events below Error level``(): Task = task {
    let collector = createCollector ()
    let sink = collector :> ILogEventSink
    let warningEvent =
        LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Warning,
            null,
            parser.Parse("Just a warning"),
            Array.empty)
    sink.Emit(warningEvent)
    do! collector.WaitForSettle()
    Assert.Equal(0, collector.Errors.Count)
}

[<Fact>]
let ``Clear removes all errors``(): Task = task {
    let collector = createCollector()
    addErrorNow(collector, "Error A", None)
    addErrorNow(collector, "Error B", Some "at Foo.Bar()")
    do! collector.WaitForSettle()
    Assert.Equal(2, collector.Errors.Count)
    collector.Clear()
    do! collector.WaitForSettle()
    Assert.Equal(0, collector.Errors.Count)
}

[<Fact>]
let ``Clear resets deduplication index``(): Task = task {
    let collector = createCollector()
    addErrorNow(collector, "Same error", Some "at Foo.Bar()")
    do! collector.WaitForSettle()
    Assert.Equal(1, collector.Errors.Count)
    Assert.Equal(1, collector.Errors[0].Count)
    collector.Clear()
    do! collector.WaitForSettle()
    addErrorNow(collector, "Same error", Some "at Foo.Bar()")
    do! collector.WaitForSettle()
    Assert.Equal(1, collector.Errors.Count)
    Assert.Equal(1, collector.Errors[0].Count)
}

[<Fact>]
let ``Emit deduplicates same error logged twice``(): Task = task {
    let collector = createCollector ()
    let sink = collector :> ILogEventSink
    let template = parser.Parse("Repeated error")
    let ex = {
        new Exception("boom") with
            member _.StackTrace = "at Foo.Bar()"
    }
    let event1 =
        LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Error,
            ex,
            template,
            Array.empty)
    let event2 =
        LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Error,
            ex,
            template,
            Array.empty)
    sink.Emit(event1)
    sink.Emit(event2)
    do! collector.WaitForSettle()
    Assert.Equal(1, collector.Errors.Count)
    Assert.Equal(2, collector.Errors[0].Count)
}
