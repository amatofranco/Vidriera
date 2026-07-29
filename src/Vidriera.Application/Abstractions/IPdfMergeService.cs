namespace Vidriera.Application.Abstractions;

public interface IPdfMergeService
{
    Task<byte[]> MergeAsync(IReadOnlyList<byte[]> pdfsInOrder, CancellationToken cancellationToken);
}
