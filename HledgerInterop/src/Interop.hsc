-- SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
--
-- SPDX-License-Identifier: MIT

{-# LANGUAGE ForeignFunctionInterface #-}

module Interop
    ( VerifyJournalResult(..)
    , verifyJournal
    , freeVerifyJournalResult
    ) where

import Control.Exception (SomeException, displayException, try)
import Control.Exception.Backtrace (collectBacktraces, displayBacktraces)
import Data.Int (Int32)
import Foreign.C.String (CString)
import Data.List (intercalate)
import Foreign.Marshal.Alloc (free, malloc)
import Foreign.Ptr (Ptr, nullPtr)
import Foreign.Storable (Storable(..))
import GHC.Foreign (newCString, peekCString)
import GHC.IO.Encoding (utf8)
import GHC.Stack (HasCallStack, callStack, prettyCallStack)

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
        recordCount' <- (#peek struct VerifyJournalResult, record_count) ptr
        errorMessage' <- (#peek struct VerifyJournalResult, error_message) ptr
        stackTrace' <- (#peek struct VerifyJournalResult, stack_trace) ptr
        pure VerifyJournalResult
            { recordCount = recordCount'
            , errorMessage = errorMessage'
            , stackTrace = stackTrace'
            }

    poke ptr result = do
        (#poke struct VerifyJournalResult, record_count) ptr (recordCount result)
        (#poke struct VerifyJournalResult, error_message) ptr (errorMessage result)
        (#poke struct VerifyJournalResult, stack_trace) ptr (stackTrace result)

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
        Left ex -> do
            messagePtr <- newCString utf8 (displayException ex)
            stackPtr <- newCString utf8 =<< renderStackTrace
            newVerifyJournalResult (-1) messagePtr stackPtr
  where
    renderStackTrace :: HasCallStack => IO String
    renderStackTrace = do
        let callStackText = prettyCallStack callStack
        backtraces <- collectBacktraces
        let runtimeBacktraceText = displayBacktraces backtraces
            sections =
                filter
                    (not . null)
                    [ if null callStackText then "" else "Call stack:\n" <> callStackText
                    , if null runtimeBacktraceText then "" else "Runtime backtrace:\n" <> runtimeBacktraceText
                    ]
        pure $
            if null sections
                then "No Haskell stack trace is available."
                else intercalate "\n\n" sections

newVerifyJournalResult :: Int32 -> CString -> CString -> IO (Ptr VerifyJournalResult)
newVerifyJournalResult recordCount' errorMessage' stackTrace' = do
    resultPtr <- malloc
    poke resultPtr VerifyJournalResult
        { recordCount = recordCount'
        , errorMessage = errorMessage'
        , stackTrace = stackTrace'
        }
    pure resultPtr

freeVerifyJournalResult :: Ptr VerifyJournalResult -> IO ()
freeVerifyJournalResult resultPtr
    | resultPtr == nullPtr = pure ()
    | otherwise = do
        result <- peek resultPtr
        freeIfAllocated (errorMessage result)
        freeIfAllocated (stackTrace result)
        free resultPtr

freeIfAllocated :: CString -> IO ()
freeIfAllocated ptr
    | ptr == nullPtr = pure ()
    | otherwise = free ptr
