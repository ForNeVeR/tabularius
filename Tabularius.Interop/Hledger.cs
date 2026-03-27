// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

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
public class Hledger
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

    public Task<int> VerifyJournal(AbsolutePath journalPath, CancellationToken cancellationToken = default) =>
        RunTask(() => HledgerInterop.VerifyJournal(journalPath.Value), cancellationToken);
}
