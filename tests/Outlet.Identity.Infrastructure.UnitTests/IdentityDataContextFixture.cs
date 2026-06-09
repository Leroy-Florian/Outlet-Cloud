using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Outlet.Identity.Infrastructure.Persistence;

namespace Outlet.Identity.Infrastructure.UnitTests;

/// <summary>
/// Hermetic SQLite-in-memory harness for the Identity context. The connection is
/// kept open for the test's lifetime so the in-memory database survives across the
/// several <see cref="IdentityDataContext"/> instances used to force real round-trips.
/// </summary>
public abstract class IdentityDataContextFixture : IDisposable
{
    private readonly SqliteConnection _connection;

    protected IdentityDataContextFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = NewContext();
        context.Database.EnsureCreated();
    }

    protected IdentityDataContext NewContext() =>
        new(new DbContextOptionsBuilder<IdentityDataContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
