// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

module Tabularius.Tests.BalanceReportViewModelTests

open Tabularius.Data
open Tabularius.ViewModels
open Xunit

let private amount commodity quantity: Amount = {
    Commodity = commodity
    Quantity = quantity
    Style = {
        CommoditySide = Side.R
        CommoditySpaced = true
        Precision = Precision 0uy
    }
}

let private reportItem account indentationSteps (amounts: Amount[]) = {
    AccountName = account
    IndentationSteps = indentationSteps
    Amount = {
        Entries = amounts |> Array.map(fun a -> { Commodity = a.Commodity; Value = a })
    }
}

let private reportOf items: BalanceReport = {
    Items = items
    Totals = { Entries = Array.empty }
}

[<Fact>]
let ``Entries are flattened from the report items``() =
    let report = reportOf [|
        reportItem "assets:ing" 0 [| amount "BTC" 9900m |]
        reportItem "expenses:goods" 1 [| amount "BTC" 100m; amount "USD" 42.5m |]
    |]

    let vm = BalanceReportViewModel report

    Assert.Equal<string[]>(
        [| "assets:ing"; "expenses:goods"; "expenses:goods" |],
        vm.Entries |> Seq.map _.AccountName |> Seq.toArray)
    Assert.Equal<string[]>(
        [| "BTC"; "BTC"; "USD" |],
        vm.Entries |> Seq.map _.Amount.Commodity |> Seq.toArray)
    Assert.Equal<decimal[]>(
        [| 9900m; 100m; 42.5m |],
        vm.Entries |> Seq.map _.Amount.Quantity |> Seq.toArray)

[<Fact>]
let ``Entries are calculated only once``() =
    let vm = BalanceReportViewModel(reportOf [| reportItem "assets:ing" 0 [| amount "BTC" 9900m |] |])
    Assert.Same(vm.Entries, vm.Entries)

[<Fact>]
let ``Empty report produces no entries``() =
    let vm = BalanceReportViewModel(reportOf Array.empty)
    Assert.Empty vm.Entries
