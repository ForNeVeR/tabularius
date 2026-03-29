// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius.DesignTime

open Tabularius.Interop

type HledgerDesignTimeApi() =
    interface IHledgerApi with
        member this.VerifyJournal(_, _) =
            raise(System.NotImplementedException())
