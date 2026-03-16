-- SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
--
-- SPDX-License-Identifier: MIT

{-# LANGUAGE ForeignFunctionInterface #-}

module Lib (adder) where

import Data.Int (Int32)

foreign export ccall adder :: Int32 -> Int32 -> Int32

adder :: Int32 -> Int32 -> Int32
adder a b = a + b
