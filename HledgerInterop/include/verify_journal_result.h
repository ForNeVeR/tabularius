/*
SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>

SPDX-License-Identifier: MIT
*/

#pragma once

#include <stdint.h>

typedef struct VerifyJournalResult {
    int32_t record_count;
    char* error_message;
    char* stack_trace;
} VerifyJournalResult;
