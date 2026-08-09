namespace WindowsDebloat.Models;

public sealed class CatalogItem
{
	public required string Id { get; init; }
	public required string Title { get; init; }
	public required string Desc { get; init; }
	public required bool Default { get; init; }
	public required CatalogGroup Group { get; init; }
	public IReadOnlyList<string>? Packages { get; init; }

	public bool IsApp => Packages is not null;
}
