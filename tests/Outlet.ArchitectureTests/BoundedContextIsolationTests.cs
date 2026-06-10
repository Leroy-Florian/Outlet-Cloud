using System.Reflection;
using NetArchTest.Rules;

namespace Outlet.ArchitectureTests;

/// <summary>
/// Each bounded context owns its own model. Contexts communicate by id (and, later,
/// domain events / composition-root orchestration), never by importing another
/// context's types. These tests keep the boundaries honest across BOTH the Domain
/// and Application layers — in particular, issuing a scoped token must not turn into
/// an Identity→Cloud dependency: scopes cross the boundary as plain strings.
/// </summary>
public sealed class BoundedContextIsolationTests : ArchitectureTestBase
{
    private static readonly Assembly[] IdentityContext = [IdentityDomainAssembly, IdentityApplicationAssembly, IdentityInfrastructureAssembly];
    private static readonly Assembly[] CloudContext = [CloudDomainAssembly, CloudApplicationAssembly, CloudInfrastructureAssembly];
    private static readonly Assembly[] CrmContext = [CrmDomainAssembly, CrmApplicationAssembly, CrmInfrastructureAssembly];

    [Fact]
    public void Identity_ShouldNot_DependOn_OtherContexts()
    {
        AssertNoDependency(IdentityContext, [.. CloudContext, .. CrmContext], "Identity", "Cloud/Crm");
    }

    [Fact]
    public void Cloud_ShouldNot_DependOn_OtherContexts()
    {
        AssertNoDependency(CloudContext, [.. IdentityContext, .. CrmContext], "Cloud", "Identity/Crm");
    }

    [Fact]
    public void Crm_ShouldNot_DependOn_OtherContexts()
    {
        AssertNoDependency(CrmContext, [.. IdentityContext, .. CloudContext], "Crm", "Identity/Cloud");
    }

    private static void AssertNoDependency(Assembly[] context, Assembly[] forbidden, string contextName, string forbiddenName)
    {
        string[] forbiddenNames = [.. forbidden.Select(a => a.GetName().Name!)];

        var result = Types.InAssemblies(context)
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenNames)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"The {contextName} context must not depend on {forbiddenName}:{Environment.NewLine}" +
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }
}
