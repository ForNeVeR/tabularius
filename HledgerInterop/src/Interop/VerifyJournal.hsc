-- SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
--
-- SPDX-License-Identifier: MIT

{-# LANGUAGE ForeignFunctionInterface #-}

module Interop.VerifyJournal
    ( VerifyJournalResult(..)
    , verifyJournal
    , freeVerifyJournalResult
    ) where

import Control.Exception (SomeException, try)
import Data.Int (Int32)
import Foreign.C.String (CString)
import Foreign.Marshal.Alloc (free)
import Foreign.Ptr (Ptr, nullPtr)
import Foreign.Storable (Storable(..))
import GHC.Foreign (peekCString)
import GHC.IO.Encoding (utf8)
import GHC.Stack (HasCallStack)

import Interop.Common (describeException, freeIfAllocated, newStruct)
import qualified Tabularius (verifyJournal)

#include "verify_journal_result.h"

data VerifyJournalResult = VerifyJournalResult
    { recordCount :: Int32
    , errorMessage :: CString
    , stackTrace :: CString
    }

instance Storable VerifyJournalResult where
    sizeOf _ = #size struct VerifyJournalResult
    alignment _ = #alignment struct VerifyJournalResult

    peek ptr = do
        recordCount' <- (#peek struct VerifyJournalResult, recordCount) ptr
        errorMessage' <- (#peek struct VerifyJournalResult, errorMessage) ptr
        stackTrace' <- (#peek struct VerifyJournalResult, stackTrace) ptr
        pure VerifyJournalResult
            { recordCount = recordCount'
            , errorMessage = errorMessage'
            , stackTrace = stackTrace'
            }

    poke ptr result = do
        (#poke struct VerifyJournalResult, recordCount) ptr (recordCount result)
        (#poke struct VerifyJournalResult, errorMessage) ptr (errorMessage result)
        (#poke struct VerifyJournalResult, stackTrace) ptr (stackTrace result)

foreign export ccall verifyJournal :: CString -> IO (Ptr VerifyJournalResult)
foreign export ccall freeVerifyJournalResult :: Ptr VerifyJournalResult -> IO ()

verifyJournal :: CString -> IO (Ptr VerifyJournalResult)
verifyJournal pathPtr = do
    hPath <- peekCString utf8 pathPtr
    buildVerifyJournalResult hPath

buildVerifyJournalResult :: HasCallStack => FilePath -> IO (Ptr VerifyJournalResult)
buildVerifyJournalResult path = do
    outcome <- (try $ Tabularius.verifyJournal path) :: IO (Either SomeException Int32)
    case outcome of
        Right count -> newVerifyJournalResult count nullPtr nullPtr
        Left se -> do
            (messagePtr, stackPtr) <- describeException se
            newVerifyJournalResult (-1) messagePtr stackPtr

newVerifyJournalResult :: Int32 -> CString -> CString -> IO (Ptr VerifyJournalResult)
newVerifyJournalResult recordCount' errorMessage' stackTrace' =
    newStruct VerifyJournalResult
        { recordCount = recordCount'
        , errorMessage = errorMessage'
        , stackTrace = stackTrace'
        }

freeVerifyJournalResult :: Ptr VerifyJournalResult -> IO ()
freeVerifyJournalResult resultPtr
    | resultPtr == nullPtr = pure ()
    | otherwise = do
        result <- peek resultPtr
        freeIfAllocated (errorMessage result)
        freeIfAllocated (stackTrace result)
        free resultPtr
