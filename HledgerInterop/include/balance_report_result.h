/*
SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>

SPDX-License-Identifier: MIT
*/

#include <stdint.h>

// NOTE: Actual MixedAmountKey has MixedAmountKeyTotalCost and MixedAmountKeyUnitCost, for now we ignore those, and this
//       type represents only the MixedAmountKeyNoCost with its commodity symbol.
typedef struct MixedAmountKey {
    char *commodity;
} MixedAmountKey;

// The full value is mantissa / (10 ^ places)
typedef struct Decimal {
    uint8_t places;
    uint32_t mantissaLength;
    uint8_t *mantissa;
} Decimal;

typedef struct Precision {
    // Show this many decimal digits (0..255). Ignore if isNatural = true.
    uint8_t precision;

    // show all significant decimal digits stored internally.
    _Bool isNatural;
} Precision;

typedef enum Side {
    Side_L,
    Side_R
} Side;

typedef struct AmountStyle {
    Side commoditySide;
    _Bool commoditySpaced;
    Precision precision;
} AmountStyle;

typedef struct Amount {
    char *commodity;
    Decimal quantity;
    AmountStyle style;
} Amount;

typedef struct MixedAmountEntry {
    MixedAmountKey key;
    Amount value;
} MixedAmountEntry;

typedef struct MixedAmount {
    int32_t entryCount;
    MixedAmountEntry *entries;
} MixedAmount;

typedef struct BalanceReportItem {
    char *accountName;
    int32_t indentationSteps;
    MixedAmount amount;
} BalanceReportItem;

typedef struct BalanceReportResult {
    int32_t itemCount;
    BalanceReportItem *items;
    MixedAmount totals;

    // The following fields are always set up together.
    char *error_message;
    char *stack_trace;
} BalanceReportResult;
