// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius.Settings.State;

/// <summary>Serializable contents of the application state file.</summary>
public sealed class StateDto
{
    public string? LastOpenedFile { get; set; }
}
