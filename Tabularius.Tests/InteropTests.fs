// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius.Tests

open System
open System.Threading.Tasks
open JetBrains.Lifetimes
open Tabularius.Interop
open TruePath
open TruePath.SystemIo
open Xunit

type private TempFile =
    { Path: AbsolutePath }
    interface IDisposable with
        member this.Dispose() = this.Path.Delete()

type HledgerFixture() =
    let definition = new LifetimeDefinition()
    let hledger = Hledger.Initialize definition.Lifetime

    member _.Hledger = hledger

    interface IDisposable with
        member _.Dispose() = definition.Terminate()

type InteropTests(fixture: HledgerFixture) =
    let CreateTempFile(content: string) = task {
        let path = Temporary.CreateTempFile()
        do! path.WriteAllTextAsync content
        return { Path = path }
    }


    interface IClassFixture<HledgerFixture>

     [<Fact>]
    member _.``Journal gets properly verified``(): Task = task {
        use! journal = CreateTempFile """
2026-01-01 Opening balances
    assets:ing  10000 BTC
    equity:opening/closing balances

2026-01-02 Tabularius
    assets:ing     -100 BTC = 9900 BTC
    expenses:goods  100 BTC
"""
        let! transactions = fixture.Hledger.VerifyJournal journal.Path
        Assert.Equal(2, transactions)
    }

    [<Fact>]
    member _.``Interop supports different path encodings``(): Task = task {
        let verify(path: AbsolutePath) = task {
            do! path.WriteAllTextAsync ""
            let! transactions = fixture.Hledger.VerifyJournal path
            Assert.Equal(0, transactions)
        }

        let folder = Temporary.CreateTempFolder("tabularius")
        try
            // Make sure we support Cyrillic and Chinese file names.
            // On Windows, these are never covered by the same ANSI code page,
            // so this will verify that we really support Unicode file names.
            do! verify(folder / "привет.journal")
            do! verify(folder / "你好.journal")
        finally
            folder.DeleteDirectoryRecursively()
    }

    [<Fact>]
    member _.``Interop supports error processing correctly``(): Task = task {
        use! journal = CreateTempFile """
    2026-01-01 Opening balances
    assets:ing  10000 BTC
    equity:opening/closing balances
"""
        let! error = Assert.ThrowsAsync<HledgerException>(fun () -> fixture.Hledger.VerifyJournal journal.Path)
        Assert.Contains("2026-01-01 Opening balances", error.Message)
        Assert.False(String.IsNullOrWhiteSpace error.StackTrace)
    }
