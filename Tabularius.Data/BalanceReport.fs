// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius.Data

open System.Globalization
open System.Text

type Side =
    | L = 0
    | R = 1

type AmountPrecision =
    /// Show this many decimal digits.
    | Precision of digits: byte
    /// Show all the significant decimal digits stored internally.
    | NaturalPrecision

type AmountStyle = {
    // TODO: Decimal mark, digit groups from the Hledger.Data.Types.AmountStyle
    /// Show the commodity symbol on the left or the right?
    CommoditySide: Side
    /// Show a space between the commodity symbol and the amount?
    CommoditySpaced: bool
    Precision: AmountPrecision
}

type Amount =
    {
        Commodity: string
        Quantity: decimal
        Style: AmountStyle
    }
    override this.ToString() =
        let sb = StringBuilder()
        let append(x: string) = sb.Append x |> ignore
        if this.Style.CommoditySide = Side.L then
            append this.Commodity

        let quantity =
            match this.Style.Precision with
            | NaturalPrecision -> this.Quantity.ToString()
            | Precision digits -> this.Quantity.ToString("F" + digits.ToString(CultureInfo.InvariantCulture))
        append quantity

        if this.Style.CommoditySide = Side.R then
            append this.Commodity
        sb.ToString()

type MixedAmountEntry = {
    Commodity: string
    Value: Amount
}

type MixedAmount = {
    Entries: MixedAmountEntry[]
}

type BalanceReportItem = {
    AccountName: string
    IndentationSteps: int
    Amount: MixedAmount
}

type BalanceReport =
    {
        Items: BalanceReportItem[]
        Totals: MixedAmount
    }
