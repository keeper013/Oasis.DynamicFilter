namespace LinqToSqlTest;

public sealed class Book
{
    public int Id { get; set; }

    public int PublishedYear { get; set; }

    public string Name { get; set; } = null!;
}

public sealed class BookFilter
{
    public int? PublishedYear { get; set; } = default!;

    public string? Name { get; set; }
}

public sealed class BookByYearRangeFilter
{
    public int FromYear { get; set; }

    public int ToYear { get; set; }
}

public sealed class BookByNameFilter
{
    public string? Name { get; set; } = null!;
}