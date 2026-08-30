// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius.About

open System
open System.Reflection

type ApplicationInfo = {
    Name: string
    /// The informational version without its build metadata part.
    Version: string
    /// The `+<metadata>` part of the informational version, e.g. the SourceLink commit hash.
    BuildMetadata: string voption
    Copyright: string
}

module ApplicationInfo =

    /// Only used to get hold of the assembly this module is compiled into: unlike Assembly.GetEntryAssembly(), this
    /// works the same way when the code is run from the test host.
    type private Marker = class end

    let private assembly = typeof<Marker>.Assembly

    let private readAttribute<'T when 'T :> Attribute>(getValue: 'T -> string): string =
        match assembly.GetCustomAttribute typeof<'T> with
        | null -> raise <| InvalidOperationException $"The assembly carries no {typeof<'T>.Name}."
        | attribute -> getValue(attribute :?> 'T)

    let internal splitInformationalVersion(informationalVersion: string): string * string voption =
        match informationalVersion.IndexOf '+' with
        | -1 -> informationalVersion, ValueNone
        | i -> informationalVersion.Substring(0, i), ValueSome(informationalVersion.Substring(i + 1))

    let Current: ApplicationInfo =
        let version, buildMetadata =
            readAttribute<AssemblyInformationalVersionAttribute>(_.InformationalVersion)
            |> splitInformationalVersion
        {
            Name = readAttribute<AssemblyProductAttribute> _.Product
            Version = version
            BuildMetadata = buildMetadata
            Copyright = readAttribute<AssemblyCopyrightAttribute> _.Copyright
        }
