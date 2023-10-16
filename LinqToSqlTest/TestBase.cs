﻿namespace LinqToSqlTest;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;

public abstract class TestBase : IDisposable
{
    protected readonly DbContextOptions _options;
    protected readonly SqliteConnection _connection;

    protected TestBase()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite(_connection)
            .Options;
    }

    public void Dispose() => _connection.Close();

    protected async Task ExecuteWithNewDatabaseContext(Func<DbContext, Task> action)
    {
        using var databaseContext = new DatabaseContext(_options);
        databaseContext.Database.EnsureCreated();
        await action(databaseContext);
    }
}
