using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using WitcherKM.Workflow.Domain;
using WitcherKM.Workflow.Infrastructure;

var directory = Path.Combine(Path.GetTempPath(), "workflow-image-cache-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
try { await Run(directory); }
finally
{
    SqliteConnection.ClearAllPools();
    var full = Path.GetFullPath(directory);
    var tempRoot = Path.GetFullPath(Path.GetTempPath());
    if (full.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase) && Directory.Exists(full))
    {
        try { Directory.Delete(full, recursive: true); }
        catch (IOException) { Console.Error.WriteLine($"Temporary test files remain at {full}"); }
    }
}

static async Task Run(string directory)
{
    var options = new DbContextOptionsBuilder<WorkflowDbContext>()
        .UseSqlite($"Data Source={Path.Combine(directory, "test.db")}").Options;
    await using var db = new WorkflowDbContext(options);
    await db.Database.MigrateAsync();
    var store = new WorkflowStore(db);
    var fetcher = new FakeFetcher();
    var images = new LocalImageStorage(directory, store, fetcher);
    const string source = "https://images.example.test/picture.png?v=1";
    const string otherVersion = "https://images.example.test/picture.png?v=2";

    var first = await images.Localize($"![Первый]({source}) и ![Второй]({source})", default);
    var local = first.Markdown.Split('(', ')')[1];
    Check(first.Warnings.Count == 0 && first.Markdown.Contains($"![Второй]({local})"), "one source is reused within a step");
    Check(fetcher.Calls == 1, "one download for repeated source");

    var second = await images.Localize($"![Ещё]({source})", default);
    Check(second.Markdown.Contains(local) && fetcher.Calls == 1, "cache is reused across steps");
    var changed = await images.Localize($"![Версия]({otherVersion})", default);
    Check(fetcher.Calls == 2 && !changed.Markdown.Contains(local), "query parameters distinguish sources");

    var category = new Category(Guid.NewGuid(), "Тест");
    await store.SaveCategory(category, default);
    var chain = new ActionChain(Guid.NewGuid(), "Проверка изображений", [category.Id]);
    var a = chain.AddStep(null, first.Markdown);
    var b = chain.AddStep(null, second.Markdown);
    await store.SaveChain(chain, default);
    var fileName = Path.GetFileName(local);
    Check(images.Get(fileName) is not null, "downloaded file exists");

    chain.RemoveStep(a.Id);
    await store.SaveChain(chain, default);
    await images.DeleteUnreferenced(a.Description, default);
    Check(images.Get(fileName) is not null, "file remains while another step references it");

    chain.RemoveStep(b.Id);
    await store.SaveChain(chain, default);
    await images.DeleteUnreferenced(b.Description, default);
    Check(images.Get(fileName) is null, "file is removed after last reference");
    await images.Localize($"![После удаления]({source})", default);
    Check(fetcher.Calls == 3, "removed source is downloaded again");
    Console.WriteLine("Image cache smoke passed (7 checks).");
}

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

sealed class FakeFetcher : IExternalImageFetcher
{
    public int Calls { get; private set; }
    public Task<(byte[] Bytes, string ContentType)> Download(string sourceUrl, CancellationToken ct)
    {
        Calls++;
        var bytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/lAAAAABJRU5ErkJggg==");
        return Task.FromResult((bytes, "image/png"));
    }
}
