namespace MudExtensions.UnitTests.TestComponents;

public sealed class MutableSelectItem
{
    public MutableSelectItem(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public int Id { get; }

    public string Name { get; set; }
}

public sealed class MutableSelectItemIdComparer : IEqualityComparer<MutableSelectItem?>
{
    public bool Equals(MutableSelectItem? x, MutableSelectItem? y) => x?.Id == y?.Id;

    public int GetHashCode(MutableSelectItem? obj) => obj?.Id.GetHashCode() ?? 0;
}
