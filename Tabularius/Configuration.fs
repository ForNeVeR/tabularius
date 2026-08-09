// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

module Tabularius.Configuration

open System.IO
open System.Threading.Tasks
open Microsoft.Extensions.Configuration
open Serilog
open Tabularius.Settings
open TruePath
open TruePath.SystemIo

let private TryGetPathArgument (args: string[]) (name: string): Result<AbsolutePath option, string> =
    match args |> Array.tryFindIndex (fun a -> a = name) with
    | None -> Ok None
    | Some i ->
        if i + 1 < args.Length then
            let path = AbsolutePath(Path.GetFullPath(args.[i + 1]))
            Ok(Some path)
        else
            Error $"%s{name} requires a file path argument."

let TryGetConfigPath(args: string[]): Result<AbsolutePath option, string> =
    TryGetPathArgument args "--config"

let TryGetStatePath(args: string[]): Result<AbsolutePath option, string> =
    TryGetPathArgument args "--state"

let ReadConfiguration(configPath: AbsolutePath): Task<IConfigurationRoot> =
    task {
        if not (configPath.ExistsFile()) then
            raise (FileNotFoundException("Configuration file not found.", configPath.Value))

        return
            ConfigurationBuilder()
                .AddJsonFile(configPath.Value, optional = false)
                .Build()
    }

type TabulariusConfiguration = {
    DiagnosticMode: bool
    StatePath: AbsolutePath
}

module TabulariusConfiguration =
    let Default = {
        DiagnosticMode = false
        StatePath = State.DefaultStateFilePath
    }

let ReadTabulariusConfiguration(config: IConfigurationRoot option, statePath: AbsolutePath option): TabulariusConfiguration =
    let diagnosticMode =
        match config with
        | Some cfg ->
            match cfg.["DiagnosticMode"] with
            | null -> false
            | value ->
                match System.Boolean.TryParse(value) with
                | true, v -> v
                | false, _ -> false
        | None -> TabulariusConfiguration.Default.DiagnosticMode
    {
        DiagnosticMode = diagnosticMode
        StatePath = statePath |> Option.defaultValue State.DefaultStateFilePath
    }

let CreateSerilogLogger(config: IConfigurationRoot option, sink: Serilog.Core.ILogEventSink option) : Serilog.Core.Logger =
    let addSink (lc: LoggerConfiguration) =
        match sink with
        | Some s -> lc.WriteTo.Sink(s, restrictedToMinimumLevel = Serilog.Events.LogEventLevel.Error) |> ignore
        | None -> ()

    match config with
    | Some cfg when cfg.GetSection("Serilog").Exists() ->
        let lc = LoggerConfiguration().ReadFrom.Configuration(cfg)
        addSink lc
        lc.CreateLogger()
    | _ ->
        let logDir = Temporary.SystemTempDirectory() / "tabularius"
        let logFilePath = logDir / "tabularius.log"
        let lc = LoggerConfiguration().WriteTo.Console().WriteTo.File(logFilePath.Value)
        addSink lc
        lc.CreateLogger()
