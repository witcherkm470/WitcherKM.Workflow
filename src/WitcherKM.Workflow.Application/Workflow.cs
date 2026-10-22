using MediatR;
using WitcherKM.Workflow.Domain;

namespace WitcherKM.Workflow.Application;

public sealed class WorkflowException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public interface IWorkflowStore
{
    Task<List<Category>> Categories(string? name, CancellationToken ct);
    Task<Category?> Category(Guid id, CancellationToken ct);
    Task<bool> CategoryNameExists(string key, Guid? exceptId, CancellationToken ct);
    Task<bool> CategoryIsUsed(Guid id, CancellationToken ct);
    Task SaveCategory(Category category, CancellationToken ct);
    Task DeleteCategory(Guid id, CancellationToken ct);
    Task<List<ActionChain>> Chains(string? name, Guid? categoryId, CancellationToken ct);
    Task<ActionChain?> Chain(Guid id, CancellationToken ct);
    Task<bool> ChainNameExists(string key, Guid? exceptId, CancellationToken ct);
    Task SaveChain(ActionChain chain, CancellationToken ct);
    Task DeleteChain(Guid id, CancellationToken ct);
    Task<bool> ImageIsReferenced(string url, CancellationToken ct);
    Task<string?> CachedImage(string sourceUrl, CancellationToken ct);
    Task RememberImage(string sourceUrl, string localUrl, CancellationToken ct);
    Task ForgetImage(string localUrl, CancellationToken ct);
}

public sealed record LocalizedImages(string Markdown, List<string> Warnings);

public interface IImageStorage
{
    Task<string> Save(Stream content, string contentType, CancellationToken ct);
    Task<LocalizedImages> Localize(string markdown, CancellationToken ct);
    Task DeleteUnreferenced(string oldMarkdown, CancellationToken ct);
    (string Path, string ContentType)? Get(string fileName);
}

public sealed record CategoryView(Guid Id, string Name);
public sealed record ChainListView(Guid Id, string Name, List<CategoryView> Categories, int StepCount);
public sealed record StepView(Guid Id, int Number, string? Title, string DisplayTitle, string Description, List<string> ImageWarnings);
public sealed record ChainView(Guid Id, string Name, List<CategoryView> Categories, List<StepView> Steps);

public sealed record ListCategories(string? Name) : IRequest<List<CategoryView>>;
public sealed record GetCategory(Guid Id) : IRequest<CategoryView>;
public sealed record CreateCategory(string? Name) : IRequest<CategoryView>;
public sealed record UpdateCategory(Guid Id, string? Name) : IRequest<CategoryView>;
public sealed record RemoveCategory(Guid Id) : IRequest<bool>;

public sealed record ListChains(string? Name, Guid? CategoryId) : IRequest<List<ChainListView>>;
public sealed record GetChain(Guid Id) : IRequest<ChainView>;
public sealed record CreateChain(string? Name, List<Guid>? CategoryIds) : IRequest<ChainView>;
public sealed record UpdateChain(Guid Id, string? Name, List<Guid>? CategoryIds) : IRequest<ChainView>;
public sealed record RemoveChain(Guid Id) : IRequest<bool>;

public sealed record GetStep(Guid ChainId, Guid StepId) : IRequest<StepView>;
public sealed record CreateStep(Guid ChainId, string? Title, string? Description) : IRequest<StepView>;
public sealed record UpdateStep(Guid ChainId, Guid StepId, string? Title, string? Description) : IRequest<StepView>;
public sealed record RemoveStep(Guid ChainId, Guid StepId) : IRequest<bool>;
public sealed record MoveStep(Guid ChainId, Guid StepId, int Position) : IRequest<StepView>;

public sealed class WorkflowHandlers(IWorkflowStore store, IImageStorage images) :
    IRequestHandler<ListCategories, List<CategoryView>>,
    IRequestHandler<GetCategory, CategoryView>,
    IRequestHandler<CreateCategory, CategoryView>,
    IRequestHandler<UpdateCategory, CategoryView>,
    IRequestHandler<RemoveCategory, bool>,
    IRequestHandler<ListChains, List<ChainListView>>,
    IRequestHandler<GetChain, ChainView>,
    IRequestHandler<CreateChain, ChainView>,
    IRequestHandler<UpdateChain, ChainView>,
    IRequestHandler<RemoveChain, bool>,
    IRequestHandler<GetStep, StepView>,
    IRequestHandler<CreateStep, StepView>,
    IRequestHandler<UpdateStep, StepView>,
    IRequestHandler<RemoveStep, bool>,
    IRequestHandler<MoveStep, StepView>
{
    private static WorkflowException Missing(string item) => new(404, $"{item} не найдена.");
    private static CategoryView View(Category x) => new(x.Id, x.Name);
    private static StepView View(ActionStep x, int number, List<string>? warnings = null) =>
        new(x.Id, number, x.Title, x.Title ?? $"Шаг {number}", x.Description, warnings ?? []);
    private static List<StepView> Steps(ActionChain x) => x.Steps.Select((s, i) => View(s, i + 1)).ToList();

    private async Task<List<CategoryView>> ChainCategories(ActionChain chain, CancellationToken ct)
    {
        var categories = await store.Categories(null, ct);
        return categories.Where(c => chain.CategoryIds.Contains(c.Id)).Select(View).OrderBy(c => c.Name).ToList();
    }

    private async Task<ChainView> View(ActionChain chain, CancellationToken ct) =>
        new(chain.Id, chain.Name, await ChainCategories(chain, ct), Steps(chain));

    private static string ValidName(string? name)
    {
        try { return NameRules.Normalize(name); }
        catch (DomainRuleException e) { throw new WorkflowException(400, e.Message); }
    }

    private async Task CheckCategories(List<Guid>? ids, CancellationToken ct)
    {
        if (ids is null || ids.Count == 0) throw new WorkflowException(400, "Выберите хотя бы одну категорию.");
        foreach (var id in ids.Distinct())
            if (await store.Category(id, ct) is null) throw new WorkflowException(400, "Выбрана несуществующая категория.");
    }

    private async Task<ActionChain> RequiredChain(Guid id, CancellationToken ct) =>
        await store.Chain(id, ct) ?? throw Missing("Цепочка");

    public async Task<List<CategoryView>> Handle(ListCategories q, CancellationToken ct) =>
        (await store.Categories(q.Name, ct)).Select(View).ToList();

    public async Task<CategoryView> Handle(GetCategory q, CancellationToken ct) =>
        View(await store.Category(q.Id, ct) ?? throw Missing("Категория"));

    public async Task<CategoryView> Handle(CreateCategory q, CancellationToken ct)
    {
        var name = ValidName(q.Name);
        if (await store.CategoryNameExists(NameRules.Key(name), null, ct)) throw new WorkflowException(409, "Категория с таким именем уже существует.");
        var category = new Category(Guid.NewGuid(), name);
        await store.SaveCategory(category, ct);
        return View(category);
    }

    public async Task<CategoryView> Handle(UpdateCategory q, CancellationToken ct)
    {
        var category = await store.Category(q.Id, ct) ?? throw Missing("Категория");
        var name = ValidName(q.Name);
        if (await store.CategoryNameExists(NameRules.Key(name), q.Id, ct)) throw new WorkflowException(409, "Категория с таким именем уже существует.");
        category.Rename(name);
        await store.SaveCategory(category, ct);
        return View(category);
    }

    public async Task<bool> Handle(RemoveCategory q, CancellationToken ct)
    {
        if (await store.Category(q.Id, ct) is null) throw Missing("Категория");
        if (await store.CategoryIsUsed(q.Id, ct)) throw new WorkflowException(409, "Категория связана с цепочками. Сначала отвяжите её.");
        await store.DeleteCategory(q.Id, ct);
        return true;
    }

    public async Task<List<ChainListView>> Handle(ListChains q, CancellationToken ct)
    {
        var chains = await store.Chains(q.Name, q.CategoryId, ct);
        var categories = await store.Categories(null, ct);
        return chains.Select(c => new ChainListView(c.Id, c.Name,
            categories.Where(x => c.CategoryIds.Contains(x.Id)).Select(View).ToList(), c.Steps.Count)).ToList();
    }

    public async Task<ChainView> Handle(GetChain q, CancellationToken ct) => await View(await RequiredChain(q.Id, ct), ct);

    public async Task<ChainView> Handle(CreateChain q, CancellationToken ct)
    {
        var name = ValidName(q.Name);
        await CheckCategories(q.CategoryIds, ct);
        if (await store.ChainNameExists(NameRules.Key(name), null, ct)) throw new WorkflowException(409, "Цепочка с таким названием уже существует.");
        var chain = new ActionChain(Guid.NewGuid(), name, q.CategoryIds!);
        await store.SaveChain(chain, ct);
        return await View(chain, ct);
    }

    public async Task<ChainView> Handle(UpdateChain q, CancellationToken ct)
    {
        var chain = await RequiredChain(q.Id, ct);
        var name = ValidName(q.Name);
        await CheckCategories(q.CategoryIds, ct);
        if (await store.ChainNameExists(NameRules.Key(name), q.Id, ct)) throw new WorkflowException(409, "Цепочка с таким названием уже существует.");
        chain.Rename(name);
        chain.SetCategories(q.CategoryIds!);
        await store.SaveChain(chain, ct);
        return await View(chain, ct);
    }

    public async Task<bool> Handle(RemoveChain q, CancellationToken ct)
    {
        var chain = await RequiredChain(q.Id, ct);
        await store.DeleteChain(q.Id, ct);
        foreach (var step in chain.Steps) await images.DeleteUnreferenced(step.Description, ct);
        return true;
    }

    public async Task<StepView> Handle(GetStep q, CancellationToken ct)
    {
        var chain = await RequiredChain(q.ChainId, ct);
        var index = chain.Steps.FindIndex(x => x.Id == q.StepId);
        if (index < 0) throw Missing("Шаг");
        return View(chain.Steps[index], index + 1);
    }

    public async Task<StepView> Handle(CreateStep q, CancellationToken ct)
    {
        var chain = await RequiredChain(q.ChainId, ct);
        var localized = await images.Localize(q.Description ?? "", ct);
        var step = chain.AddStep(q.Title, localized.Markdown);
        try { await store.SaveChain(chain, ct); }
        catch
        {
            await images.DeleteUnreferenced(localized.Markdown, CancellationToken.None);
            throw;
        }
        return View(step, chain.Steps.Count, localized.Warnings);
    }

    public async Task<StepView> Handle(UpdateStep q, CancellationToken ct)
    {
        var chain = await RequiredChain(q.ChainId, ct);
        var index = chain.Steps.FindIndex(x => x.Id == q.StepId);
        if (index < 0) throw Missing("Шаг");
        var step = chain.Steps[index];
        var old = step.Description;
        var localized = await images.Localize(q.Description ?? "", ct);
        step.Update(q.Title, localized.Markdown);
        try { await store.SaveChain(chain, ct); }
        catch
        {
            await images.DeleteUnreferenced(localized.Markdown, CancellationToken.None);
            throw;
        }
        await images.DeleteUnreferenced(old, ct);
        return View(step, index + 1, localized.Warnings);
    }

    public async Task<bool> Handle(RemoveStep q, CancellationToken ct)
    {
        var chain = await RequiredChain(q.ChainId, ct);
        var step = chain.Steps.FirstOrDefault(x => x.Id == q.StepId) ?? throw Missing("Шаг");
        chain.RemoveStep(q.StepId);
        await store.SaveChain(chain, ct);
        await images.DeleteUnreferenced(step.Description, ct);
        return true;
    }

    public async Task<StepView> Handle(MoveStep q, CancellationToken ct)
    {
        var chain = await RequiredChain(q.ChainId, ct);
        try { chain.MoveStep(q.StepId, q.Position); }
        catch (KeyNotFoundException) { throw Missing("Шаг"); }
        catch (DomainRuleException e) { throw new WorkflowException(400, e.Message); }
        await store.SaveChain(chain, ct);
        return View(chain.Steps[q.Position - 1], q.Position);
    }
}
