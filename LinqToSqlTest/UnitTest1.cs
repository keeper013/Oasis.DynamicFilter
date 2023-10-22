namespace LinqToSqlTest;

using Microsoft.EntityFrameworkCore;
using Oasis.DynamicFilter;
using System.Linq;

public class UnitTest1 : TestBase
{
    [Theory]
    [InlineData("Book 1", null, 1)]
    [InlineData("Book 3", null, 2)]
    [InlineData(null, 2012, 1)]
    [InlineData(null, 2008, 2)]
    [InlineData("Book 3", 2008, 1)]
    [InlineData("Book 5", 2017, 0)]
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

    [Theory]
    [InlineData(2001, 2005, 0)]
    [InlineData(2007, 2015, 3)]
    public async Task FilterBookByYearRange(int fromYear, int toYear, int number)
    {
        var expressionMaker = new FilterBuilder()
            .Configure<Book, BookByYearRangeFilter>()
                .FilterByRangedFilter(f => f.FromYear, RangeOperator.LessThanOrEqual, e => e.PublishedYear, RangeOperator.LessThanOrEqual, f => f.ToYear)
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
    public async Task FilterBookByPartialName()
    {
        var expressionMaker = new FilterBuilder()
            .Configure<Book, BookByNameFilter>()
                .FilterByStringProperty(b => b.Name, StringOperator.Contains, f => f.Name)
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
            Assert.Equal("Book 5", result[0].Name);
        });
    }

    private async Task InitializeData()
    {
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            databaseContext.Set<Book>().AddRange(new Book[]
            {
                new Book { Id = 1, Name = "Book 1", PublishedYear = 2000 },
                new Book { Id = 2, Name = "Book 2", PublishedYear = 2008 },
                new Book { Id = 3, Name = "Book 3", PublishedYear = 2008 },
                new Book { Id = 4, Name = "Book 3", PublishedYear = 2012 },
                new Book { Id = 5, Name = "Book 5", PublishedYear = 2016 },
            });

            await databaseContext.SaveChangesAsync();
        });
    }
}