// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius

open System.Collections.ObjectModel

type IWindowService =
    abstract ShowErrorList: errors: ObservableCollection<ErrorEntry> -> unit

type DesignTimeWindowService() =
    interface IWindowService with
        member _.ShowErrorList(_: ObservableCollection<ErrorEntry>) = ()
