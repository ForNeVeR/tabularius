// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

module Tabularius.Configuration

open System.IO
open System.Threading.Tasks
open Microsoft.Extensions.Configuration
open Serilog
open TruePath
open TruePath.SystemIo

let TryGetConfigPath(args: string[]): Result<AbsolutePath option, string> =
    match args |> Array.tryFindIndex (fun a -> a = "--config") with
    | None -> Ok None
    | Some i ->
        if i + 1 < args.Length then
            let path = AbsolutePath(Path.GetFullPath(args.[i + 1]))
            Ok(Some path)
        else
            Error "--config requires a file path argument."

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
}

module TabulariusConfiguration =
    let Default = { DiagnosticMode = false }

let ReadTabulariusConfiguration(config: IConfigurationRoot option): TabulariusConfiguration =
    match config with
    | Some cfg ->
        let diagnosticMode =
            match cfg.["DiagnosticMode"] with
            | null -> false
            | value ->
                match System.Boolean.TryParse(value) with
                | true, v -> v
                | false, _ -> false
        { DiagnosticMode = diagnosticMode }
    | None ->
        TabulariusConfiguration.Default

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
