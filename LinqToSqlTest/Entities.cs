namespace LinqToSqlTest;

public sealed class Book
{
    public int Id { get; set; }

    public int PublishedYear { get; set; }

    public string Name { get; set; } = null!;

    public int AuthorId { get; set; }

    public Author Author { get; set; } = null!;
}

public sealed class Author
{
    public int Id { get; set; }

    public int BirthYear { get; set; }

    public string Name { get; set; } = null!;

    public List<Book> Books { get; set; } = null!;
}

public sealed class BookFilter
{
    public int? PublishedYear { get; set; } = default!;

    public string? Name { get; set; }
}

public sealed class AuthorFilter
{
    public string? AuthorName { get; set; }

    public int? Age { get; set; }
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