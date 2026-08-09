// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius

open System
open System.Threading.Tasks
open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Platform.Storage
open MsBox.Avalonia
open MsBox.Avalonia.Enums
open Tabularius.Resources
open Tabularius.Settings
open Tabularius.ViewModels
open Tabularius.Views
open TruePath

type WindowService(mainWindow: Window) =
    interface IWindowService with
        member _.ShowErrorMessage(message, error, additionalText) =
            let errorParagraph =
                error
                |> Option.ofObj
                |> Option.map(fun error ->
                    match error.Message with
                    | m when not <| String.IsNullOrWhiteSpace m -> m.TrimEnd()
                    | _ -> Localization.General_UnknownError
                )

            let parts =
                seq {
                    Some message
                    errorParagraph
                    Option.ofObj additionalText
                } |> Seq.choose id

            let content = String.concat "\n\n" parts
            let box = MessageBoxManager.GetMessageBoxStandard(Localization.General_Error, content, ButtonEnum.Ok, Icon.Error)
            box.ShowWindowDialogAsync(mainWindow)

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
            let! state = State.LoadFromFile()
            let storageProvider = mainWindow.StorageProvider
            let! suggestedLocation, suggestedFileName =
                state.LastOpenedFile
                |> ValueOption.bind(fun location ->
                    location.Parent
                    |> ValueOption.ofNullable
                    |> ValueOption.map(fun folder -> task {
                        let! folder = storageProvider.TryGetFolderFromPathAsync folder.Value
                        return (folder |> ValueOption.ofObj), (ValueSome location.FileName)
                    })
                )
                |> ValueOption.defaultWith(fun() -> Task.FromResult(ValueNone, ValueNone))

            let options = FilePickerOpenOptions(
                FileTypeFilter = [|
                    FilePickerFileType(Localization.FilePicker_JournalFiles, Patterns = [| "*.journal" |])
                |],
                SuggestedStartLocation = (suggestedLocation |> ValueOption.toObj),
                SuggestedFileName = (suggestedFileName |> ValueOption.toObj)
            )

            let! files = storageProvider.OpenFilePickerAsync(options)
            let result =
                match files.Count with
                | 0 -> ValueNone
                | 1 -> ValueSome(AbsolutePath files[0].Path.LocalPath)
                | _ -> failwithf $"Expected 0 or 1 file, got %d{files.Count}."
            match result with
            | ValueNone -> ()
            | ValueSome path ->
                do! State.SaveToFile { state with LastOpenedFile = ValueSome path }
            return result
        }
