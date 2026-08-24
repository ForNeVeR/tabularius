// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius.Tests.Interop

open System
open System.Numerics
open System.Threading.Tasks
open Tabularius.Data
open Tabularius.Interop
open Xunit

[<Collection(HledgerCollection.Name)>]
type BalanceReportTests(fixture: HledgerFixture) =

    [<Fact>]
    member _.``Balance report gets calculated``(): Task = task {
        use! journal = fixture.CreateTempFile Journals.Example
        let! report = fixture.Hledger.BalanceReport journal.Path

        Assert.Equal<string[]>(
            [| "assets:ing"; "equity:opening/closing balances"; "expenses:goods" |],
            report.Items |> Array.map _.AccountName)

        let amounts = report.Items |> Array.map (fun item -> Array.exactlyOne item.Amount.Entries)
        Assert.Equal<string[]>([| "BTC"; "BTC"; "BTC" |], amounts |> Array.map _.Commodity)
        Assert.Equal<decimal[]>([| 9900m; -10000m; 100m |], amounts |> Array.map _.Value.Quantity)
        Assert.Equal(Side.R, amounts[0].Value.Style.CommoditySide)
        Assert.True amounts[0].Value.Style.CommoditySpaced
    }

    [<Fact>]
    member _.``Empty journal produces an empty balance report``(): Task = task {
        use! journal = fixture.CreateTempFile ""
        let! report = fixture.Hledger.BalanceReport journal.Path
        Assert.Empty report.Items
        Assert.Empty report.Totals.Entries
    }

    [<Fact>]
    member _.``Balance report reports errors``(): Task = task {
        use! journal = fixture.CreateTempFile Journals.Invalid
        let! error = Assert.ThrowsAsync<HledgerException>(fun () -> fixture.Hledger.BalanceReport journal.Path)
        Assert.False(String.IsNullOrWhiteSpace error.Message)
        Assert.False(String.IsNullOrWhiteSpace error.StackTrace)
    }

module BalanceReportMarshallerTests =

    [<Fact>]
    let ``Quantities are converted exactly``() =
        Assert.Equal(-10000m, BalanceReportMarshaller.ToDecimal(BigInteger(-10000), 0uy))
        Assert.Equal(123.45m, BalanceReportMarshaller.ToDecimal(BigInteger(12345), 2uy))

    [<Fact>]
    let ``Too many decimal places are rejected``() =
        Assert.Throws<OverflowException>(Action(fun () ->
            BalanceReportMarshaller.ToDecimal(BigInteger.One, 29uy) |> ignore)) |> ignore

    [<Fact>]
    let ``Too large mantissas are rejected``() =
        Assert.Throws<OverflowException>(Action(fun () ->
            BalanceReportMarshaller.ToDecimal(BigInteger.Pow(BigInteger 2, 96), 0uy) |> ignore)) |> ignore
