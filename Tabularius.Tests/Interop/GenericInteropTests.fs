// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius.Tests.Interop

open System
open System.Threading.Tasks
open Tabularius.Interop
open TruePath
open TruePath.SystemIo
open Xunit

[<Collection(HledgerCollection.Name)>]
type GenericInteropTests(fixture: HledgerFixture) =

    [<Fact>]
    member _.``Interop supports different path encodings``(): Task = task {
        let verify(path: AbsolutePath) = task {
            do! path.WriteAllTextAsync ""
            let! report = fixture.Hledger.BalanceReport path
            Assert.Equal(0, report.Items.Length)
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
        use! journal = fixture.CreateTempFile Journals.Invalid
        let! error = Assert.ThrowsAsync<HledgerException>(fun () -> fixture.Hledger.BalanceReport journal.Path)
        Assert.Contains("2026-01-01 Opening balances", error.Message)
        Assert.False(String.IsNullOrWhiteSpace error.StackTrace)
    }
