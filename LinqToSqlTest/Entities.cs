namespace LinqToSqlTest;

public sealed class Entity1
{
    public int Id { get; set; }

    public int Number { get; set; }

    public string Name { get; set; } = null!;
}

public sealed class EntityFilter
{
    public int? Number { get; set; }

    public string? Name { get; set; }
}