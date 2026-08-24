-- SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
--
-- SPDX-License-Identifier: MIT

module Interop.Common
    ( describeException
    , freeIfAllocated
    , newByteArray
    , newStruct
    , newStructArray
    , newUtf8CString
    , newUtf8CStringT
    , renderStackTrace
    , twosComplementLE
    ) where

import Control.Exception (SomeException(..), displayException, someExceptionContext)
import Control.Exception.Context (displayExceptionContext)
import Data.Bits (shiftR, testBit, (.&.))
import Data.Char (isSpace)
import Data.Int (Int32)
import Data.List (intercalate)
import Data.Text (Text, unpack)
import Data.Word (Word32, Word8)
import Foreign.C.String (CString)
import Foreign.Marshal.Alloc (free, malloc)
import Foreign.Marshal.Array (newArray)
import Foreign.Ptr (Ptr, nullPtr)
import Foreign.Storable (Storable(..))
import GHC.Foreign (newCString)
import GHC.IO.Encoding (utf8)
import GHC.Stack (HasCallStack, callStack, prettyCallStack)

newUtf8CString :: String -> IO CString
newUtf8CString = newCString utf8

newUtf8CStringT :: Text -> IO CString
newUtf8CStringT = newUtf8CString . unpack

freeIfAllocated :: Ptr a -> IO ()
freeIfAllocated ptr
    | ptr == nullPtr = pure ()
    | otherwise = free ptr

newStruct :: Storable a => a -> IO (Ptr a)
newStruct value = do
    ptr <- malloc
    poke ptr value
    pure ptr

-- An empty list is represented as a null pointer of length zero.
newStructArray :: Storable a => [a] -> IO (Ptr a, Int32)
newStructArray [] = pure (nullPtr, 0)
newStructArray values = do
    ptr <- newArray values
    pure (ptr, fromIntegral (length values))

newByteArray :: [Word8] -> IO (Ptr Word8, Word32)
newByteArray [] = pure (nullPtr, 0)
newByteArray bytes = do
    ptr <- newArray bytes
    pure (ptr, fromIntegral (length bytes))

-- Returns a pair of freshly allocated C strings: (message, stack trace).
describeException :: HasCallStack => SomeException -> IO (CString, CString)
describeException se@(SomeException e) = do
    messagePtr <- newUtf8CString (displayException e)
    stackPtr <- newUtf8CString (renderStackTrace se)
    pure (messagePtr, stackPtr)

renderStackTrace :: HasCallStack => SomeException -> String
renderStackTrace se =
    let callStackText = prettyCallStack callStack
        contextText = displayExceptionContext (someExceptionContext se)
        sections = filter (not . all isSpace) [callStackText, contextText]
    in if null sections
        then "No Haskell stack trace is available."
        else intercalate "\n\n" sections

-- Encodes an integer as a minimal-length little-endian two's-complement byte sequence, i.e. the same representation as
-- System.Numerics.BigInteger.ToByteArray() produces.
twosComplementLE :: Integer -> [Word8]
twosComplementLE value =
    let byte = fromIntegral (value .&. 0xFF) :: Word8
        rest = value `shiftR` 8
        isLast = (rest == 0 && not (testBit byte 7)) || (rest == -1 && testBit byte 7)
    in if isLast then [byte] else byte : twosComplementLE rest
