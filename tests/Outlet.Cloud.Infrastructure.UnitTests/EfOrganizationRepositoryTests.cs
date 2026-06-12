using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Outlet.Cloud.Domain.Organizations;
using Outlet.Cloud.Infrastructure.Persistence;

namespace Outlet.Cloud.Infrastructure.UnitTests;

public sealed class EfOrganizationRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly CloudDbContext _db;

    public EfOrganizationRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _db = NewContext();
        _db.Database.EnsureCreated();
    }

    private CloudDbContext NewContext() =>
        new(new DbContextOptionsBuilder<CloudDbContext>().UseSqlite(_connection).Options);

    private static Organization NewOrg(Guid ownerId) =>
        Organization.Create(
            OrganizationId.From(Guid.NewGuid()),
            OrganizationSlug.From("acme"),
            OrganizationName.From("Acme Corp"),
            MemberUserId.From(ownerId)).Value!;

    [Fact]
    public async Task Should_RoundTrip_Organization_WithOwnerMembership()
    {
        var ownerId = Guid.NewGuid();
        var organization = NewOrg(ownerId);
        await new EfOrganizationRepository(_db).AddAsync(organization);

        await using var readDb = NewContext();
        var loaded = await new EfOrganizationRepository(readDb).GetByIdAsync(organization.Id);

        loaded.Should().NotBeNull();
        loaded!.Slug.Value.Should().Be("acme");
        loaded.Memberships.Should().ContainSingle(m =>
            m.Id == MemberUserId.From(ownerId) && m.Role == OrganizationRole.Owner);
    }

    [Fact]
    public async Task Should_PersistAddedMember_OnUpdate()
    {
        var organization = NewOrg(Guid.NewGuid());
        await new EfOrganizationRepository(_db).AddAsync(organization);
        var newMember = Guid.NewGuid();

        organization.AddMember(MemberUserId.From(newMember), OrganizationRole.Member);
        await new EfOrganizationRepository(_db).UpdateAsync(organization);

        await using var readDb = NewContext();
        var loaded = await new EfOrganizationRepository(readDb).GetByIdAsync(organization.Id);
        loaded!.Memberships.Should().HaveCount(2);
        loaded.Memberships.Should().Contain(m => m.Id == MemberUserId.From(newMember));
    }

    [Fact]
    public async Task Should_PersistRegistryVisibility_OnUpdate()
    {
        var organization = NewOrg(Guid.NewGuid());
        await new EfOrganizationRepository(_db).AddAsync(organization);

        organization.ChangeRegistryVisibility(RegistryVisibility.Public);
        await new EfOrganizationRepository(_db).UpdateAsync(organization);

        await using var readDb = NewContext();
        var loaded = await new EfOrganizationRepository(readDb).GetByIdAsync(organization.Id);
        loaded!.RegistryVisibility.Should().Be(RegistryVisibility.Public);
    }

    [Fact]
    public async Task Should_DefaultRegistryVisibilityToPrivate_When_RoundTripped()
    {
        var organization = NewOrg(Guid.NewGuid());
        await new EfOrganizationRepository(_db).AddAsync(organization);

        await using var readDb = NewContext();
        var loaded = await new EfOrganizationRepository(readDb).GetBySlugAsync(OrganizationSlug.From("acme"));

        loaded!.RegistryVisibility.Should().Be(RegistryVisibility.Private);
    }

    [Fact]
    public async Task Should_ReportSlugExists_When_OrganizationPersisted()
    {
        await new EfOrganizationRepository(_db).AddAsync(NewOrg(Guid.NewGuid()));

        await using var readDb = NewContext();
        var exists = await new EfOrganizationRepository(readDb).ExistsWithSlugAsync(OrganizationSlug.From("acme"));

        exists.Should().BeTrue();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
