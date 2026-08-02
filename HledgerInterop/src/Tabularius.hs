-- SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
--
-- SPDX-License-Identifier: MIT

module Tabularius where

import Data.Int (Int32)
import Hledger.Data.Types (jtxns)
import Hledger.Read (definputopts, orDieTrying, readJournal)
import Hledger.Read.Common (PrefixedFilePath)
import System.IO (IOMode (ReadMode), hSetEncoding, utf8, withFile)

verifyJournal :: PrefixedFilePath -> IO Int32
verifyJournal path =
    withFile path ReadMode $ \h -> do
        hSetEncoding h utf8
        journal <- orDieTrying $ readJournal definputopts (Just path) h
        return $ fromIntegral $ length $ jtxns journal
