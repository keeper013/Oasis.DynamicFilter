namespace LinqToSqlTest;

using Microsoft.EntityFrameworkCore;
using Oasis.DynamicFilter;
using System.Linq;

public class UnitTest1 : TestBase
{
    [Fact]
    public async Task SingleFieldTest()
    {
        var expressionMaker = new FilterBuilder().Register<Entity1, EntityFilter<double?>>().Build();

        await InitializeData();

        var filter = new EntityFilter<double?> { Number = 2 };
        var exp = expressionMaker.GetExpression<Entity1, EntityFilter<double?>>(filter);

        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var query = databaseContext.Set<Entity1>().Where(exp);
            // var str = query.ToQueryString();
            var result = await query.ToListAsync();
            Assert.Single(result);
            Assert.Equal("2", result[0].Name);
        });
    }

    [Theory]
    [InlineData(2, "2", 1)]
    [InlineData(2, "3", 0)]
    [InlineData(1, "1", 1)]
    public async Task MultipleFieldTest(byte number, string name, int count)
    {
        var expressionMaker = new FilterBuilder().Register<Entity1, EntityFilter<byte>>().Build();

        await InitializeData();

        var filter = new EntityFilter<byte> { Number = number, Name = name };
        var exp = expressionMaker.GetExpression<Entity1, EntityFilter<byte>>(filter);

        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var query = databaseContext.Set<Entity1>().Where(exp);
            // var str = query.ToQueryString();
            var result = await query.ToListAsync();
            Assert.Equal(count, result.Count);
            if (count == 1)
            {
                Assert.Equal(result[0].Number, number);
            }
        });
    }

    private async Task InitializeData()
    {
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            databaseContext.Set<Entity1>().Add(new Entity1 { Id = 1, Number = 1, Name = "1" });
            databaseContext.Set<Entity1>().Add(new Entity1 { Id = 2, Number = 2, Name = "2" });
            databaseContext.Set<Entity1>().Add(new Entity1 { Id = 3, Number = 3, Name = "3" });
            databaseContext.Set<Entity1>().Add(new Entity1 { Id = 4, Number = 3, Name = "4" });
            databaseContext.Set<Entity1>().Add(new Entity1 { Id = 5, Number = 4, Name = "5" });
            await databaseContext.SaveChangesAsync();
        });
    }
}