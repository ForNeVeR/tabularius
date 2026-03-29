/*
SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>

SPDX-License-Identifier: MIT
*/

#pragma once

#include <stdint.h>

typedef struct VerifyJournalResult {
    // Do not read this if error_message is not null.
    int32_t record_count;
    // The following fields are always set up together.
    char* error_message;
    char* stack_trace;
} VerifyJournalResult;
