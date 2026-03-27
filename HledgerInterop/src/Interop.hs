-- SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
--
-- SPDX-License-Identifier: MIT

{-# LANGUAGE ForeignFunctionInterface #-}

module Interop where

import Control.Exception (bracket)
import Foreign (Ptr)
import Foreign.C.Types (CChar)
import GHC.Foreign (peekCString)
import GHC.IO.Encoding (utf8)
import System.IO (readFile')

import qualified Tabularius (verifyJournal)

foreign export ccall verifyJournal :: Ptr CChar -> IO ()

verifyJournal :: Ptr CChar -> IO ()
verifyJournal path = do
    hPath <- peekCString utf8 path
    contents <- readFile' hPath
    return ()
