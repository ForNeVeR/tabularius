// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius.Converters

open System
open Avalonia.Data.Converters

type ErrorMessageDisplayConverter() =
    interface IValueConverter with
        member _.Convert(value, _, _, _) =
            match value with
            | :? string as s when not(String.IsNullOrWhiteSpace s) -> s :> obj
            | _ -> Tabularius.Resources.Localization.ErrorList_NoMessage :> obj
        member _.ConvertBack(_, _, _, _) =
            raise (NotSupportedException())
