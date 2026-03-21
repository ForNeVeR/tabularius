// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius.ViewModels

open System.Collections.ObjectModel
open CommunityToolkit.Mvvm.ComponentModel
open Tabularius

type StatusViewModel(errorCollector: ErrorCollector) =
    inherit ObservableObject()
    member _.Errors: ObservableCollection<ErrorEntry> = errorCollector.Errors
