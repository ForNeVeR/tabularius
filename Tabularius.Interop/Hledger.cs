// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;
using JetBrains.Annotations;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using TruePath;

namespace Tabularius.Interop;

/// <remarks>
/// <para>This class incapsulates a single thread that will be used to talk with the GHC runtime.</para>
/// <para>
///     This helps to conserve system resources, since otherwise every thread calling to GHC functions will use some
///     resources, see <a href="https://ghc.gitlab.haskell.org/ghc/doc/users_guide/exts/ffi.html#hs-thread-done">the
///     notes</a> on <c>hs_thread_done</c> function.
/// </para>
/// </remarks>
[PublicAPI]
public class Hledger : IHledgerApi
{
    private static readonly Lock Lock = new();
    private static bool _initialized;

    private readonly SingleThreadScheduler _scheduler;
    private readonly TaskScheduler _taskScheduler;
    private Hledger(Lifetime lifetime)
    {
        _scheduler = SingleThreadScheduler.RunOnSeparateThread(lifetime, "Hledger", __ =>
        {
            using var _ = Lock.EnterScope();
            if (_initialized) throw new InvalidOperationException(
                "Multiple initialization attempts are not supported by the GHC runtime.");

            unsafe { HledgerInterop.HsInit(null, null); }
            lifetime.OnTermination(HledgerInterop.HsExit);

            _initialized = true;
        });
        _taskScheduler = _scheduler.AsTaskScheduler();
    }

    public static Hledger Initialize(Lifetime lifetime) => new(lifetime);

    private Task<T> RunTask<T>(Func<T> func, CancellationToken cancellationToken)
    {
        return Task.Factory.StartNew(
            func,
            cancellationToken,
            TaskCreationOptions.RunContinuationsAsynchronously,
            _taskScheduler);
    }

    public unsafe Task<int> VerifyJournal(AbsolutePath journalPath, CancellationToken cancellationToken = default) =>
        RunTask(() =>
        {
            var result = HledgerInterop.VerifyJournal(journalPath.Value);
            try
            {
                if (result->error_message != null)
                {
                    var errorMessage = Marshal.PtrToStringUTF8((IntPtr)result->error_message);
                    var stackTrace = Marshal.PtrToStringUTF8((IntPtr)result->stack_trace);
                    throw new HledgerException(errorMessage ?? "[NO MESSAGE]", stackTrace ?? "[NO STACK TRACE]");
                }

                return result->record_count;
            }
            finally
            {
                HledgerInterop.FreeVerifyJournalResult(result);
            }
        }, cancellationToken);
}
