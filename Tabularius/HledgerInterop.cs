// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;
using Tabularius.Interop;

Hledger.Initialize();
try
{
    Console.WriteLine("Hello, World!");
    var result = Hledger.Adder(2, 3);
    Console.WriteLine(result);
    Console.WriteLine("Goodbye, World!");
}
finally
{
    Hledger.Shutdown();
}
