// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;

namespace Tabularius.Interop;

internal static partial class HledgerInterop
{
    [LibraryImport("hledger-interop-shared", EntryPoint = "hs_init")]
    public static unsafe partial void HsInit(int* argc, nint* argv);

    [LibraryImport("hledger-interop-shared", EntryPoint = "hs_exit")]
    public static partial void HsExit();

    [LibraryImport("hledger-interop-shared", EntryPoint = "verifyJournal", StringMarshalling = StringMarshalling.Utf8)]
    public static unsafe partial VerifyJournalResult* VerifyJournal(string journalPath);
}
