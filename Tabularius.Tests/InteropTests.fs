// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

module InteropTests

open Tabularius.Interop
open Xunit

[<Fact>]
let ``Journal gets properly verified``(): unit =
    Hledger.Initialize()
    try
        Hledger.VerifyJournal(@"T:\Temp\привет\медвед.txt")
    finally
        Hledger.Shutdown()
