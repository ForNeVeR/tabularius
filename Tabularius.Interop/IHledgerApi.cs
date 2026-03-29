using JetBrains.Annotations;
using TruePath;

namespace Tabularius.Interop;

[PublicAPI]
public interface IHledgerApi
{
    Task<int> VerifyJournal(AbsolutePath journalPath, CancellationToken cancellationToken = default);
}
