// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius.Tests.Interop

open System
open JetBrains.Lifetimes
open Tabularius.Interop
open TruePath
open TruePath.SystemIo
open Xunit

type TempFile =
    { Path: AbsolutePath }
    interface IDisposable with
        member this.Dispose() = this.Path.Delete()

module Journals =
    let Example = """
2026-01-01 Opening balances
    assets:ing  10000 BTC
    equity:opening/closing balances

2026-01-02 Tabularius
    assets:ing     -100 BTC = 9900 BTC
    expenses:goods  100 BTC
"""

    let Invalid = """
    2026-01-01 Opening balances
    assets:ing  10000 BTC
    equity:opening/closing balances
"""

module HledgerCollection =
    [<Literal>]
    let Name = "Hledger"

/// <remarks>
/// The GHC runtime may only be initialized once per process, so every interop test shares a single instance.
/// </remarks>
type HledgerFixture() =
    let definition = new LifetimeDefinition()
    let hledger = Hledger.Initialize definition.Lifetime

    member _.Hledger = hledger

    member _.CreateTempFile(content: string) = task {
        let path = Temporary.CreateTempFile()
        do! path.WriteAllTextAsync content
        return { Path = path }
    }

    interface IDisposable with
        member _.Dispose() = definition.Terminate()

[<CollectionDefinition(HledgerCollection.Name)>]
type HledgerCollectionDefinition() =
    interface ICollectionFixture<HledgerFixture>
