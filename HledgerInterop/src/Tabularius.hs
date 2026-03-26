-- SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
--
-- SPDX-License-Identifier: MIT

module Tabularius where

import Hledger.Read (readJournalFile')
import Hledger.Read.Common (PrefixedFilePath)

verifyJournal :: PrefixedFilePath -> IO ()
verifyJournal path = do
    let _ = readJournalFile' path
    return ()
