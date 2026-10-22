namespace WitcherKM.Workflow.Api.Contracts;

public sealed record NameInput(string? Name);
public sealed record ChainInput(string? Name, List<Guid>? CategoryIds);
public sealed record StepInput(string? Title, string? Description);
public sealed record PositionInput(int Position);
public sealed record ImageUploadResponse(string Url);
