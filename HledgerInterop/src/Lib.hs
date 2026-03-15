-- SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
--
-- SPDX-License-Identifier: MIT

{-# LANGUAGE ForeignFunctionInterface #-}

module Lib (someFunc) where

foreign export ccall someFunc :: IO ()

someFunc :: IO ()
someFunc = putStrLn "someFunc"
