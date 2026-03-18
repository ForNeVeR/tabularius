// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

module Tabularius.Program

open System
open Microsoft.Extensions.Logging
open Serilog
open Tabularius.Interop
open TruePath

[<EntryPoint>]
let main (_args: string[]): int =
    let logDir = Temporary.SystemTempDirectory() / "tabularius"
    let logFilePath = logDir / "tabularius.log"

    let serilogLogger =
        LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(logFilePath.Value)
            .CreateLogger()

    use loggerFactory =
        LoggerFactory.Create(fun builder ->
            builder.AddSerilog(serilogLogger, dispose = true) |> ignore)

    let logger = loggerFactory.CreateLogger("Tabularius.Program")

    logger.LogInformation("Tabularius is starting. Log directory: {LogDir}", logDir)

    Hledger.Initialize()
    try
        let result = Hledger.Adder(2, 3)
        Console.WriteLine(result)
    finally
        logger.LogInformation("Tabularius is shutting down.")
        Hledger.Shutdown()

    0
