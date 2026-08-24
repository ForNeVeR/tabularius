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
type VerifyJournalTests(fixture: HledgerFixture) =

    [<Fact>]
    member _.``Journal gets properly verified``(): Task = task {
        use! journal = fixture.CreateTempFile Journals.Example
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
        use! journal = fixture.CreateTempFile Journals.Invalid
        let! error = Assert.ThrowsAsync<HledgerException>(fun () -> fixture.Hledger.VerifyJournal journal.Path)
        Assert.Contains("2026-01-01 Opening balances", error.Message)
        Assert.False(String.IsNullOrWhiteSpace error.StackTrace)
    }
