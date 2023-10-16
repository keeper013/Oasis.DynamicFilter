namespace LinqToSqlTest;

using Microsoft.EntityFrameworkCore;
using Oasis.DynamicFilter;
using System.Linq;

public class UnitTest1 : TestBase
{
    [Fact]
    public async Task Test1()
    {
        var expressionMaker = new FilterBuilder().Register<Entity1, EntityFilter<double?>>().Build();

        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            databaseContext.Set<Entity1>().Add(new Entity1 { Id = 1, Number = 1, Name = "1" });
            databaseContext.Set<Entity1>().Add(new Entity1 { Id = 2, Number = 2, Name = "2" });
            databaseContext.Set<Entity1>().Add(new Entity1 { Id = 3, Number = 3, Name = "3" });
            databaseContext.Set<Entity1>().Add(new Entity1 { Id = 4, Number = 3, Name = "4" });
            databaseContext.Set<Entity1>().Add(new Entity1 { Id = 5, Number = 4, Name = "5" });
            await databaseContext.SaveChangesAsync();

            var filter = new EntityFilter<double?> { Number = 2, Name = "2" };
            var exp = expressionMaker.GetExpression<Entity1, EntityFilter<double?>>(filter);
            var query = databaseContext.Set<Entity1>().Where(exp);

            // var str = query.ToQueryString();
            var result = await query.ToListAsync();
            Assert.Single(result);
            Assert.Equal("2", result[0].Name);
        });
    }
}