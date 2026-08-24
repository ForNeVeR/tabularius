-- SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
--
-- SPDX-License-Identifier: MIT

import Test.Hspec (hspec)

import qualified Interop.BalanceReportSpec
import qualified Interop.VerifyJournalSpec

main :: IO ()
main = hspec $ do
    Interop.BalanceReportSpec.spec
    Interop.VerifyJournalSpec.spec
