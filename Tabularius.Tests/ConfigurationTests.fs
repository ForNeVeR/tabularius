// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

module ConfigurationTests

open System.IO
open Microsoft.Extensions.Configuration
open Tabularius.Configuration
open TruePath
open Xunit

[<Fact>]
let ``TryGetConfigPath returns None when no args``(): unit =
    Assert.Equal(Ok None, TryGetConfigPath [||])

[<Fact>]
let ``TryGetConfigPath returns path when --config is provided``(): unit =
    let result = TryGetConfigPath [| "--config"; "test.json" |]
    match result with
    | Ok(Some path) -> Assert.EndsWith("test.json", path.Value)
    | _ -> Assert.Fail "Expected Ok(Some path)"

[<Fact>]
let ``TryGetConfigPath returns Error when --config has no value``(): unit =
    match TryGetConfigPath [| "--config" |] with
    | Error _ -> ()
    | _ -> Assert.Fail "Expected Error"

[<Fact>]
let ``TryGetConfigPath ignores unrelated args``(): unit =
    Assert.Equal(Ok None, TryGetConfigPath [| "--verbose"; "true" |])

[<Fact>]
let ``ReadConfiguration reads valid JSON file``(): unit =
    let testDataDir = AbsolutePath.CurrentWorkingDirectory / "testdata"
    let configPath = testDataDir / "valid-config.json"
    let config = ReadConfiguration(configPath).GetAwaiter().GetResult()
    Assert.True(config.GetSection("Serilog").Exists())
    Assert.Equal("Warning", config.["Serilog:MinimumLevel"])

[<Fact>]
let ``ReadConfiguration throws for missing file``(): unit =
    let missingPath = AbsolutePath.CurrentWorkingDirectory / "nonexistent.json"
    Assert.Throws<FileNotFoundException>(fun () ->
        ReadConfiguration(missingPath).GetAwaiter().GetResult() |> ignore)
    |> ignore

[<Fact>]
let ``CreateSerilogLogger returns logger with config``(): unit =
    let testDataDir = AbsolutePath.CurrentWorkingDirectory / "testdata"
    let configPath = testDataDir / "valid-config.json"
    let config = ReadConfiguration(configPath).GetAwaiter().GetResult()
    use logger = CreateSerilogLogger(Some config)
    Assert.NotNull(logger)

[<Fact>]
let ``CreateSerilogLogger returns default logger without config``(): unit =
    use logger = CreateSerilogLogger None
    Assert.NotNull(logger)
