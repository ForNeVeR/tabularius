-- SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
--
-- SPDX-License-Identifier: MIT

{-# LANGUAGE ForeignFunctionInterface #-}

module Interop where

import Data.Int (Int32)
import Foreign.C.String (CString)
import GHC.Foreign (peekCString)
import GHC.IO.Encoding (utf8)

import qualified Tabularius (verifyJournal)

foreign export ccall verifyJournal :: CString -> IO Int32

verifyJournal :: CString -> IO Int32
verifyJournal pathPtr = do
    hPath <- peekCString utf8 pathPtr
    Tabularius.verifyJournal hPath
