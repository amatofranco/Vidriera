using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Catalogs;

namespace Vidriera.Application.Tests.Catalogs;

public class CatalogHdPagesGeneratorTests
{
    private static readonly Guid CompanyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CatalogId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static string HdKey(int page) => $"companies/{CompanyId}/catalogs/{CatalogId}/pages/{page}-hd.jpg";

    private static CatalogHdPagesGenerator Build(
        FakeStorage storage,
        CatalogGenerationGate? gate = null,
        Func<int, bool>? isStillCurrentAfterUploads = null,
        Func<bool>? hasPendingJob = null,
        TimeSpan? checkInterval = null) =>
        new(
            storage,
            new FakeRasterizer(),
            gate ?? new CatalogGenerationGate(),
            _ => Task.FromResult(isStillCurrentAfterUploads?.Invoke(storage.Uploaded.Count) ?? true),
            _ => Task.FromResult(hasPendingJob?.Invoke() ?? false),
            checkInterval ?? TimeSpan.Zero);

    [Fact]
    public async Task UploadsEveryPage_WhenNothingInterrupts()
    {
        var storage = new FakeStorage();

        var result = await Build(storage).GenerateAsync(CompanyId, CatalogId, [], 5, CancellationToken.None);

        Assert.Equal(new HdPagesResult(HdPagesOutcome.Completed, 5), result);
        Assert.Equal(Enumerable.Range(1, 5).Select(HdKey).ToHashSet(), storage.Existing);
    }

    [Fact]
    public async Task StopsAndDeletesWhatItUploaded_WhenTheCatalogIsReplacedMidway()
    {
        var storage = new FakeStorage();

        var result = await Build(storage, isStillCurrentAfterUploads: uploaded => uploaded < 3)
            .GenerateAsync(CompanyId, CatalogId, [], 10, CancellationToken.None);

        Assert.Equal(new HdPagesResult(HdPagesOutcome.CatalogReplaced, 3), result);
        Assert.Empty(storage.Existing);
    }

    [Fact]
    public async Task DeletesEverything_WhenTheCatalogWasReplacedRightAtTheEnd()
    {
        var storage = new FakeStorage();

        var result = await Build(storage, isStillCurrentAfterUploads: uploaded => uploaded < 4, checkInterval: TimeSpan.FromHours(1))
            .GenerateAsync(CompanyId, CatalogId, [], 4, CancellationToken.None);

        Assert.Equal(new HdPagesResult(HdPagesOutcome.CatalogReplaced, 4), result);
        Assert.Empty(storage.Existing);
    }

    [Fact]
    public async Task YieldsAndKeepsWhatItUploaded_WhenAGenerationIsWaitingForTheGate()
    {
        var storage = new FakeStorage();
        var gate = new CatalogGenerationGate();
        Assert.True(await gate.TryEnterAsync(TimeSpan.Zero, CancellationToken.None));
        var waiter = gate.TryEnterAsync(TimeSpan.FromSeconds(5), CancellationToken.None);

        var result = await Build(storage, gate).GenerateAsync(CompanyId, CatalogId, [], 10, CancellationToken.None);

        Assert.Equal(new HdPagesResult(HdPagesOutcome.YieldedToGeneration, 1), result);
        Assert.Equal([HdKey(1)], storage.Existing);
        gate.Exit();
        Assert.True(await waiter);
        gate.Exit();
    }

    [Fact]
    public async Task YieldsAndKeepsWhatItUploaded_WhenAJobIsPending()
    {
        var storage = new FakeStorage();

        var result = await Build(storage, hasPendingJob: () => storage.Uploaded.Count >= 2)
            .GenerateAsync(CompanyId, CatalogId, [], 10, CancellationToken.None);

        Assert.Equal(new HdPagesResult(HdPagesOutcome.YieldedToGeneration, 2), result);
        Assert.Equal([HdKey(1), HdKey(2)], storage.Existing.OrderBy(k => k));
    }

    [Fact]
    public async Task OnlyChecksTheDatabaseEveryInterval()
    {
        var storage = new FakeStorage();
        var checks = 0;
        var generator = new CatalogHdPagesGenerator(
            storage,
            new FakeRasterizer(),
            new CatalogGenerationGate(),
            _ => { checks++; return Task.FromResult(true); },
            _ => Task.FromResult(false),
            TimeSpan.FromHours(1));

        await generator.GenerateAsync(CompanyId, CatalogId, [], 20, CancellationToken.None);

        Assert.Equal(1, checks);
    }

    private sealed class FakeRasterizer : IPdfRasterizerService
    {
        public IAsyncEnumerable<(int PageIndex, byte[] ImageBytes, int Width, int Height)> RasterizePagesToImagesAsync(
            byte[] pdfBytes, IReadOnlyList<int> pageIndices, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public async IAsyncEnumerable<(int PageIndex, byte[] ImageBytes, int Width, int Height)> RasterizePagesToHdImagesAsync(
            byte[] pdfBytes, IReadOnlyList<int> pageIndices, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var index in pageIndices)
            {
                await Task.Yield();
                yield return (index, [1, 2, 3], 10, 14);
            }
        }
    }

    private sealed class FakeStorage : IBlobStorageService
    {
        private readonly ConcurrentDictionary<string, byte> _existing = new();

        public List<string> Uploaded { get; } = [];

        public IReadOnlySet<string> Existing => _existing.Keys.ToHashSet();

        public Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken)
        {
            Uploaded.Add(key);
            _existing[key] = 0;
            return Task.FromResult(key);
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            _existing.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken) => Task.FromResult(_existing.ContainsKey(key));

        public Task<Stream> DownloadAsync(string key, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<(Stream Content, string ContentType)> DownloadWithContentTypeAsync(string key, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<long> GetSizeAsync(string key, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
