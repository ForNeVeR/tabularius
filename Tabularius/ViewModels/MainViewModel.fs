// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius.ViewModels

open CommunityToolkit.Mvvm.ComponentModel
open Tabularius

type MainViewModel(errorCollector: ErrorCollector, config: Configuration.TabulariusConfiguration, windowService: IWindowService, activityHost: IBackgroundActivityHost) =
    inherit ObservableObject()

    new() = MainViewModel(ErrorCollector.DesignTime, Configuration.TabulariusConfiguration.Default, DesignTimeWindowService(), DesignTimeBackgroundActivityHost())

    member this.Status = StatusViewModel(errorCollector, config, windowService, activityHost)
