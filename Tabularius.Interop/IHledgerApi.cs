// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

using JetBrains.Annotations;
using Tabularius.Data;
using TruePath;

namespace Tabularius.Interop;

[PublicAPI]
public interface IHledgerApi
{
    Task<BalanceReport> BalanceReport(AbsolutePath journalPath, CancellationToken cancellationToken = default);
}
