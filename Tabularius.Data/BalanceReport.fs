// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius.Data

type Side =
    | L = 0
    | R = 1

type AmountPrecision =
    /// Show this many decimal digits.
    | Precision of digits: byte
    /// Show all the significant decimal digits stored internally.
    | NaturalPrecision

type AmountStyle = {
    /// Show the commodity symbol on the left or the right?
    CommoditySide: Side
    /// Show a space between the commodity symbol and the amount?
    CommoditySpaced: bool
    Precision: AmountPrecision
}

type Amount = {
    Commodity: string
    Quantity: decimal
    Style: AmountStyle
}

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

type BalanceReport = {
    Items: BalanceReportItem[]
    Totals: MixedAmount
}
