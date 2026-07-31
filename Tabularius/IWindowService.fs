// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius

open System
open System.Threading.Tasks
open TruePath

type IWindowService =
    abstract ShowErrorMessage: message: string * error: Exception | null * additionalText: string | null -> Task
    abstract ShowErrorList: collector: ErrorCollector -> unit
    abstract ChooseJournalFile: unit -> Task<ValueOption<AbsolutePath>>

type DesignTimeWindowService() =
    interface IWindowService with
        member this.ShowErrorMessage(_, _, _) = Task.CompletedTask
        member _.ShowErrorList _ = ()
        member _.ChooseJournalFile() = Task.FromResult ValueNone
