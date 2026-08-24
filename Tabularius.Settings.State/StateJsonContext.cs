// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

using System.Text.Json.Serialization;

namespace Tabularius.Settings.State;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(StateDto))]
public sealed partial class StateJsonContext : JsonSerializerContext;
