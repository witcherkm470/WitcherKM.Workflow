using System.Text.RegularExpressions;

namespace WitcherKM.Workflow.Domain;

public sealed class DomainRuleException(string message) : Exception(message);

public static class NameRules
{
    public static string Normalize(string? value)
    {
        var name = Regex.Replace((value ?? "").Trim(), @"\s+", " ");
        if (name.Length == 0) throw new DomainRuleException("Название не может быть пустым.");
        for (var i = 0; i < name.Length; i++)
        {
            if (!char.IsLetter(name[i])) continue;
            return name[..i] + char.ToUpperInvariant(name[i]) + name[(i + 1)..];
        }
        return name;
    }

    public static string Key(string name) => Normalize(name).ToUpperInvariant();
}

public sealed class Category(Guid id, string name)
{
    public Guid Id { get; } = id;
    public string Name { get; private set; } = NameRules.Normalize(name);
    public void Rename(string name) => Name = NameRules.Normalize(name);
}

public sealed class ActionStep(Guid id, string? title, string? description)
{
    public Guid Id { get; } = id;
    public string? Title { get; private set; } = CleanTitle(title);
    public string Description { get; private set; } = description ?? "";
    public void Update(string? title, string? description)
    {
        Title = CleanTitle(title);
        Description = description ?? "";
    }
    private static string? CleanTitle(string? title) => string.IsNullOrWhiteSpace(title) ? null : title.Trim();
}

public sealed class ActionChain
{
    public Guid Id { get; }
    public string Name { get; private set; }
    public List<Guid> CategoryIds { get; } = [];
    public List<ActionStep> Steps { get; } = [];

    public ActionChain(Guid id, string name, IEnumerable<Guid> categoryIds, IEnumerable<ActionStep>? steps = null)
    {
        Id = id;
        Name = NameRules.Normalize(name);
        SetCategories(categoryIds);
        if (steps is not null) Steps.AddRange(steps);
    }

    public void Rename(string name) => Name = NameRules.Normalize(name);

    public void SetCategories(IEnumerable<Guid> ids)
    {
        var values = ids.Distinct().ToList();
        if (values.Count == 0) throw new DomainRuleException("Выберите хотя бы одну категорию.");
        CategoryIds.Clear();
        CategoryIds.AddRange(values);
    }

    public ActionStep AddStep(string? title, string? description)
    {
        var step = new ActionStep(Guid.NewGuid(), title, description);
        Steps.Add(step);
        return step;
    }

    public ActionStep GetStep(Guid id) => Steps.FirstOrDefault(x => x.Id == id)
        ?? throw new KeyNotFoundException("Шаг не найден.");

    public void RemoveStep(Guid id)
    {
        if (Steps.RemoveAll(x => x.Id == id) != 1) throw new KeyNotFoundException("Шаг не найден.");
    }

    public void MoveStep(Guid id, int position)
    {
        var index = Steps.FindIndex(x => x.Id == id);
        if (index < 0) throw new KeyNotFoundException("Шаг не найден.");
        if (position < 1 || position > Steps.Count) throw new DomainRuleException("Недопустимая позиция шага.");
        var step = Steps[index];
        Steps.RemoveAt(index);
        Steps.Insert(position - 1, step);
    }
}
