// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius.ViewModels

open CommunityToolkit.Mvvm.ComponentModel
open Tabularius

type MainViewModel(errorCollector: ErrorCollector) =
    inherit ObservableObject()

    new() = MainViewModel(ErrorCollector.DesignTime)

    member this.Status = StatusViewModel(errorCollector)
