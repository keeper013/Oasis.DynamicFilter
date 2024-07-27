namespace LinqToSqlTest;

using Microsoft.EntityFrameworkCore;
using Oasis.DynamicFilter;
using System.Linq;

public class UnitTest1 : TestBase
{
    [Theory]
    [InlineData("Book Test 1", null, 1)]
    [InlineData("Book 3", null, 2)]
    [InlineData(null, 2012, 1)]
    [InlineData(null, 2008, 2)]
    [InlineData("Book 3", 2008, 1)]
    [InlineData("Test Book 5", 2017, 0)]
    public async Task FilterBook(string? name, int? year, int number)
    {
        var expressionMaker = new FilterBuilder().Register<Book, BookFilter>().Build();

        await InitializeData();

        var filter = new BookFilter { Name = name, PublishedYear = year };
        var exp = expressionMaker.GetExpression<Book, BookFilter>(filter);

        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var query = databaseContext.Set<Book>().Where(exp);
            //var str = query.ToQueryString();
            var result = await query.ToListAsync();
            Assert.Equal(number, result.Count);
        });
    }

    [Fact]
    public async Task FilterBookWithAuthorAge()
    {
        var expressionMaker = new FilterBuilder()
            .Configure<Book, AuthorFilter>()
                .Filter(filter => book => book.Author.Name.Contains(filter.AuthorName!), filter => !string.IsNullOrEmpty(filter.AuthorName))
                .Filter(filter => book => !filter.Age.HasValue || book.PublishedYear - book.Author.BirthYear < filter.Age)
                .Finish()
        .Build();

        await InitializeData();

        var filter = new AuthorFilter { Age = 40 };
        var exp = expressionMaker.GetExpression<Book, AuthorFilter>(filter);
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var query = databaseContext.Set<Book>().Include(b => b.Author).Where(exp);
            //var str = query.ToQueryString();
            var result = await query.ToListAsync();
            Assert.Equal(2, result.Count);
            Assert.Single(result.Where(r => r.Name == "Book Test 1"));
            Assert.Single(result.Where(r => r.Name == "Book 2"));
        });
    }

    [Fact]
    public async Task FilterBookWithAuthorName()
    {
        var expressionMaker = new FilterBuilder()
            .Configure<Book, AuthorFilter>()
                .Filter(filter => book => book.Author.Name.Contains(filter.AuthorName!), filter => !string.IsNullOrEmpty(filter.AuthorName))
                .Filter(filter => book => book.PublishedYear - book.Author.BirthYear < filter.Age, filter => filter.Age.HasValue)
                .Finish()
        .Build();

        await InitializeData();

        var filter = new AuthorFilter { AuthorName = "John" };
        var exp = expressionMaker.GetExpression<Book, AuthorFilter>(filter);
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var query = databaseContext.Set<Book>().Include(b => b.Author).Where(exp);
            //var str = query.ToQueryString();
            var result = await query.ToListAsync();
            Assert.Equal(2, result.Count);
            Assert.Single(result.Where(r => r.Name == "Book Test 1"));
            Assert.Single(result.Where(r => r.Name == "Book 3" && r.PublishedYear == 2012));
        });
    }

    [Fact]
    public async Task FilterBookWithAuthorNameAndAge()
    {
        var expressionMaker = new FilterBuilder()
            .Configure<Book, AuthorFilter>()
                .Filter(filter => book => book.Author.Name.Contains(filter.AuthorName!), filter => !string.IsNullOrEmpty(filter.AuthorName))
                .Filter(filter => book => book.PublishedYear - book.Author.BirthYear < filter.Age, filter => filter.Age.HasValue)
                .Finish()
        .Build();

        await InitializeData();

        var filter = new AuthorFilter { Age = 40, AuthorName = "Jane" };
        var exp = expressionMaker.GetExpression<Book, AuthorFilter>(filter);
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var query = databaseContext.Set<Book>().Include(b => b.Author).Where(exp);
            //var str = query.ToQueryString();
            var result = await query.ToListAsync();
            Assert.Single(result);
            Assert.Single(result.Where(r => r.Name == "Book 2" && r.PublishedYear == 2008));
        });
    }

    [Theory]
    [InlineData(2001, 2005, 0)]
    [InlineData(2007, 2015, 3)]
    public async Task FilterBookByYearRange(int fromYear, int toYear, int number)
    {
        var expressionMaker = new FilterBuilder()
            .Configure<Book, BookByYearRangeFilter>()
                .Filter(filter => book => filter.FromYear <= book.PublishedYear && book.PublishedYear <= filter.ToYear)
                .Finish()
        .Build();

        await InitializeData();

        var filter = new BookByYearRangeFilter { FromYear = fromYear, ToYear = toYear };
        var exp = expressionMaker.GetExpression<Book, BookByYearRangeFilter>(filter);

        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var query = databaseContext.Set<Book>().Where(exp);
            //var str = query.ToQueryString();
            var result = await query.ToListAsync();
            Assert.Equal(number, result.Count);
        });
    }

    [Fact]
    public async Task FilterBookByPartialNameContains()
    {
        var expressionMaker = new FilterBuilder()
            .Configure<Book, BookByNameFilter>()
                .ExcludeProperties(book => book.Name)
                .Filter(filter => book => book.Name.Contains(filter.Name!), filter => !string.IsNullOrEmpty(filter.Name))
            .Finish()
        .Build();

        await InitializeData();

        var filter = new BookByNameFilter { Name = "5" };
        var exp = expressionMaker.GetExpression<Book, BookByNameFilter>(filter);

        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var query = databaseContext.Set<Book>().Where(exp);
            //var str = query.ToQueryString();
            var result = await query.ToListAsync();
            Assert.Single(result);
            Assert.Equal("Test Book 5", result[0].Name);
        });
    }

    [Fact]
    public async Task FilterBookByPartialNameStartsWith()
    {
        var expressionMaker = new FilterBuilder()
            .Configure<Book, BookByNameFilter>()
                .ExcludeProperties(book => book.Name)
                .Filter(filter => book => book.Name.StartsWith(filter.Name!), filter => !string.IsNullOrEmpty(filter.Name))
            .Finish()
        .Build();

        await InitializeData();

        var filter = new BookByNameFilter { Name = "Book T" };
        var exp = expressionMaker.GetExpression<Book, BookByNameFilter>(filter);

        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var query = databaseContext.Set<Book>().Where(exp);
            //var str = query.ToQueryString();
            var result = await query.ToListAsync();
            Assert.Single(result);
            Assert.Equal("Book Test 1", result[0].Name);
        });
    }

    [Fact]
    public async Task FilterBookByPartialNameEndsWith()
    {
        var expressionMaker = new FilterBuilder()
            .Configure<Book, BookByNameFilter>()
                .ExcludeProperties(book => book.Name)
                .Filter(filter => book => book.Name.EndsWith(filter.Name!), filter => !string.IsNullOrEmpty(filter.Name))
            .Finish()
        .Build();

        await InitializeData();

        var filter = new BookByNameFilter { Name = "2" };
        var exp = expressionMaker.GetExpression<Book, BookByNameFilter>(filter);

        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var query = databaseContext.Set<Book>().Where(exp);
            //var str = query.ToQueryString();
            var result = await query.ToListAsync();
            Assert.Single(result);
            Assert.Equal("Book 2", result[0].Name);
        });
    }

    [Fact]
    public async Task FilterBookByPartialNameIn()
    {
        var expressionMaker = new FilterBuilder()
            .Configure<Book, BookByNameFilter>()
                .ExcludeProperties(book => book.Name)
                .Filter(filter => book => filter.Name!.Contains(book.Name), filter => !string.IsNullOrEmpty(filter.Name))
            .Finish()
        .Build();

        await InitializeData();

        var filter = new BookByNameFilter { Name = "Book 1 or Book 2" };
        var exp = expressionMaker.GetExpression<Book, BookByNameFilter>(filter);

        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var query = databaseContext.Set<Book>().Where(exp);
            //var str = query.ToQueryString();
            var result = await query.ToListAsync();
            Assert.Single(result);
            Assert.Equal("Book 2", result[0].Name);
        });
    }

    private async Task InitializeData()
    {
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var author1 = new Author { Id = 1, Name = "John Doe", BirthYear = 1971 };
            var author2 = new Author { Id = 2, Name = "Jane Doe", BirthYear = 1969 };
            var author3 = new Author { Id = 3, Name = "Black Smith", BirthYear = 1950 };
            databaseContext.Set<Book>().AddRange(new Book[]
            {
                new Book { Id = 1, Name = "Book Test 1", PublishedYear = 2000, AuthorId = 1, Author = author1 },
                new Book { Id = 2, Name = "Book 2", PublishedYear = 2008, AuthorId = 2, Author = author2 },
                new Book { Id = 3, Name = "Book 3", PublishedYear = 2008, AuthorId = 3, Author = author3 },
                new Book { Id = 4, Name = "Book 3", PublishedYear = 2012, AuthorId = 1, Author = author1 },
                new Book { Id = 5, Name = "Test Book 5", PublishedYear = 2016, AuthorId = 2, Author = author2 },
            });

            databaseContext.Set<Author>().AddRange(new Author[]
            {
                author1,
                author2,
                author3,
            });

            await databaseContext.SaveChangesAsync();
        });
    }
}