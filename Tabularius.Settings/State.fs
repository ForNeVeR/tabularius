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
    { LastOpenedFile: ValueOption<AbsolutePath> }

    static member DefaultStateFilePath: AbsolutePath =
        ApplicationDirectories(applicationName = "Tabularius",
                               vendorName = "me.fornever",
                               allowCompatMode = true).StateDirectory() / "state.json"

    static member val private JsonOptions =
        JsonSerializerOptions(
            WriteIndented = true
        )

    static member LoadFromFile(path: AbsolutePath): Task<State> =
        task {
            try
                if not <| path.ExistsFile() then
                    return { LastOpenedFile = ValueNone }
                else
                    use stream = path.OpenRead()
                    let! dto = JsonSerializer.DeserializeAsync<InternalState>(stream, State.JsonOptions)
                    let lastOpenedFile =
                        dto
                        |> ValueOption.ofObj
                        |> ValueOption.map _.LastOpenedFile
                        |> ValueOption.bind ValueOption.ofObj
                        |> ValueOption.map AbsolutePath
                    return { LastOpenedFile = lastOpenedFile }
            with
            | ex ->
                Log.Root.Error ex
                return { LastOpenedFile = ValueNone }
        }

    static member SaveToFile(state: State, path: AbsolutePath): Task =
        task {
            try
                let dto: InternalState = {
                    LastOpenedFile = state.LastOpenedFile |> ValueOption.map _.Value |> ValueOption.toObj
                }

                path.Parent.Value.CreateDirectory()
                use stream = path.OpenWrite()
                do! JsonSerializer.SerializeAsync(stream, dto, State.JsonOptions)
            with
            | ex ->
                Log.Root.Error ex
        }
