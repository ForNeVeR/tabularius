// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

module Tabularius.ApplicationLifetime

open Avalonia
open Avalonia.Controls.ApplicationLifetimes

let Desktop(): Option<IClassicDesktopStyleApplicationLifetime> =
    Application.Current
    |> Option.ofObj
    |> Option.bind(fun x -> x.ApplicationLifetime |> Option.ofObj)
    |> Option.filter(fun x -> x :? IClassicDesktopStyleApplicationLifetime)
    |> Option.map(fun x -> x :?> IClassicDesktopStyleApplicationLifetime)
