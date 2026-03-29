// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Platform.Storage
open Tabularius.Resources
open Tabularius.ViewModels
open Tabularius.Views
open TruePath

type WindowService(mainWindow: Window) =
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

        member this.ChooseJournalFile() = task {
            let options = FilePickerOpenOptions(FileTypeFilter = [|
                FilePickerFileType(Localization.FilePicker_JournalFiles, Patterns = [| "*.journal" |])
            |])
            let! files = mainWindow.StorageProvider.OpenFilePickerAsync(options)
            return
                match files.Count with
                | 0 -> ValueNone
                | 1 -> ValueSome(AbsolutePath files[0].Path.LocalPath)
                | _ -> failwithf $"Expected 0 or 1 file, got %d{files.Count}."
        }


