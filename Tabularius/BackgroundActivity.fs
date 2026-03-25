// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius

open System.Collections.ObjectModel
open System.Threading
open System.Threading.Tasks
open CommunityToolkit.Mvvm.ComponentModel
open JetBrains.Collections.Viewable

type IActivityProgress =
    abstract ReportText: text: string -> unit
    abstract ReportPercentage: percentage: float -> unit

type BackgroundActivityEntry(scheduler: IScheduler, cancelAction: unit -> unit) =
    inherit ObservableObject()

    let mutable text = ""
    let mutable percentage = 0.0

    member this.Text
        with get() =
            scheduler.AssertThread()
            text
        and set(value) =
            scheduler.AssertThread()
            text <- value
            this.OnPropertyChanged(nameof this.Text)

    member this.Percentage
        with get() =
            scheduler.AssertThread()
            percentage
        and set(value) =
            scheduler.AssertThread()
            percentage <- value
            this.OnPropertyChanged(nameof this.Percentage)
            this.OnPropertyChanged(nameof this.PercentageText)

    member this.PercentageText =
        scheduler.AssertThread()
        $"%.0f{percentage}%%"

    member _.Cancel() = cancelAction()

    interface IActivityProgress with
        member this.ReportText(t) =
            if scheduler.IsActive then this.Text <- t
            else scheduler.Queue(fun () -> this.Text <- t)

        member this.ReportPercentage(p) =
            if scheduler.IsActive then this.Percentage <- p
            else scheduler.Queue(fun () -> this.Percentage <- p)

type IBackgroundActivityHost =
    abstract Activities: ObservableCollection<BackgroundActivityEntry>
    abstract StartActivity<'t> : activity: (IActivityProgress -> CancellationToken -> Task<'t>) -> Task<'t>

type DesignTimeBackgroundActivityHost() =
    let activities = ObservableCollection<BackgroundActivityEntry>()

    interface IBackgroundActivityHost with
        member _.Activities = activities
        member _.StartActivity<'t>(_) = Task.FromResult(Unchecked.defaultof<'t>)
