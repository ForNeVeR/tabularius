// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius.Converters

open System
open Avalonia.Data.Converters

type IntGreaterThanZeroConverter() =
    interface IValueConverter with
        member _.Convert(value, _, _, _) =
            match value with
            | :? int as i -> i > 0 :> obj
            | _ -> false :> obj
        member _.ConvertBack(_, _, _, _) =
            raise (NotSupportedException())
