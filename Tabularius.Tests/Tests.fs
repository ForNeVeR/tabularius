// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

module Tests

open Tabularius.Interop
open Xunit

[<Fact>]
let ``Adder adds the numbers`` () =
    Hledger.Initialize()
    try
        Assert.Equal(5, Hledger.Adder(2, 3))
    finally
        Hledger.Shutdown()
