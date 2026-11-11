using Microsoft.Extensions.Logging.Abstractions;
using Vidriera.Application.Catalogs;

namespace Vidriera.Application.Tests.Catalogs;

public class CatalogJobProgressReporterTests
{
    [Fact]
    public async Task Report_DoesNotWaitForTheSave()
    {
        var release = new TaskCompletionSource();
        var reporter = new CatalogJobProgressReporter(_ => release.Task, TimeSpan.Zero, NullLogger.Instance);

        reporter.Report(new("rasterizing", 1, 10));

        Assert.False(reporter.FlushAsync().IsCompleted);
        release.SetResult();
        await reporter.FlushAsync();
    }

    [Fact]
    public async Task WhileASaveIsInFlight_OnlyTheLatestProgressIsSavedNext()
    {
        var saved = new List<int>();
        var firstSaveStarted = new TaskCompletionSource();
        var firstSave = new TaskCompletionSource();
        var reporter = new CatalogJobProgressReporter(
            p =>
            {
                saved.Add(p.Current);
                if (p.Current != 1)
                {
                    return Task.CompletedTask;
                }

                firstSaveStarted.SetResult();
                return firstSave.Task;
            },
            TimeSpan.Zero,
            NullLogger.Instance);

        reporter.Report(new("rasterizing", 1, 10));
        await firstSaveStarted.Task;
        reporter.Report(new("rasterizing", 2, 10));
        reporter.Report(new("rasterizing", 3, 10));
        reporter.Report(new("rasterizing", 4, 10));
        firstSave.SetResult();
        await reporter.FlushAsync();

        Assert.Equal([1, 4], saved);
    }

    [Fact]
    public async Task ReportAfterTheLoopFinished_StartsANewSave()
    {
        var saved = new List<int>();
        var reporter = new CatalogJobProgressReporter(
            p => { saved.Add(p.Current); return Task.CompletedTask; }, TimeSpan.Zero, NullLogger.Instance);

        reporter.Report(new("rasterizing", 1, 10));
        await reporter.FlushAsync();
        reporter.Report(new("rasterizing", 2, 10));
        await reporter.FlushAsync();

        Assert.Equal([1, 2], saved);
    }

    [Fact]
    public async Task AFailedSave_DoesNotStopLaterSaves()
    {
        var saved = new List<int>();
        var reporter = new CatalogJobProgressReporter(
            p =>
            {
                saved.Add(p.Current);
                return p.Current == 1 ? Task.FromException(new InvalidOperationException()) : Task.CompletedTask;
            },
            TimeSpan.Zero,
            NullLogger.Instance);

        reporter.Report(new("rasterizing", 1, 10));
        await reporter.FlushAsync();
        reporter.Report(new("rasterizing", 2, 10));
        await reporter.FlushAsync();

        Assert.Equal([1, 2], saved);
    }
}
