// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius

open System.Threading.Tasks
open TruePath

type IWindowService =
    abstract ShowErrorList: collector: ErrorCollector -> unit
    abstract ChooseJournalFile: unit -> Task<ValueOption<AbsolutePath>>

type DesignTimeWindowService() =
    interface IWindowService with
        member _.ShowErrorList(_: ErrorCollector) = ()
        member _.ChooseJournalFile(): Task<ValueOption<AbsolutePath>> = Task.FromResult ValueNone
