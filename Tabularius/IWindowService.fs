// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius

type IWindowService =
    abstract ShowErrorList: collector: ErrorCollector -> unit

type DesignTimeWindowService() =
    interface IWindowService with
        member _.ShowErrorList(_: ErrorCollector) = ()
