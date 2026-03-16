// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;

namespace Tabularius.Interop;

public partial class Hledger
{
    [LibraryImport("hledger-interop-shared", EntryPoint = "hs_init")]
    private static unsafe partial void HsInit(nint* argc, nint* argv);

    [LibraryImport("hledger-interop-shared", EntryPoint = "hs_exit")]
    private static partial void HsExit();

    public static unsafe void Initialize() => HsInit(null, null);
    public static void Shutdown() => HsExit();

    [LibraryImport("hledger-interop-shared", EntryPoint = "adder")]
    public static partial int Adder(int a, int b);
}
