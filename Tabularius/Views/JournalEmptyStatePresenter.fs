// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius.Views

open System.Windows.Input
open Avalonia
open Avalonia.Controls

/// Wraps the journal report control: shows it as is, or replaces it with the "no journal loaded" state.
type JournalEmptyStatePresenter() =
    inherit ContentControl()

    static let IsEmptyProperty: StyledProperty<bool> =
        AvaloniaProperty.Register<JournalEmptyStatePresenter, bool>(
            nameof Unchecked.defaultof<JournalEmptyStatePresenter>.IsEmpty
        )

    static let LoadJournalCommandProperty: StyledProperty<ICommand | null> =
        AvaloniaProperty.Register<JournalEmptyStatePresenter, ICommand | null>(
            nameof Unchecked.defaultof<JournalEmptyStatePresenter>.LoadJournalCommand
        )

    /// True when there's no journal to show: the wrapped content gets hidden, and the empty state is shown instead.
    member this.IsEmpty
        with get(): bool = this.GetValue IsEmptyProperty
        and set(value: bool) = this.SetValue(IsEmptyProperty, value) |> ignore

    /// Invoked by the journal-loading button of the empty state.
    member this.LoadJournalCommand
        with get(): ICommand | null = this.GetValue LoadJournalCommandProperty
        and set(value: ICommand | null) = this.SetValue(LoadJournalCommandProperty, value) |> ignore
