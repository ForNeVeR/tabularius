// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

using JetBrains.Annotations;
using TruePath;

namespace Tabularius.Interop;

[PublicAPI]
public interface IHledgerApi
{
    Task<int> VerifyJournal(AbsolutePath journalPath, CancellationToken cancellationToken = default);
}
