/*
SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>

SPDX-License-Identifier: MIT
*/

#pragma once

#include <stdint.h>

typedef struct VerifyJournalResult {
    // Do not read this if errorMessage is not null.
    int32_t recordCount;
    // The following fields are always set up together.
    char* errorMessage;
    char* stackTrace;
} VerifyJournalResult;
