-- SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
--
-- SPDX-License-Identifier: MIT

module Tabularius where

import Data.Int (Int32)
import Hledger.Data.Types (jtxns)
import Hledger.Read (readJournalFile')
import Hledger.Read.Common (PrefixedFilePath)

verifyJournal :: PrefixedFilePath -> IO Int32
verifyJournal path = do
    journal <- readJournalFile' path
    return $ fromIntegral $ length $ jtxns journal
