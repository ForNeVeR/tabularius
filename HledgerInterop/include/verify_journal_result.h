/*
SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>

SPDX-License-Identifier: MIT
*/

#ifndef VERIFY_JOURNAL_RESULT_H
#define VERIFY_JOURNAL_RESULT_H

#include <stdint.h>

typedef struct VerifyJournalResult {
    int32_t record_count;
    char* error_message;
    char* stack_trace;
} VerifyJournalResult;

VerifyJournalResult* verifyJournal(const char* path);
void freeVerifyJournalResult(VerifyJournalResult* result);

#endif
