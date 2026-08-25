-- SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
--
-- SPDX-License-Identifier: MIT

module Interop.BalanceReportSpec (spec) where

import Control.Exception (bracket)
import Data.Decimal (Decimal, DecimalRaw(..))
import Data.List (isInfixOf)
import Foreign.Marshal.Array (peekArray)
import Foreign.Ptr (nullPtr)
import Foreign.Storable (peek)
import GHC.Foreign (peekCString, withCString)
import GHC.IO.Encoding (utf8)
import Test.Hspec

import qualified Data.Map.Strict as Map
import qualified Data.Text as T
import qualified Hledger.Data.Types as H

import TestFramework (decodeTwosComplementLE, exampleJournal, peekOptionalUtf8, withMissingJournalPath, withTempJournal)
import qualified Interop.BalanceReport as Interop
import qualified Tabularius

withBalanceReportResult :: FilePath -> (Interop.BalanceReportResult -> IO a) -> IO a
withBalanceReportResult path action =
    withCString utf8 path $ \pathPtr ->
        bracket
            (Interop.balanceReport pathPtr)
            Interop.freeBalanceReportResult
            (\resultPtr -> peek resultPtr >>= action)

readItems :: Interop.BalanceReportResult -> IO [Interop.BalanceReportItem]
readItems result = peekArray (fromIntegral (Interop.itemCount result)) (Interop.items result)

readEntries :: Interop.MixedAmount -> IO [(String, String, Decimal)]
readEntries mixed
    | Interop.entries mixed == nullPtr = pure []
    | otherwise = peekArray (fromIntegral (Interop.entryCount mixed)) (Interop.entries mixed) >>= mapM readEntry

readEntry :: Interop.MixedAmountEntry -> IO (String, String, Decimal)
readEntry entry = do
    let amount = Interop.entryValue entry
    keyCommodity <- peekCString utf8 (Interop.keyCommodity (Interop.entryKey entry))
    commodity <- peekCString utf8 (Interop.amountCommodity amount)
    quantity <- readQuantity (Interop.amountQuantity amount)
    pure (keyCommodity, commodity, quantity)

readQuantity :: Interop.Decimal -> IO Decimal
readQuantity value = do
    bytes <- peekArray (fromIntegral (Interop.mantissaLength value)) (Interop.mantissa value)
    pure $ Decimal (Interop.places value) (decodeTwosComplementLE bytes)

quantities :: H.MixedAmount -> [H.Quantity]
quantities (H.Mixed amountMap) = map H.aquantity (Map.elems amountMap)

spec :: Spec
spec = do
    describe "Tabularius.balanceReport" $ do
        it "returns the expected accounts" $
            withTempJournal exampleJournal $ \path -> do
                (reportItems, _) <- Tabularius.balanceReport path
                map (\(name, _, _, _) -> T.unpack name) reportItems `shouldBe`
                    [ "assets:ing"
                    , "equity:opening/closing balances"
                    , "expenses:goods"
                    ]

        it "returns the expected quantities" $
            withTempJournal exampleJournal $ \path -> do
                (reportItems, reportTotals) <- Tabularius.balanceReport path
                map (\(_, _, _, amount) -> quantities amount) reportItems `shouldBe`
                    [[9900], [-10000], [100]]
                quantities reportTotals `shouldSatisfy` all (== 0)

    describe "Interop.balanceReport" $ do
        it "marshals every report item" $
            withTempJournal exampleJournal $ \path ->
                withBalanceReportResult path $ \result -> do
                    Interop.itemCount result `shouldBe` 3
                    reportItems <- readItems result
                    names <- mapM (peekCString utf8 . Interop.accountName) reportItems
                    names `shouldBe`
                        [ "assets:ing"
                        , "equity:opening/closing balances"
                        , "expenses:goods"
                        ]
                    map Interop.indentationSteps reportItems `shouldSatisfy` all (>= 0)

        it "marshals the amounts, keeping the keys paired with the values" $
            withTempJournal exampleJournal $ \path ->
                withBalanceReportResult path $ \result -> do
                    reportItems <- readItems result
                    itemEntries <- mapM (readEntries . Interop.itemAmount) reportItems
                    itemEntries `shouldBe`
                        [ [("BTC", "BTC", 9900)]
                        , [("BTC", "BTC", -10000)]
                        , [("BTC", "BTC", 100)]
                        ]

        it "marshals the totals" $
            withTempJournal exampleJournal $ \path ->
                withBalanceReportResult path $ \result -> do
                    totalEntries <- readEntries (Interop.totals result)
                    map (\(_, _, quantity) -> quantity) totalEntries `shouldSatisfy` all (== 0)

        it "marshals the amount styles" $
            withTempJournal exampleJournal $ \path ->
                withBalanceReportResult path $ \result -> do
                    reportItems <- readItems result
                    case reportItems of
                        [] -> expectationFailure "The report contains no items."
                        (item : _) -> do
                            Interop.entryCount (Interop.itemAmount item) `shouldBe` 1
                            firstEntry <- peek (Interop.entries (Interop.itemAmount item))
                            let style = Interop.amountStyle (Interop.entryValue firstEntry)
                            Interop.commoditySide style `shouldBe` 1  -- Side_R
                            Interop.commoditySpaced style `shouldBe` 1
                            Interop.precisionIsNatural (Interop.stylePrecision style) `shouldBe` 0
                            Interop.precisionDigits (Interop.stylePrecision style) `shouldBe` 0

        it "returns error details and a stack trace on failure" $
            withMissingJournalPath $ \path ->
                withBalanceReportResult path $ \result -> do
                    Interop.itemCount result `shouldBe` (-1)
                    Interop.items result `shouldBe` nullPtr
                    Interop.entryCount (Interop.totals result) `shouldBe` 0
                    Interop.entries (Interop.totals result) `shouldBe` nullPtr
                    errorText <- peekOptionalUtf8 (Interop.errorMessage result)
                    stackText <- peekOptionalUtf8 (Interop.stackTrace result)
                    errorText `shouldSatisfy` maybe False (not . null)
                    stackText `shouldSatisfy` maybe False (isInfixOf "CallStack (from HasCallStack)")
                    stackText `shouldSatisfy` maybe False (isInfixOf "buildBalanceReportResult")

    describe "Interop.freeBalanceReportResult" $
        it "ignores a null pointer" $
            Interop.freeBalanceReportResult nullPtr
