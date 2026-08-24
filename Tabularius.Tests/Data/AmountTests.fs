// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

module Tabularius.Tests.Data.AmountTests

open System
open System.Globalization
open Tabularius.Data
open Xunit

/// The quantity formatting in Amount.ToString depends on the ambient culture, so pin it for the assertions.
let private withInvariantCulture(): IDisposable =
    let oldCulture = CultureInfo.CurrentCulture
    CultureInfo.CurrentCulture <- CultureInfo.InvariantCulture
    { new IDisposable with
        member _.Dispose() = CultureInfo.CurrentCulture <- oldCulture }

let private amount commodity quantity side spaced precision = {
    Commodity = commodity
    Quantity = quantity
    Style = {
        CommoditySide = side
        CommoditySpaced = spaced
        Precision = precision
    }
}

[<Fact>]
let ``Left commodity with natural precision keeps all stored digits``() =
    use _culture = withInvariantCulture()
    let value = amount "$" 123.450m Side.L false NaturalPrecision
    Assert.Equal("$123.450", value.ToString())

[<Fact>]
let ``Left commodity with fixed precision pads decimals``() =
    use _culture = withInvariantCulture()
    let value = amount "$" 100m Side.L false (Precision 2uy)
    Assert.Equal("$100.00", value.ToString())

[<Fact>]
let ``Right commodity with natural precision``() =
    use _culture = withInvariantCulture()
    let value = amount "BTC" 9900m Side.R false NaturalPrecision
    Assert.Equal("9900BTC", value.ToString())

[<Fact>]
let ``Right commodity with fixed precision rounds``() =
    use _culture = withInvariantCulture()
    let value = amount "BTC" 123.456m Side.R false (Precision 2uy)
    Assert.Equal("123.46BTC", value.ToString())

[<Fact>]
let ``Zero precision drops the decimal separator``() =
    use _culture = withInvariantCulture()
    let value = amount "BTC" 123.456m Side.R false (Precision 0uy)
    Assert.Equal("123BTC", value.ToString())

[<Fact>]
let ``Negative quantities keep the sign next to the digits``() =
    use _culture = withInvariantCulture()
    let value = amount "$" -42m Side.L false (Precision 2uy)
    Assert.Equal("$-42.00", value.ToString())

[<Fact>]
let ``Spaced commodity is currently rendered without a space``() =
    use _culture = withInvariantCulture()
    let value = amount "BTC" 9900m Side.R true NaturalPrecision
    Assert.Equal("9900BTC", value.ToString())
