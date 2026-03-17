// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

module Tabularius.Program

open System
open Tabularius.Interop

Hledger.Initialize()
try
    Console.WriteLine("Hello, World!")
    let result = Hledger.Adder(2, 3)
    Console.WriteLine(result)
    Console.WriteLine("Goodbye, World!")
finally
    Hledger.Shutdown()
