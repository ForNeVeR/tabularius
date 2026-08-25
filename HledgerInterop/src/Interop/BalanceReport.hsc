-- SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
--
-- SPDX-License-Identifier: MIT

{-# LANGUAGE ForeignFunctionInterface #-}

module Interop.BalanceReport
    ( Amount(..)
    , AmountStyle(..)
    , BalanceReportItem(..)
    , BalanceReportResult(..)
    , Decimal(..)
    , MixedAmount(..)
    , MixedAmountEntry(..)
    , MixedAmountKey(..)
    , Precision(..)
    , balanceReport
    , freeBalanceReportResult
    ) where

import Control.Exception (SomeException, onException, try)
import Data.Int (Int32)
import Data.Word (Word32, Word8)
import Foreign.C.String (CString)
import Foreign.C.Types (CBool, CInt)
import Foreign.Marshal.Alloc (free)
import Foreign.Marshal.Array (peekArray)
import Foreign.Marshal.Utils (fromBool)
import Foreign.Ptr (Ptr, nullPtr)
import Foreign.Storable (Storable(..))
import GHC.Foreign (peekCString)
import GHC.IO.Encoding (utf8)
import GHC.Stack (HasCallStack)

import qualified Data.Decimal as D
import qualified Data.Map.Strict as Map
import qualified Hledger.Data.Types as H
import qualified Hledger.Reports.BalanceReport as BR

import Interop.Common
    ( describeException
    , freeIfAllocated
    , newByteArray
    , newStruct
    , newStructArray
    , newUtf8CStringT
    , twosComplementLE
    )
import qualified Tabularius (balanceReport)

#include "balance_report_result.h"

-- This maps only MixedAmountKeyNoCost, all the other variants (MixedAmountKeyTotalCost, MixedAmountKeyUnitCost) are not
-- supported.
newtype MixedAmountKey = MixedAmountKey
    { keyCommodity :: CString
    }

-- The total value is mantissa / (10 ^ places). mantissa is encoded in minimal-length little-endian two's-complement
-- integer (same format used by System.Numerics.BigInteger::ToByteArray in .NET).
data Decimal = Decimal
    { places :: Word8
    , mantissaLength :: Word32
    , mantissa :: Ptr Word8
    }

-- Either precisionIsNatural = True, or meaningful precisionDigits.
data Precision = Precision
    { precisionDigits :: Word8
    , precisionIsNatural :: CBool
    }

data AmountStyle = AmountStyle
    { commoditySide :: CInt
    , commoditySpaced :: CBool
    , stylePrecision :: Precision
    }

-- acost and acostbasis from Hledger.Data.Types.Amount are not supported.
data Amount = Amount
    { amountCommodity :: CString
    , amountQuantity :: Decimal
    , amountStyle :: AmountStyle
    }

data MixedAmountEntry = MixedAmountEntry
    { entryKey :: MixedAmountKey
    , entryValue :: Amount
    }

data MixedAmount = MixedAmount
    { entryCount :: Int32
    , entries :: Ptr MixedAmountEntry
    }

data BalanceReportItem = BalanceReportItem
    { accountName :: CString
    , indentationSteps :: Int32
    , itemAmount :: MixedAmount
    }

data BalanceReportResult = BalanceReportResult
    { itemCount :: Int32
    , items :: Ptr BalanceReportItem
    , totals :: MixedAmount
    , errorMessage :: CString
    , stackTrace :: CString
    }

instance Storable MixedAmountKey where
    sizeOf _ = #size struct MixedAmountKey
    alignment _ = #alignment struct MixedAmountKey

    peek ptr = MixedAmountKey <$> (#peek struct MixedAmountKey, commodity) ptr

    poke ptr key = (#poke struct MixedAmountKey, commodity) ptr (keyCommodity key)

instance Storable Decimal where
    sizeOf _ = #size struct Decimal
    alignment _ = #alignment struct Decimal

    peek ptr = do
        places' <- (#peek struct Decimal, places) ptr
        mantissaLength' <- (#peek struct Decimal, mantissaLength) ptr
        mantissa' <- (#peek struct Decimal, mantissa) ptr
        pure Decimal
            { places = places'
            , mantissaLength = mantissaLength'
            , mantissa = mantissa'
            }

    poke ptr value = do
        (#poke struct Decimal, places) ptr (places value)
        (#poke struct Decimal, mantissaLength) ptr (mantissaLength value)
        (#poke struct Decimal, mantissa) ptr (mantissa value)

instance Storable Precision where
    sizeOf _ = #size struct Precision
    alignment _ = #alignment struct Precision

    peek ptr = do
        precisionDigits' <- (#peek struct Precision, precision) ptr
        precisionIsNatural' <- (#peek struct Precision, isNatural) ptr
        pure Precision
            { precisionDigits = precisionDigits'
            , precisionIsNatural = precisionIsNatural'
            }

    poke ptr value = do
        (#poke struct Precision, precision) ptr (precisionDigits value)
        (#poke struct Precision, isNatural) ptr (precisionIsNatural value)

instance Storable AmountStyle where
    sizeOf _ = #size struct AmountStyle
    alignment _ = #alignment struct AmountStyle

    peek ptr = do
        commoditySide' <- (#peek struct AmountStyle, commoditySide) ptr
        commoditySpaced' <- (#peek struct AmountStyle, commoditySpaced) ptr
        stylePrecision' <- (#peek struct AmountStyle, precision) ptr
        pure AmountStyle
            { commoditySide = commoditySide'
            , commoditySpaced = commoditySpaced'
            , stylePrecision = stylePrecision'
            }

    poke ptr value = do
        (#poke struct AmountStyle, commoditySide) ptr (commoditySide value)
        (#poke struct AmountStyle, commoditySpaced) ptr (commoditySpaced value)
        (#poke struct AmountStyle, precision) ptr (stylePrecision value)

instance Storable Amount where
    sizeOf _ = #size struct Amount
    alignment _ = #alignment struct Amount

    peek ptr = do
        amountCommodity' <- (#peek struct Amount, commodity) ptr
        amountQuantity' <- (#peek struct Amount, quantity) ptr
        amountStyle' <- (#peek struct Amount, style) ptr
        pure Amount
            { amountCommodity = amountCommodity'
            , amountQuantity = amountQuantity'
            , amountStyle = amountStyle'
            }

    poke ptr value = do
        (#poke struct Amount, commodity) ptr (amountCommodity value)
        (#poke struct Amount, quantity) ptr (amountQuantity value)
        (#poke struct Amount, style) ptr (amountStyle value)

instance Storable MixedAmountEntry where
    sizeOf _ = #size struct MixedAmountEntry
    alignment _ = #alignment struct MixedAmountEntry

    peek ptr = do
        entryKey' <- (#peek struct MixedAmountEntry, key) ptr
        entryValue' <- (#peek struct MixedAmountEntry, value) ptr
        pure MixedAmountEntry
            { entryKey = entryKey'
            , entryValue = entryValue'
            }

    poke ptr entry = do
        (#poke struct MixedAmountEntry, key) ptr (entryKey entry)
        (#poke struct MixedAmountEntry, value) ptr (entryValue entry)

instance Storable MixedAmount where
    sizeOf _ = #size struct MixedAmount
    alignment _ = #alignment struct MixedAmount

    peek ptr = do
        entryCount' <- (#peek struct MixedAmount, entryCount) ptr
        entries' <- (#peek struct MixedAmount, entries) ptr
        pure MixedAmount
            { entryCount = entryCount'
            , entries = entries'
            }

    poke ptr value = do
        (#poke struct MixedAmount, entryCount) ptr (entryCount value)
        (#poke struct MixedAmount, entries) ptr (entries value)

instance Storable BalanceReportItem where
    sizeOf _ = #size struct BalanceReportItem
    alignment _ = #alignment struct BalanceReportItem

    peek ptr = do
        accountName' <- (#peek struct BalanceReportItem, accountName) ptr
        indentationSteps' <- (#peek struct BalanceReportItem, indentationSteps) ptr
        itemAmount' <- (#peek struct BalanceReportItem, amount) ptr
        pure BalanceReportItem
            { accountName = accountName'
            , indentationSteps = indentationSteps'
            , itemAmount = itemAmount'
            }

    poke ptr item = do
        (#poke struct BalanceReportItem, accountName) ptr (accountName item)
        (#poke struct BalanceReportItem, indentationSteps) ptr (indentationSteps item)
        (#poke struct BalanceReportItem, amount) ptr (itemAmount item)

instance Storable BalanceReportResult where
    sizeOf _ = #size struct BalanceReportResult
    alignment _ = #alignment struct BalanceReportResult

    peek ptr = do
        itemCount' <- (#peek struct BalanceReportResult, itemCount) ptr
        items' <- (#peek struct BalanceReportResult, items) ptr
        totals' <- (#peek struct BalanceReportResult, totals) ptr
        errorMessage' <- (#peek struct BalanceReportResult, errorMessage) ptr
        stackTrace' <- (#peek struct BalanceReportResult, stackTrace) ptr
        pure BalanceReportResult
            { itemCount = itemCount'
            , items = items'
            , totals = totals'
            , errorMessage = errorMessage'
            , stackTrace = stackTrace'
            }

    poke ptr result = do
        (#poke struct BalanceReportResult, itemCount) ptr (itemCount result)
        (#poke struct BalanceReportResult, items) ptr (items result)
        (#poke struct BalanceReportResult, totals) ptr (totals result)
        (#poke struct BalanceReportResult, errorMessage) ptr (errorMessage result)
        (#poke struct BalanceReportResult, stackTrace) ptr (stackTrace result)

foreign export ccall balanceReport :: CString -> IO (Ptr BalanceReportResult)
foreign export ccall freeBalanceReportResult :: Ptr BalanceReportResult -> IO ()

balanceReport :: CString -> IO (Ptr BalanceReportResult)
balanceReport pathPtr = do
    hPath <- peekCString utf8 pathPtr
    buildBalanceReportResult hPath

buildBalanceReportResult :: HasCallStack => FilePath -> IO (Ptr BalanceReportResult)
buildBalanceReportResult path = do
    outcome <- (try $ Tabularius.balanceReport path >>= marshalReport)
        :: IO (Either SomeException (Ptr BalanceReportResult))
    case outcome of
        Right resultPtr -> pure resultPtr
        Left se -> do
            (messagePtr, stackPtr) <- describeException se
            newStruct BalanceReportResult
                { itemCount = -1
                , items = nullPtr
                , totals = emptyMixedAmount
                , errorMessage = messagePtr
                , stackTrace = stackPtr
                }

emptyMixedAmount :: MixedAmount
emptyMixedAmount = MixedAmount { entryCount = 0, entries = nullPtr }

-- Every step frees whatever it has already allocated if a later one fails, so that a failed marshalling leaks nothing.
marshalReport :: BR.BalanceReport -> IO (Ptr BalanceReportResult)
marshalReport (reportItems, reportTotals) = do
    marshalledItems <- marshalEach marshalItem freeItem reportItems
    (itemsPtr, count) <- newStructArray marshalledItems
        `onException` mapM_ freeItem marshalledItems
    marshalledTotals <- marshalMixedAmount reportTotals
        `onException` freeItems count itemsPtr
    newStruct BalanceReportResult
        { itemCount = count
        , items = itemsPtr
        , totals = marshalledTotals
        , errorMessage = nullPtr
        , stackTrace = nullPtr
        } `onException` (freeItems count itemsPtr >> freeMixedAmount marshalledTotals)

-- Marshals every element, freeing the already marshalled ones if any of the elements fails.
marshalEach :: (a -> IO b) -> (b -> IO ()) -> [a] -> IO [b]
marshalEach marshal freeValue = go []
    where
        go marshalled [] = pure (reverse marshalled)
        go marshalled (x:xs) = do
            value <- marshal x `onException` mapM_ freeValue marshalled
            go (value:marshalled) xs

marshalItem :: BR.BalanceReportItem -> IO BalanceReportItem
marshalItem (fullName, _elidedName, indentation, amount) = do
    namePtr <- newUtf8CStringT fullName
    marshalledAmount <- marshalMixedAmount amount `onException` freeIfAllocated namePtr
    pure BalanceReportItem
        { accountName = namePtr
        , indentationSteps = fromIntegral indentation
        , itemAmount = marshalledAmount
        }

marshalMixedAmount :: H.MixedAmount -> IO MixedAmount
marshalMixedAmount (H.Mixed amountMap) = do
    marshalledEntries <- marshalEach marshalEntry freeEntry (Map.toList amountMap)
    (entriesPtr, count) <- newStructArray marshalledEntries
        `onException` mapM_ freeEntry marshalledEntries
    pure MixedAmount
        { entryCount = count
        , entries = entriesPtr
        }

marshalEntry :: (H.MixedAmountKey, H.Amount) -> IO MixedAmountEntry
marshalEntry (key, amount) = do
    commodityPtr <- newUtf8CStringT (keyCommoditySymbol key)
    marshalledAmount <- marshalAmount amount `onException` freeIfAllocated commodityPtr
    pure MixedAmountEntry
        { entryKey = MixedAmountKey { keyCommodity = commodityPtr }
        , entryValue = marshalledAmount
        }

keyCommoditySymbol :: H.MixedAmountKey -> H.CommoditySymbol
keyCommoditySymbol (H.MixedAmountKeyNoCost commodity) = commodity
keyCommoditySymbol a@(H.MixedAmountKeyTotalCost _ _) = error("Unsupported amount: " ++ show a)
keyCommoditySymbol a@(H.MixedAmountKeyUnitCost _ _ _) = error("Unsupported amount: " ++ show a)

marshalAmount :: H.Amount -> IO Amount
marshalAmount amount = do
    case H.acost amount of
        Just cost -> error ("Unsupported amount with acost: " ++ show cost)
        Nothing -> pure ()
    case H.acostbasis amount of
        Just costbasis -> error ("Unsupported amount with acostbasis: " ++ show costbasis)
        Nothing -> pure ()

    commodityPtr <- newUtf8CStringT (H.acommodity amount)
    marshalledQuantity <- marshalQuantity (H.aquantity amount)
        `onException` freeIfAllocated commodityPtr
    pure Amount
        { amountCommodity = commodityPtr
        , amountQuantity = marshalledQuantity
        , amountStyle = marshalStyle (H.astyle amount)
        }

marshalQuantity :: H.Quantity -> IO Decimal
marshalQuantity quantity = do
    (mantissaPtr, length') <- newByteArray (twosComplementLE (D.decimalMantissa quantity))
    pure Decimal
        { places = D.decimalPlaces quantity
        , mantissaLength = length'
        , mantissa = mantissaPtr
        }

marshalStyle :: H.AmountStyle -> AmountStyle
marshalStyle style = AmountStyle
    { commoditySide = sideValue (H.ascommodityside style)
    , commoditySpaced = fromBool (H.ascommodityspaced style)
    , stylePrecision = marshalPrecision (H.asprecision style)
    }

sideValue :: H.Side -> CInt
sideValue H.L = #const Side_L
sideValue H.R = #const Side_R

marshalPrecision :: H.AmountPrecision -> Precision
marshalPrecision (H.Precision digits) = Precision
    { precisionDigits = digits
    , precisionIsNatural = fromBool False
    }
marshalPrecision H.NaturalPrecision = Precision
    { precisionDigits = 0
    , precisionIsNatural = fromBool True
    }

freeBalanceReportResult :: Ptr BalanceReportResult -> IO ()
freeBalanceReportResult resultPtr
    | resultPtr == nullPtr = pure ()
    | otherwise = do
        result <- peek resultPtr
        freeItems (itemCount result) (items result)
        freeMixedAmount (totals result)
        freeIfAllocated (errorMessage result)
        freeIfAllocated (stackTrace result)
        free resultPtr

freeItems :: Int32 -> Ptr BalanceReportItem -> IO ()
freeItems count itemsPtr
    | itemsPtr == nullPtr = pure ()
    | otherwise = do
        peekArray (fromIntegral count) itemsPtr >>= mapM_ freeItem
        free itemsPtr

freeItem :: BalanceReportItem -> IO ()
freeItem item = do
    freeIfAllocated (accountName item)
    freeMixedAmount (itemAmount item)

freeMixedAmount :: MixedAmount -> IO ()
freeMixedAmount amount
    | entries amount == nullPtr = pure ()
    | otherwise = do
        peekArray (fromIntegral (entryCount amount)) (entries amount) >>= mapM_ freeEntry
        free (entries amount)

freeEntry :: MixedAmountEntry -> IO ()
freeEntry entry = do
    freeIfAllocated (keyCommodity (entryKey entry))
    freeIfAllocated (amountCommodity (entryValue entry))
    freeIfAllocated (mantissa (amountQuantity (entryValue entry)))
