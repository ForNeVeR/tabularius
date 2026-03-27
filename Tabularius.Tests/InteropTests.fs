// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

module InteropTests

open System
open System.Threading
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

let private CreateTempFile(content: string) = task {
    let path = Temporary.CreateTempFile()
    do! path.WriteAllTextAsync content
    return { Path = path }
}

let private HledgerMutex = new SemaphoreSlim(1, 1)
let mutable private Initialized = false
let DoWithHledger action = task {
    do! HledgerMutex.WaitAsync()
    try
        // We only support single-time initialization, as multiple reinitialization is not supported by GHC.
        if not Initialized then Hledger.Initialize Lifetime.Eternal
    finally
        HledgerMutex.Release() |> ignore

    return! Lifetime.UsingAsync(fun lt -> task {
        Hledger.Initialize lt
        return! action()
    })
}


[<Fact>]
let ``Journal gets properly verified``(): Task = DoWithHledger(fun() -> task {
    use! journal = CreateTempFile """
2026-01-01 Opening balances
    assets:ing  10000 BTC
    equity:opening/closing balances

2026-01-02 Tabularius
    assets:ing     -100 BTC = 9900 BTC
    expenses:goods  100 BTC
"""
    Assert.Equal(2, Hledger.VerifyJournal journal.Path)
})

[<Fact>]
let ``Interop supports different path encodings``(): Task = task {
    let verify(path: AbsolutePath) = task {
        do! path.WriteAllTextAsync ""
        Assert.Equal(0, Hledger.VerifyJournal path)
    }

    let folder = Temporary.CreateTempFolder("tabularius")
    try
        do! DoWithHledger(fun() -> task {
            // Make sure we support Cyrillic and Chinese file names.
            // On Windows, these are never covered by the same ANSI code page,
            // so this will verify that we really support Unicode file names.
            do! verify(folder / "привет.journal")
            do! verify(folder / "你好.journal")
        })
    finally
        folder.DeleteDirectoryRecursively()
}
