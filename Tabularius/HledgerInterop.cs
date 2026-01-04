// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;

HledgerInterop.Initialize();
try
{
    Console.WriteLine("Hello, World!");
    HledgerInterop.someFunc();
    Console.WriteLine("Goodbye, World!");
}
finally
{
    HledgerInterop.Shutdown();
}

public static partial class HledgerInterop
{
    [LibraryImport("hledger-interop-shared.dll", EntryPoint = "hs_init")]
    private static unsafe partial void HsInit(nint* argc, nint* argv);

    [LibraryImport("hledger-interop-shared.dll", EntryPoint = "hs_exit")]
    private static partial void HsExit();

    public static unsafe void Initialize() => HsInit(null, null);
    public static void Shutdown() => HsExit();

    [LibraryImport("hledger-interop-shared.dll")]
    public static partial void someFunc();
}

