// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius

open System
open System.Collections.ObjectModel
open System.Threading
open System.Threading.Tasks
open JetBrains.Collections.Viewable

type BackgroundActivityHost(scheduler: IScheduler) =
    let activities = ObservableCollection<BackgroundActivityEntry>()

    let scheduleOnUiThread action =
        Task.Factory.StartNew(
            action = Action(action),
            cancellationToken = CancellationToken.None,
            creationOptions = TaskCreationOptions.RunContinuationsAsynchronously,
            scheduler = scheduler.AsTaskScheduler()
        )

    member _.Activities: ObservableCollection<BackgroundActivityEntry> = activities

    member _.StartActivity<'t>(activity: IActivityProgress -> CancellationToken -> Task<'t>) : Task<'t> =
        let cts = new CancellationTokenSource()
        let entry = BackgroundActivityEntry(scheduler, cts.Cancel)
        task {
            do! scheduleOnUiThread(fun () -> activities.Add(entry))
            let mutable exn: exn option = None
            let mutable result = Unchecked.defaultof<'t>
            try
                let! r = activity (entry :> IActivityProgress) cts.Token
                result <- r
            with ex ->
                exn <- Some ex
            do! scheduleOnUiThread(fun () -> activities.Remove(entry) |> ignore)
            cts.Dispose()
            return
                match exn with
                | Some ex -> raise ex
                | None -> result
        }

    interface IBackgroundActivityHost with
        member this.Activities = this.Activities
        member this.StartActivity<'t>(activity) = this.StartActivity<'t>(activity)
