// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius.ViewModels

open System.Collections.ObjectModel
open Tabularius.Data

type BalanceReportEntry = {
    AccountName: string
    Amount: Amount
}

type BalanceReportViewModel(report: BalanceReport) =
    member val Entries: ObservableCollection<BalanceReportEntry> =
        report.Items
        |> Seq.collect(fun item ->
            item.Amount.Entries
            |> Seq.map(fun entry -> { AccountName = item.AccountName; Amount = entry.Value })
        )
        |> ObservableCollection
