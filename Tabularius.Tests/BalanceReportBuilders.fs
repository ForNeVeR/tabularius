// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

/// Builders for the balance report data, shared between the test suites.
module Tabularius.Tests.BalanceReportBuilders

open Tabularius.Data

let amount commodity quantity: Amount = {
    Commodity = commodity
    Quantity = quantity
    Style = {
        CommoditySide = Side.R
        CommoditySpaced = true
        Precision = Precision 0uy
    }
}

let reportItem account indentationSteps (amounts: Amount[]): BalanceReportItem = {
    AccountName = account
    IndentationSteps = indentationSteps
    Amount = {
        Entries = amounts |> Array.map(fun a -> { Commodity = a.Commodity; Value = a })
    }
}

let reportOf items: BalanceReport = {
    Items = items
    Totals = { Entries = Array.empty }
}

let emptyReport = reportOf Array.empty
