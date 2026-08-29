-- SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
--
-- SPDX-License-Identifier: MIT

module Tabularius (balanceReport) where

import Hledger.Data.Types (Journal)
import Hledger.Read (definputopts, orDieTrying, readJournal)
import Hledger.Read.Common (PrefixedFilePath)
import Hledger.Reports.ReportOptions (defreportspec)
import qualified Hledger.Reports.BalanceReport as BR (BalanceReport, balanceReport)
import System.IO (IOMode (ReadMode), hSetEncoding, utf8, withFile)

withJournal :: PrefixedFilePath -> (Journal -> r) -> IO r
withJournal path action =
    withFile path ReadMode $ \h -> do
        hSetEncoding h utf8
        journal <- orDieTrying $ readJournal definputopts (Just path) h
        return $ action journal

balanceReport :: PrefixedFilePath -> IO BR.BalanceReport
balanceReport path =
    withJournal path $ \j ->
        BR.balanceReport defreportspec j
