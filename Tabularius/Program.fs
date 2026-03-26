// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

module Tabularius.Program

open System
open System.Threading.Tasks
open Avalonia
open Avalonia.Logging
open JetBrains.Diagnostics
open Microsoft.Extensions.Logging
open Tabularius.Interop

let BuildAvaloniaApp() =
    AppBuilder
        .Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace(areas = Array.empty)

let private SetUpLogger(logger: Microsoft.Extensions.Logging.ILogger) =
    AppDomain.CurrentDomain.UnhandledException.Add(fun args ->
        match args.ExceptionObject with
        | :? Exception as ex -> logger.LogError(ex, "Unhandled AppDomain exception")
        | obj -> logger.LogError("Unhandled non-exception AppDomain object: {Object}", obj))

    TaskScheduler.UnobservedTaskException.Add(fun args ->
        logger.LogError(args.Exception, "Unobserved task exception")
        args.SetObserved())

    JetBrains.Diagnostics.Log.DefaultFactory <- {
        new ILogFactory with
            member this.GetLog(category) =
                let convertLevel (level: LoggingLevel) =
                    match level with
                    | LoggingLevel.OFF -> LogLevel.None
                    | LoggingLevel.FATAL -> LogLevel.Critical
                    | LoggingLevel.ERROR -> LogLevel.Error
                    | LoggingLevel.WARN -> LogLevel.Warning
                    | LoggingLevel.INFO -> LogLevel.Information
                    | LoggingLevel.VERBOSE -> LogLevel.Debug
                    | LoggingLevel.TRACE -> LogLevel.Trace
                    | _ -> LogLevel.Critical

                {
                    new ILog with
                    member this.IsEnabled(level) = logger.IsEnabled(convertLevel level)
                    member this.Log(level, message, ``exception``) =
                        logger.Log(convertLevel level, message, ``exception``)
                    member this.Category = category
                }
    }

    let convertLevel(level: LogEventLevel) =
        match level with
        | LogEventLevel.Fatal -> LogLevel.Critical
        | LogEventLevel.Error -> LogLevel.Error
        | LogEventLevel.Warning -> LogLevel.Warning
        | LogEventLevel.Information -> LogLevel.Information
        | LogEventLevel.Debug -> LogLevel.Debug
        | LogEventLevel.Verbose -> LogLevel.Trace
        | _ -> LogLevel.Critical
    Logger.Sink <- {
        new ILogSink with
            member this.IsEnabled(level, area) =
                logger.IsEnabled(convertLevel level)

            member this.Log(level, area, source, messageTemplate) =
                logger.Log(convertLevel level, messageTemplate, null)
            member this.Log(level, area, source, messageTemplate, propertyValues) =
                logger.Log(convertLevel level, messageTemplate, propertyValues)
    }

[<EntryPoint; STAThread>]
let main(args: string[]) : int =
    Hledger.Initialize()
    Hledger.VerifyJournal(@"T:\Temp\привет\медвед.txt")
    // Hledger.VerifyJournal(@"T:\Temp\privet\medved\medved.txt")
    Hledger.Shutdown()
    0

    // match Configuration.TryGetConfigPath args with
    // | Error msg ->
    //     eprintfn $"%s{msg}"
    //     ExitCodes.ConfigArgMissing
    // | Ok configPath ->
    //     let config =
    //         match configPath with
    //         | Some path ->
    //             try
    //                 let cfg = Configuration.ReadConfiguration(path).GetAwaiter().GetResult()
    //                 Some cfg
    //             with
    //             | :? FileNotFoundException as ex ->
    //                 eprintfn $"Configuration file not found: %s{ex.FileName}"
    //                 None
    //             | :? FormatException as ex ->
    //                 eprintfn $"Invalid configuration file: %s{ex.Message}"
    //                 None
    //         | None -> None
    //
    //     match configPath, config with
    //     | Some _, None ->
    //         // Config was requested but failed to load.
    //         ExitCodes.ConfigFileNotFound
    //     | _ ->
    //         let appConfig = Configuration.ReadTabulariusConfiguration(config)
    //         let errorCollector = ErrorCollector(Lifetime.Eternal, AvaloniaScheduler())
    //         let activityHost = BackgroundActivityHost(AvaloniaScheduler())
    //
    //         let serilogLogger = Configuration.CreateSerilogLogger(config, Some(errorCollector :> Serilog.Core.ILogEventSink))
    //         Log.Logger <- serilogLogger
    //
    //         use loggerFactory =
    //             LoggerFactory.Create(fun builder ->
    //                 builder.AddSerilog(serilogLogger, dispose = true) |> ignore)
    //
    //         let logger = loggerFactory.CreateLogger("Tabularius.Program")
    //
    //         SetUpLogger logger
    //
    //         App.SetErrorCollector(errorCollector)
    //         App.SetConfiguration(appConfig)
    //         App.SetBackgroundActivityHost(activityHost)
    //
    //         logger.LogInformation("Tabularius is starting.")
    //
    //         try
    //             BuildAvaloniaApp().StartWithClassicDesktopLifetime(args)
    //         finally
    //             logger.LogInformation("Tabularius is shutting down.")
