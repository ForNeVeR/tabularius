-- SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
--
-- SPDX-License-Identifier: MIT

import Control.Exception (bracket)
import Data.Int (Int32)
import System.Directory (getTemporaryDirectory, removeFile)
import System.IO (hClose, hPutStr, hSetEncoding, openTempFile, utf8)
import Test.Hspec

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

main :: IO ()
main = hspec $ do
    describe "Tabularius.verifyJournal" $ do
        it "returns the correct number of transactions" $
            withTempJournal exampleJournal $ \path -> do
                count <- Tabularius.verifyJournal path
                count `shouldBe` (2 :: Int32)
