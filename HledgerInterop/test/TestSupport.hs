-- SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
--
-- SPDX-License-Identifier: MIT

module TestSupport
    ( decodeTwosComplementLE
    , exampleJournal
    , peekOptionalUtf8
    , utf8Journal
    , withMissingJournalPath
    , withTempJournal
    ) where

import Control.Exception (bracket)
import Data.Bits (shiftL, testBit, (.|.))
import Data.Word (Word8)
import Foreign.C.String (CString)
import Foreign.Ptr (nullPtr)
import GHC.Foreign (peekCString)
import System.Directory (getTemporaryDirectory, removeFile)
import System.IO (hClose, hPutStr, hSetEncoding, openTempFile, utf8)

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

-- Journal with Russian and Chinese text in transaction names.
-- No single-byte encoding covers both scripts, so this verifies true UTF-8 reading.
utf8Journal :: String
utf8Journal = unlines
    [ "2026-01-01 \1055\1088\1080\1074\1077\1090"  -- "Привет" (Russian)
    , "    assets:ing  100 BTC"
    , "    equity:opening/closing balances"
    , ""
    , "2026-01-02 \20320\22909"  -- "你好" (Chinese)
    , "    assets:ing  -50 BTC"
    , "    expenses:goods  50 BTC"
    ]

withTempJournal :: String -> (FilePath -> IO a) -> IO a
withTempJournal content = bracket acquire removeFile
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

peekOptionalUtf8 :: CString -> IO (Maybe String)
peekOptionalUtf8 ptr
    | ptr == nullPtr = pure Nothing
    | otherwise = Just <$> peekCString utf8 ptr

-- Independent reimplementation of the mantissa decoding, to check the encoding produced by Interop.Common.
decodeTwosComplementLE :: [Word8] -> Integer
decodeTwosComplementLE [] = 0
decodeTwosComplementLE bytes =
    let magnitude = foldl' (\acc byte -> (acc `shiftL` 8) .|. fromIntegral byte) 0 (reverse bytes)
    in if testBit (last bytes) 7
        then magnitude - (1 `shiftL` (8 * length bytes))
        else magnitude
