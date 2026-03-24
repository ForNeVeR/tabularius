// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius.ViewModels

open CommunityToolkit.Mvvm.ComponentModel
open Tabularius

type MainViewModel(errorCollector: ErrorCollector, config: Configuration.TabulariusConfiguration, windowService: IWindowService) =
    inherit ObservableObject()

    new() = MainViewModel(ErrorCollector.DesignTime, Configuration.TabulariusConfiguration.Default, DesignTimeWindowService())

    member this.Status = StatusViewModel(errorCollector, config, windowService)
