-- SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
--
-- SPDX-License-Identifier: MIT

module Interop.VerifyJournalSpec (spec) where

import Control.Exception (bracket)
import Control.Monad ((>=>))
import Data.List (isInfixOf)
import Foreign.Storable (peek)
import GHC.Foreign (withCString)
import GHC.IO.Encoding (utf8)
import Test.Hspec

import TestSupport (exampleJournal, peekOptionalUtf8, utf8Journal, withMissingJournalPath, withTempJournal)
import qualified Interop.VerifyJournal as Interop
import qualified Tabularius

withInteropResult :: FilePath -> (Interop.VerifyJournalResult -> IO a) -> IO a
withInteropResult path action =
    withCString utf8 path $ \pathPtr ->
        bracket
            (Interop.verifyJournal pathPtr)
            Interop.freeVerifyJournalResult
            (peek >=> action)

spec :: Spec
spec = do
    describe "Tabularius.verifyJournal" $ do
        it "returns the correct number of transactions" $
            withTempJournal exampleJournal $ \path -> do
                count <- Tabularius.verifyJournal path
                count `shouldBe` 2

        it "reads a journal with UTF-8 content (Russian and Chinese)" $
            withTempJournal utf8Journal $ \path -> do
                count <- Tabularius.verifyJournal path
                count `shouldBe` 2

    describe "Interop.verifyJournal" $ do
        it "returns a structured success result" $
            withTempJournal exampleJournal $ \path ->
                withInteropResult path $ \result -> do
                    Interop.recordCount result `shouldBe` 2
                    errorText <- peekOptionalUtf8 (Interop.errorMessage result)
                    stackText <- peekOptionalUtf8 (Interop.stackTrace result)
                    errorText `shouldBe` Nothing
                    stackText `shouldBe` Nothing

        it "returns error details and a stack trace on failure" $
            withMissingJournalPath $ \path ->
                withInteropResult path $ \result -> do
                    Interop.recordCount result `shouldBe` (-1)
                    errorText <- peekOptionalUtf8 (Interop.errorMessage result)
                    stackText <- peekOptionalUtf8 (Interop.stackTrace result)
                    errorText `shouldSatisfy` maybe False (not . null)
                    errorText `shouldSatisfy` maybe True (not . isInfixOf "HasCallStack backtrace")
                    errorText `shouldSatisfy` maybe True (not . isInfixOf "CallStack (from HasCallStack)")
                    stackText `shouldSatisfy` maybe False (not . null)
                    stackText `shouldSatisfy` maybe False (isInfixOf "CallStack (from HasCallStack)")
                    stackText `shouldSatisfy` maybe False (isInfixOf "buildVerifyJournalResult")
