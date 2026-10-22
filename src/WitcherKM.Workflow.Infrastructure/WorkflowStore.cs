using Microsoft.EntityFrameworkCore;
using WitcherKM.Workflow.Application;
using WitcherKM.Workflow.Domain;

namespace WitcherKM.Workflow.Infrastructure;

public sealed class WorkflowDbContext(DbContextOptions<WorkflowDbContext> options) : DbContext(options)
{
    public DbSet<CategoryRow> Categories => Set<CategoryRow>();
    public DbSet<ChainRow> Chains => Set<ChainRow>();
    public DbSet<ChainCategoryRow> ChainCategories => Set<ChainCategoryRow>();
    public DbSet<StepRow> Steps => Set<StepRow>();
    public DbSet<ExternalImageRow> ExternalImages => Set<ExternalImageRow>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<CategoryRow>().HasKey(x => x.Id);
        model.Entity<CategoryRow>().HasIndex(x => x.NameKey).IsUnique();
        model.Entity<ChainRow>().HasKey(x => x.Id);
        model.Entity<ChainRow>().HasIndex(x => x.NameKey).IsUnique();
        model.Entity<ChainCategoryRow>().HasKey(x => new { x.ChainId, x.CategoryId });
        model.Entity<ChainCategoryRow>().HasOne(x => x.Chain).WithMany(x => x.Categories).HasForeignKey(x => x.ChainId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<ChainCategoryRow>().HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<StepRow>().HasKey(x => x.Id);
        model.Entity<StepRow>().HasOne(x => x.Chain).WithMany(x => x.Steps).HasForeignKey(x => x.ChainId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<StepRow>().HasIndex(x => new { x.ChainId, x.Position });
        model.Entity<ExternalImageRow>().HasKey(x => x.SourceUrl);
        model.Entity<ExternalImageRow>().HasIndex(x => x.LocalUrl).IsUnique();
    }
}

public sealed class CategoryRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string NameKey { get; set; } = "";
}

public sealed class ChainRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string NameKey { get; set; } = "";
    public List<ChainCategoryRow> Categories { get; set; } = [];
    public List<StepRow> Steps { get; set; } = [];
}

public sealed class ChainCategoryRow
{
    public Guid ChainId { get; set; }
    public ChainRow Chain { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public CategoryRow Category { get; set; } = null!;
}

public sealed class StepRow
{
    public Guid Id { get; set; }
    public Guid ChainId { get; set; }
    public ChainRow Chain { get; set; } = null!;
    public int Position { get; set; }
    public string? Title { get; set; }
    public string Description { get; set; } = "";
}

public sealed class ExternalImageRow
{
    public string SourceUrl { get; set; } = "";
    public string LocalUrl { get; set; } = "";
}

public sealed class WorkflowStore(WorkflowDbContext db) : IWorkflowStore
{
    public async Task<List<Category>> Categories(string? name, CancellationToken ct)
    {
        var query = db.Categories.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(name)) query = query.Where(x => x.NameKey.Contains(System.Text.RegularExpressions.Regex.Replace(name.Trim(), @"\s+", " ").ToUpperInvariant()));
        var rows = await query.OrderBy(x => x.NameKey).ToListAsync(ct);
        return rows.Select(x => new Category(x.Id, x.Name)).ToList();
    }

    public async Task<Category?> Category(Guid id, CancellationToken ct)
    {
        var row = await db.Categories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return row is null ? null : new Category(row.Id, row.Name);
    }

    public Task<bool> CategoryNameExists(string key, Guid? exceptId, CancellationToken ct) =>
        db.Categories.AnyAsync(x => x.NameKey == key && (!exceptId.HasValue || x.Id != exceptId.Value), ct);

    public Task<bool> CategoryIsUsed(Guid id, CancellationToken ct) =>
        db.ChainCategories.AnyAsync(x => x.CategoryId == id, ct);

    public async Task SaveCategory(Category category, CancellationToken ct)
    {
        var row = await db.Categories.FirstOrDefaultAsync(x => x.Id == category.Id, ct);
        if (row is null)
        {
            row = new CategoryRow { Id = category.Id };
            db.Categories.Add(row);
        }
        row.Name = category.Name;
        row.NameKey = NameRules.Key(category.Name);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteCategory(Guid id, CancellationToken ct)
    {
        var row = await db.Categories.FirstAsync(x => x.Id == id, ct);
        db.Categories.Remove(row);
        await db.SaveChangesAsync(ct);
    }

    private static ActionChain Map(ChainRow row) => new(row.Id, row.Name,
        row.Categories.Select(x => x.CategoryId), row.Steps.OrderBy(x => x.Position).Select(x => new ActionStep(x.Id, x.Title, x.Description)));

    private IQueryable<ChainRow> FullChains() => db.Chains.Include(x => x.Categories).Include(x => x.Steps).AsSplitQuery();

    public async Task<List<ActionChain>> Chains(string? name, Guid? categoryId, CancellationToken ct)
    {
        var query = FullChains().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(name)) query = query.Where(x => x.NameKey.Contains(System.Text.RegularExpressions.Regex.Replace(name.Trim(), @"\s+", " ").ToUpperInvariant()));
        if (categoryId.HasValue) query = query.Where(x => x.Categories.Any(c => c.CategoryId == categoryId.Value));
        return (await query.OrderBy(x => x.NameKey).ToListAsync(ct)).Select(Map).ToList();
    }

    public async Task<ActionChain?> Chain(Guid id, CancellationToken ct)
    {
        var row = await FullChains().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return row is null ? null : Map(row);
    }

    public Task<bool> ChainNameExists(string key, Guid? exceptId, CancellationToken ct) =>
        db.Chains.AnyAsync(x => x.NameKey == key && (!exceptId.HasValue || x.Id != exceptId.Value), ct);

    public async Task SaveChain(ActionChain chain, CancellationToken ct)
    {
        var row = await FullChains().FirstOrDefaultAsync(x => x.Id == chain.Id, ct);
        if (row is null)
        {
            row = new ChainRow { Id = chain.Id };
            db.Chains.Add(row);
        }
        row.Name = chain.Name;
        row.NameKey = NameRules.Key(chain.Name);

        foreach (var link in row.Categories.Where(x => !chain.CategoryIds.Contains(x.CategoryId)).ToList()) db.ChainCategories.Remove(link);
        foreach (var id in chain.CategoryIds.Where(id => row.Categories.All(x => x.CategoryId != id)))
            row.Categories.Add(new ChainCategoryRow { ChainId = chain.Id, CategoryId = id });

        foreach (var step in row.Steps.Where(x => chain.Steps.All(s => s.Id != x.Id)).ToList()) db.Steps.Remove(step);
        for (var i = 0; i < chain.Steps.Count; i++)
        {
            var item = chain.Steps[i];
            var stored = row.Steps.FirstOrDefault(x => x.Id == item.Id);
            if (stored is null)
            {
                stored = new StepRow { Id = item.Id, ChainId = chain.Id };
                db.Steps.Add(stored);
            }
            stored.Title = item.Title;
            stored.Description = item.Description;
            stored.Position = i + 1;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteChain(Guid id, CancellationToken ct)
    {
        var row = await db.Chains.FirstAsync(x => x.Id == id, ct);
        db.Chains.Remove(row);
        await db.SaveChangesAsync(ct);
    }

    public Task<bool> ImageIsReferenced(string url, CancellationToken ct) =>
        db.Steps.AnyAsync(x => x.Description.Contains(url), ct);

    public Task<string?> CachedImage(string sourceUrl, CancellationToken ct) =>
        db.ExternalImages.AsNoTracking().Where(x => x.SourceUrl == sourceUrl).Select(x => x.LocalUrl).FirstOrDefaultAsync(ct);

    public async Task RememberImage(string sourceUrl, string localUrl, CancellationToken ct)
    {
        db.ExternalImages.Add(new ExternalImageRow { SourceUrl = sourceUrl, LocalUrl = localUrl });
        await db.SaveChangesAsync(ct);
    }

    public async Task ForgetImage(string localUrl, CancellationToken ct)
    {
        var row = await db.ExternalImages.FirstOrDefaultAsync(x => x.LocalUrl == localUrl, ct);
        if (row is null) return;
        db.ExternalImages.Remove(row);
        await db.SaveChangesAsync(ct);
    }
}
