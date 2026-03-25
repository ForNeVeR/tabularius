// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

module ExceptionInfoTests

open System
open Tabularius
open Xunit

[<Fact>]
let ``fromException captures simple exception``() =
    let ex = {
        new Exception("boom") with
            member _.StackTrace = "at Foo.Bar()"
    }
    let info = ExceptionInfo.fromException ex
    Assert.Equal("boom", info.Message)
    Assert.Equal("at Foo.Bar()", info.StackTrace)
    Assert.Empty(info.InnerExceptions)

[<Fact>]
let ``fromException captures InnerException``() =
    let inner = {
        new Exception("inner") with
            member _.StackTrace = "at Inner.Method()"
    }
    let outer = Exception("outer", inner)
    let info = ExceptionInfo.fromException outer
    Assert.Equal("outer", info.Message)
    Assert.Single(info.InnerExceptions) |> ignore
    Assert.Equal("inner", info.InnerExceptions[0].Message)
    Assert.Equal("at Inner.Method()", info.InnerExceptions[0].StackTrace)
    Assert.Empty(info.InnerExceptions[0].InnerExceptions)

[<Fact>]
let ``fromException captures AggregateException with multiple inners``() =
    let inner1 = {
        new Exception("first") with
            member _.StackTrace = "at First()"
    }
    let inner2 = {
        new Exception("second") with
            member _.StackTrace = "at Second()"
    }
    let agg = AggregateException("aggregated", [| inner1; inner2 |])
    let info = ExceptionInfo.fromException agg
    Assert.Equal("aggregated (first) (second)", info.Message)
    Assert.Equal(2, info.InnerExceptions.Length)
    Assert.Equal("first", info.InnerExceptions[0].Message)
    Assert.Equal("second", info.InnerExceptions[1].Message)

[<Fact>]
let ``Equal ExceptionInfo records are structurally equal``() =
    let info1 = { TypeName = "System.Exception"; Message = "boom"; StackTrace = "at Foo()"; InnerExceptions = [] }
    let info2 = { TypeName = "System.Exception"; Message = "boom"; StackTrace = "at Foo()"; InnerExceptions = [] }
    Assert.Equal(info1, info2)

[<Fact>]
let ``Different message makes ExceptionInfo not equal``() =
    let info1 = { TypeName = "System.Exception"; Message = "boom"; StackTrace = "at Foo()"; InnerExceptions = [] }
    let info2 = { TypeName = "System.Exception"; Message = "bang"; StackTrace = "at Foo()"; InnerExceptions = [] }
    Assert.NotEqual(info1, info2)

[<Fact>]
let ``Different InnerExceptions makes ExceptionInfo not equal``() =
    let inner = { TypeName = "System.Exception"; Message = "inner"; StackTrace = ""; InnerExceptions = [] }
    let info1 = { TypeName = "System.Exception"; Message = "outer"; StackTrace = ""; InnerExceptions = [inner] }
    let info2 = { TypeName = "System.Exception"; Message = "outer"; StackTrace = ""; InnerExceptions = [] }
    Assert.NotEqual(info1, info2)

[<Fact>]
let ``Nested ExceptionInfo equality works recursively``() =
    let inner = { TypeName = "System.Exception"; Message = "inner"; StackTrace = "at Bar()"; InnerExceptions = [] }
    let info1 = { TypeName = "System.Exception"; Message = "outer"; StackTrace = "at Foo()"; InnerExceptions = [inner] }
    let info2 = { TypeName = "System.Exception"; Message = "outer"; StackTrace = "at Foo()"; InnerExceptions = [inner] }
    Assert.Equal(info1, info2)
