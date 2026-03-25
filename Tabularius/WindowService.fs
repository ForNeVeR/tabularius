// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius

open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Tabularius.ViewModels
open Tabularius.Views

type WindowService() =
    interface IWindowService with
        member _.ShowErrorList(collector: ErrorCollector) =
            Application.Current
            |> Option.ofObj
            |> Option.bind(fun x -> x.ApplicationLifetime |> Option.ofObj)
            |> Option.filter (fun x -> x :? IClassicDesktopStyleApplicationLifetime)
            |> Option.map(fun x -> x :?> IClassicDesktopStyleApplicationLifetime)
            |> Option.bind(fun x -> x.MainWindow |> Option.ofObj)
            |> Option.iter(fun mainWindow ->
                let vm = ErrorListViewModel collector
                let dialog = ErrorListWindow(DataContext = vm)
                dialog.ShowDialog(mainWindow) |> ignore
            )
