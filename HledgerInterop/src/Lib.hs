{-# LANGUAGE ForeignFunctionInterface #-}

module Lib (someFunc) where

foreign export ccall someFunc :: IO ()

someFunc :: IO ()
someFunc = putStrLn "someFunc"
