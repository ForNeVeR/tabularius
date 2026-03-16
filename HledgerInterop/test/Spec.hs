-- SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
--
-- SPDX-License-Identifier: MIT

import Lib (adder)
import Test.Hspec

main :: IO ()
main = hspec $ do
  describe "adder" $ do
    it "adds two positive numbers" $
      adder 1 2 `shouldBe` 3
    it "adds zeros" $
      adder 0 0 `shouldBe` 0
