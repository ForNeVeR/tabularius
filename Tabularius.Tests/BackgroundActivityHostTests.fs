// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

module BackgroundActivityHostTests

open System
open System.Threading
open System.Threading.Tasks
open JetBrains.Collections.Viewable
open Tabularius
open Xunit

let private createHost () = BackgroundActivityHost(SynchronousScheduler.Instance)

[<Fact>]
let ``StartActivity adds an entry to Activities`` () : Task = task {
    let host = createHost()
    let tcs = TaskCompletionSource<unit>()
    let t = host.StartActivity<unit>(fun _ _ -> tcs.Task)
    Assert.Equal(1, host.Activities.Count)
    tcs.SetResult(())
    do! t
}

[<Fact>]
let ``Entry is removed on successful completion`` () : Task = task {
    let host = createHost()
    do! host.StartActivity<unit>(fun _ _ -> Task.FromResult(()))
    Assert.Equal(0, host.Activities.Count)
}

[<Fact>]
let ``Entry is removed when task throws`` () : Task = task {
    let host = createHost()
    let t = host.StartActivity<unit>(fun _ _ -> task { return raise (Exception("oops")) })
    try
        do! t
    with _ -> ()
    Assert.Equal(0, host.Activities.Count)
}

[<Fact>]
let ``StartActivity propagates exception to caller`` () : Task = task {
    let host = createHost()
    let ex = Exception("from task")
    let! result =
        task {
            try
                do! host.StartActivity<unit>(fun _ _ -> task { return raise ex })
                return false
            with e ->
                return Object.ReferenceEquals(e, ex)
        }
    Assert.True(result)
}

[<Fact>]
let ``StartActivity returns the task result to the caller`` () : Task = task {
    let host = createHost()
    let! result = host.StartActivity<int>(fun _ _ -> Task.FromResult(42))
    Assert.Equal(42, result)
}

[<Fact>]
let ``Entry is removed when task is cancelled via Cancel button`` () : Task = task {
    let host = createHost()
    let started = TaskCompletionSource<unit>()
    let t = host.StartActivity<unit>(fun _ ct -> task {
        started.SetResult(())
        do! Task.Delay(Timeout.Infinite, ct)
    })
    do! started.Task
    Assert.Equal(1, host.Activities.Count)
    host.Activities[0].Cancel()
    try
        do! t
    with :? OperationCanceledException -> ()
    Assert.Equal(0, host.Activities.Count)
}

[<Fact>]
let ``Cancel cancels the activity's CancellationToken`` () : Task = task {
    let host = createHost()
    let mutable wasCancelled = false
    let started = TaskCompletionSource<unit>()
    let t = host.StartActivity<unit>(fun _ ct -> task {
        started.SetResult(())
        do! Task.Delay(Timeout.Infinite, ct)
        wasCancelled <- ct.IsCancellationRequested
    })
    do! started.Task
    host.Activities[0].Cancel()
    try do! t with :? OperationCanceledException -> wasCancelled <- true
    Assert.True wasCancelled
}

[<Fact>]
let ``ReportText updates the entry Text`` () : Task = task {
    let host = createHost()
    do! host.StartActivity<unit>(fun progress _ -> task {
        let entry = host.Activities[0]
        progress.ReportText("Loading...")
        Assert.Equal("Loading...", entry.Text)
    })
}

[<Fact>]
let ``ReportPercentage updates the entry Percentage`` () : Task = task {
    let host = createHost()
    do! host.StartActivity<unit>(fun progress _ -> task {
        let entry = host.Activities[0]
        progress.ReportPercentage(75.0)
        Assert.Equal(75.0, entry.Percentage)
    })
}

[<Fact>]
let ``PercentageText reflects Percentage`` () : Task = task {
    let host = createHost()
    do! host.StartActivity<unit>(fun progress _ -> task {
        let entry = host.Activities[0]
        progress.ReportPercentage(42.0)
        Assert.Equal("42%", entry.PercentageText)
    })
}

[<Fact>]
let ``Multiple concurrent activities are all tracked`` () : Task = task {
    let host = createHost()
    let tcs1 = TaskCompletionSource<unit>()
    let tcs2 = TaskCompletionSource<unit>()
    let t1 = host.StartActivity<unit>(fun _ _ -> tcs1.Task)
    let t2 = host.StartActivity<unit>(fun _ _ -> tcs2.Task)
    Assert.Equal(2, host.Activities.Count)
    tcs1.SetResult(())
    do! t1
    Assert.Equal(1, host.Activities.Count)
    tcs2.SetResult(())
    do! t2
    Assert.Equal(0, host.Activities.Count)
}
