-- SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
--
-- SPDX-License-Identifier: MIT

import Control.Exception (bracket)
import Control.Monad ((>=>))
import Data.Int (Int32)
import Foreign.C.String (CString)
import Foreign.Ptr (nullPtr)
import Foreign.Storable (peek)
import GHC.Foreign (peekCString, withCString)
import System.Directory (getTemporaryDirectory, removeFile)
import System.IO (hClose, hPutStr, hSetEncoding, openTempFile, utf8)
import Test.Hspec

import qualified Interop
import qualified Tabularius

exampleJournal :: String
exampleJournal = unlines
    [ "2026-01-01 Opening balances"
    , "    assets:ing  10000 BTC"
    , "    equity:opening/closing balances"
    , ""
    , "2026-01-02 Tabularius"
    , "    assets:ing     -100 BTC = 9900 BTC"
    , "    expenses:goods  100 BTC"
    ]

withTempJournal :: String -> (FilePath -> IO a) -> IO a
withTempJournal content action =
    bracket acquire removeFile action
  where
    acquire = do
        tmpDir <- getTemporaryDirectory
        (path, h) <- openTempFile tmpDir "tabularius.journal"
        hSetEncoding h utf8
        hPutStr h content
        hClose h
        return path

withMissingJournalPath :: (FilePath -> IO a) -> IO a
withMissingJournalPath action = do
    tmpDir <- getTemporaryDirectory
    (path, h) <- openTempFile tmpDir "missing.journal"
    hClose h
    removeFile path
    action path

withInteropResult :: FilePath -> (Interop.VerifyJournalResult -> IO a) -> IO a
withInteropResult path action =
    withCString utf8 path $ \pathPtr ->
        bracket
            (Interop.verifyJournal pathPtr)
            Interop.freeVerifyJournalResult
            (peek >=> action)

peekOptionalUtf8 :: CString -> IO (Maybe String)
peekOptionalUtf8 ptr
    | ptr == nullPtr = pure Nothing
    | otherwise = Just <$> peekCString utf8 ptr

main :: IO ()
main = hspec $ do
    describe "Tabularius.verifyJournal" $ do
        it "returns the correct number of transactions" $
            withTempJournal exampleJournal $ \path -> do
                count <- Tabularius.verifyJournal path
                count `shouldBe` (2 :: Int32)

    describe "Interop.verifyJournal" $ do
        it "returns a structured success result" $
            withTempJournal exampleJournal $ \path ->
                withInteropResult path $ \result -> do
                    Interop.recordCount result `shouldBe` (2 :: Int32)
                    errorText <- peekOptionalUtf8 (Interop.errorMessage result)
                    stackText <- peekOptionalUtf8 (Interop.stackTrace result)
                    errorText `shouldBe` Nothing
                    stackText `shouldBe` Nothing

        it "returns error details and a stack trace on failure" $
            withMissingJournalPath $ \path ->
                withInteropResult path $ \result -> do
                    Interop.recordCount result `shouldBe` (-1 :: Int32)
                    errorText <- peekOptionalUtf8 (Interop.errorMessage result)
                    stackText <- peekOptionalUtf8 (Interop.stackTrace result)
                    errorText `shouldSatisfy` maybe False (not . null)
                    stackText `shouldSatisfy` maybe False (not . null)
