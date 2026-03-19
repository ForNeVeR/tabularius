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

let CreateSerilogLogger(config: IConfigurationRoot option) : Serilog.Core.Logger =
    match config with
    | Some cfg when cfg.GetSection("Serilog").Exists() ->
        LoggerConfiguration()
            .ReadFrom.Configuration(cfg)
            .CreateLogger()
    | _ ->
        let logDir = Temporary.SystemTempDirectory() / "tabularius"
        let logFilePath = logDir / "tabularius.log"

        LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(logFilePath.Value)
            .CreateLogger()
