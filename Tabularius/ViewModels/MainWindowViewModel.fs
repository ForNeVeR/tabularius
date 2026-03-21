// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius.ViewModels

open CommunityToolkit.Mvvm.ComponentModel
open Tabularius.Resources

type MainWindowViewModel() =
    inherit ObservableObject()
    member this.Greeting = Localization.Welcome
