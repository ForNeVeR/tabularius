// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius.About

open System
open Tabularius.Resources

type AboutViewModel(info: ApplicationInfo) =
    new() = AboutViewModel ApplicationInfo.Current

    member _.Title: string = String.Format(Localization.About_WindowTitle, info.Name)
    member _.ApplicationName: string = info.Name
    member _.Version: string = String.Format(Localization.About_Version, info.Version)
    member _.BuildMetadata: string =
        match info.BuildMetadata with
        | ValueSome metadata -> String.Format(Localization.About_Build, metadata)
        | ValueNone -> ""
    member _.HasBuildMetadata: bool = info.BuildMetadata |> ValueOption.isSome
    member _.Copyright: string = String.Format(Localization.About_Copyright, info.Copyright)
