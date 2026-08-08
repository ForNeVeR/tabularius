// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius.Settings

open System.Text.Json
open System.Threading.Tasks
open FVNever.AppDirs
open JetBrains.Diagnostics
open TruePath
open TruePath.SystemIo

[<CLIMutable>]
type InternalState = {
    LastOpenedFile: string | null
}

type State =
    { LastOpenedFile: Option<AbsolutePath> }

    static member internal DefaultStateFilePath: AbsolutePath =
        ApplicationDirectories(applicationName = "Tabularius",
                               vendorName = "me.fornever",
                               allowCompatMode = true).StateDirectory() / "state.json"

    static member val private JsonOptions =
        JsonSerializerOptions(
            WriteIndented = true
        )

    static member internal LoadFromFile(path: AbsolutePath): Task<State> =
        task {
            try
                if not <| path.ExistsFile() then
                    return { LastOpenedFile = None }
                else
                    use stream = path.OpenRead()
                    let! dto = JsonSerializer.DeserializeAsync<InternalState>(stream, State.JsonOptions)
                    let lastOpenedFile =
                        dto
                        |> Option.ofObj
                        |> Option.map _.LastOpenedFile
                        |> Option.bind Option.ofObj
                        |> Option.map AbsolutePath
                    return { LastOpenedFile = lastOpenedFile }
            with
            | ex ->
                Log.Root.Error ex
                return { LastOpenedFile = None }
        }

    static member internal SaveToFile(state: State, path: AbsolutePath): Task =
        task {
            let dto: InternalState = {
                LastOpenedFile = state.LastOpenedFile |> Option.map _.Value |> Option.toObj
            }

            path.Parent.Value.CreateDirectory()
            use stream = path.OpenWrite()
            do! JsonSerializer.SerializeAsync(stream, dto, State.JsonOptions)
        }

    static member LoadFromFile(): Task<State> =
        State.LoadFromFile(State.DefaultStateFilePath)

    static member SaveToFile(state: State): Task =
        State.SaveToFile(state, State.DefaultStateFilePath)
