// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

using JetBrains.Annotations;
using JetBrains.Lifetimes;
using TruePath;

namespace Tabularius.Interop;

[PublicAPI]
public static class Hledger
{
    public static unsafe void Initialize(Lifetime lifetime)
    {
        lifetime.Bracket(
            () => HledgerInterop.HsInit(null, null),
            () => HledgerInterop.HsExit());
    }

    public static void VerifyJournal(AbsolutePath journalPath)
    {
        HledgerInterop.VerifyJournal(journalPath.Value);
    }
}
