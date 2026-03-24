// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius.Views

open Avalonia.Controls
open Avalonia.Input
open Avalonia.Markup.Xaml
open Tabularius.ViewModels

type ErrorCounterIndicator() as this =
    inherit UserControl()

    do
        this.InitializeComponent()
        this.Tapped.Add(this.OnTapped)

    member private this.OnTapped(_: TappedEventArgs) =
        match this.DataContext with
        | :? StatusViewModel as vm when vm.ErrorCount > 0 ->
            match TopLevel.GetTopLevel(this) with
            | :? Window as parentWindow ->
                let dialog = ErrorListWindow(DataContext = ErrorListViewModel(vm.Errors))
                dialog.ShowDialog(parentWindow) |> ignore
            | _ -> ()
        | _ -> ()

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)
