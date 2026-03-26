-- SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
--
-- SPDX-License-Identifier: MIT

{-# LANGUAGE ForeignFunctionInterface #-}

module Interop where

import Foreign.C.String (CString, peekCString)

import qualified Tabularius (verifyJournal)

foreign export ccall verifyJournal :: CString -> IO ()

verifyJournal :: CString -> IO ()
verifyJournal path = do
    hPath <- peekCString path
    putStrLn $ "Verifying journal at path: " ++ hPath
    contents <- readFile hPath
    putStrLn $ "Journal contents:\n" ++ contents
    return ()
    -- Tabularius.verifyJournal hPath
