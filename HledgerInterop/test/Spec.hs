-- SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
--
-- SPDX-License-Identifier: MIT

import Test.Hspec (hspec)

import qualified Interop.BalanceReportSpec

main :: IO ()
main = hspec $ do
    Interop.BalanceReportSpec.spec
