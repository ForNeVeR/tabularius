// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

module Tabularius.Program

open System
open System.IO
open System.Threading.Tasks
open Avalonia
open JetBrains.Lifetimes
open Microsoft.Extensions.Logging
open Serilog

let BuildAvaloniaApp() =
    AppBuilder
        .Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace(areas = Array.empty)

[<EntryPoint; STAThread>]
let main(args: string[]) : int =
    match Configuration.TryGetConfigPath args with
    | Error msg ->
        eprintfn $"%s{msg}"
        ExitCodes.ConfigArgMissing
    | Ok configPath ->
        let config =
            match configPath with
            | Some path ->
                try
                    let cfg = Configuration.ReadConfiguration(path).GetAwaiter().GetResult()
                    Some cfg
                with
                | :? FileNotFoundException as ex ->
                    eprintfn $"Configuration file not found: %s{ex.FileName}"
                    None
                | :? FormatException as ex ->
                    eprintfn $"Invalid configuration file: %s{ex.Message}"
                    None
            | None -> None

        match configPath, config with
        | Some _, None ->
            // Config was requested but failed to load.
            ExitCodes.ConfigFileNotFound
        | _ ->
            let appConfig = Configuration.ReadTabulariusConfiguration(config)
            let errorCollector = ErrorCollector(Lifetime.Eternal, AvaloniaScheduler())

            let serilogLogger = Configuration.CreateSerilogLogger(config, Some(errorCollector :> Serilog.Core.ILogEventSink))
            Log.Logger <- serilogLogger

            use loggerFactory =
                LoggerFactory.Create(fun builder ->
                    builder.AddSerilog(serilogLogger, dispose = true) |> ignore)

            let logger = loggerFactory.CreateLogger("Tabularius.Program")

            AppDomain.CurrentDomain.UnhandledException.Add(fun args ->
                match args.ExceptionObject with
                | :? Exception as ex -> logger.LogError(ex, "Unhandled AppDomain exception")
                | obj -> logger.LogError("Unhandled non-exception AppDomain object: {Object}", obj))

            TaskScheduler.UnobservedTaskException.Add(fun args ->
                logger.LogError(args.Exception, "Unobserved task exception")
                args.SetObserved())

            App.SetErrorCollector(errorCollector)
            App.SetConfiguration(appConfig)

            logger.LogInformation("Tabularius is starting.")

            try
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args)
            finally
                logger.LogInformation("Tabularius is shutting down.")
