// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

module Tabularius.Tests.AboutViewModelTests

open Tabularius.About
open Xunit

let private synthetic = {
    Name = "Tabularius Test"
    Version = "1.2.3"
    BuildMetadata = ValueSome "abcdef0"
    Copyright = "2024-2026 Friedrich von Never"
}

[<Fact>]
let ``Current reads the product name from the assembly``(): unit =
    Assert.Equal("Tabularius", ApplicationInfo.Current.Name)

[<Fact>]
let ``Current reads a non-empty version from the assembly``(): unit =
    Assert.NotEmpty ApplicationInfo.Current.Version

[<Fact>]
let ``Current reads a non-empty copyright from the assembly``(): unit =
    Assert.NotEmpty ApplicationInfo.Current.Copyright

[<Fact>]
let ``splitInformationalVersion separates the build metadata``(): unit =
    Assert.Equal(("0.0.0", ValueSome "abcdef0"), ApplicationInfo.splitInformationalVersion "0.0.0+abcdef0")

[<Fact>]
let ``splitInformationalVersion keeps a version without build metadata``(): unit =
    Assert.Equal(("0.0.0", ValueNone), ApplicationInfo.splitInformationalVersion "0.0.0")

[<Fact>]
let ``splitInformationalVersion only cuts at the first plus``(): unit =
    Assert.Equal(("1.0.0", ValueSome "a+b"), ApplicationInfo.splitInformationalVersion "1.0.0+a+b")

[<Fact>]
let ``Title is formatted from the application name``(): unit =
    Assert.Equal("About Tabularius Test", AboutViewModel(synthetic).Title)

[<Fact>]
let ``ApplicationName is shown verbatim``(): unit =
    Assert.Equal("Tabularius Test", AboutViewModel(synthetic).ApplicationName)

[<Fact>]
let ``Version is formatted``(): unit =
    Assert.Equal("Version 1.2.3", AboutViewModel(synthetic).Version)

[<Fact>]
let ``Copyright is formatted``(): unit =
    Assert.Equal("Copyright 2024-2026 Friedrich von Never", AboutViewModel(synthetic).Copyright)

[<Fact>]
let ``BuildMetadata is formatted when present``(): unit =
    let vm = AboutViewModel synthetic
    Assert.True vm.HasBuildMetadata
    Assert.Equal("Build abcdef0", vm.BuildMetadata)

[<Fact>]
let ``BuildMetadata is empty when absent``(): unit =
    let vm = AboutViewModel { synthetic with BuildMetadata = ValueNone }
    Assert.False vm.HasBuildMetadata
    Assert.Equal("", vm.BuildMetadata)
