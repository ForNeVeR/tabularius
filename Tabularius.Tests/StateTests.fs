// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

module Tabularius.Tests.StateTests

open System
open System.Runtime.InteropServices
open System.Threading.Tasks
open Tabularius.Settings
open TruePath
open Xunit


[<Fact>]
let ``SaveToFile then LoadFromFile round-trips Some``(): Task = task {
    let path = Temporary.CreateTempFile()
    let expected = { LastOpenedFile = Some(AbsolutePath.CurrentWorkingDirectory / "some-file.journal") }
    do! State.SaveToFile(expected, path)
    let! actual = State.LoadFromFile(path)
    Assert.Equal(expected, actual)
}

[<Fact>]
let ``SaveToFile then LoadFromFile round-trips None``(): Task = task {
    let path = Temporary.CreateTempFile()
    let expected = { LastOpenedFile = None }
    do! State.SaveToFile(expected, path)
    let! actual = State.LoadFromFile(path)
    Assert.Equal(None, actual.LastOpenedFile)
}

[<Fact>]
let ``LoadFromFile returns default state when file is missing``(): Task = task {
    let path = Temporary.CreateTempFile()
    let! actual = State.LoadFromFile(path)
    Assert.Equal({ LastOpenedFile = None }, actual)
}

[<Fact>]
let ``DefaultStateFilePath follows the per-OS convention``(): unit =
    let expected =
        if RuntimeInformation.IsOSPlatform OSPlatform.Windows then
            AbsolutePath(nonNull <| Environment.GetEnvironmentVariable "LOCALAPPDATA") / "me.fornever" / "Tabularius" / ".state" / "state.json"
         elif RuntimeInformation.IsOSPlatform OSPlatform.OSX then
            AbsolutePath(nonNull <| Environment.GetEnvironmentVariable "HOME") / "Library" / "Application Support" / "me.fornever.Tabularius" / ".state" / "state.json"
        else
            let xdgStateHome = Environment.GetEnvironmentVariable("XDG_STATE_HOME")
            if String.IsNullOrWhiteSpace xdgStateHome then
                AbsolutePath(nonNull <| Environment.GetEnvironmentVariable "HOME") / ".local" / "state" / "Tabularius" / "state.json"
            else
                AbsolutePath(nonNull xdgStateHome) / "Tabularius" / "state.json"
    let path = State.DefaultStateFilePath
    Assert.Equal(expected, path)
