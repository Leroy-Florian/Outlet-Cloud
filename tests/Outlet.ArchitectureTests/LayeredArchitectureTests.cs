using System.Reflection;
using NetArchTest.Rules;

namespace Outlet.ArchitectureTests;

public sealed class LayeredArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void Domain_ShouldNot_DependOn_Application()
    {
        var result = Types.InAssemblies(AllDomainAssemblies)
            .ShouldNot()
            .HaveDependencyOnAny([.. AllApplicationAssemblies.Select(a => a.GetName().Name!)])
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Domain layer depends on Application layer:{Environment.NewLine}" +
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Domain_ShouldNot_DependOn_Infrastructure()
    {
        var result = Types.InAssemblies(AllDomainAssemblies)
            .ShouldNot()
            .HaveDependencyOnAny([.. AllInfrastructureAssemblies.Select(a => a.GetName().Name!)])
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Domain layer depends on Infrastructure layer:{Environment.NewLine}" +
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_ShouldNot_DependOn_Infrastructure()
    {
        var result = Types.InAssemblies(AllApplicationAssemblies)
            .ShouldNot()
            .HaveDependencyOnAny([.. AllInfrastructureAssemblies.Select(a => a.GetName().Name!)])
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Application layer depends on Infrastructure layer:{Environment.NewLine}" +
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Kernel_ShouldNot_DependOn_AnyLayer()
    {
        string[] forbidden =
        [
            .. AllDomainAssemblies.Select(a => a.GetName().Name!),
            .. AllApplicationAssemblies.Select(a => a.GetName().Name!),
            .. AllInfrastructureAssemblies.Select(a => a.GetName().Name!),
        ];

        var result = Types.InAssembly(KernelAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbidden)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"SharedKernel must stay independent of every bounded-context layer:{Environment.NewLine}" +
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Theory]
    [MemberData(nameof(InfrastructureAssemblies))]
    public void Infrastructure_Should_DependOn_ApplicationOrDomain(Assembly infrastructureAssembly)
    {
        var hasRealTypes = Types.InAssembly(infrastructureAssembly)
            .That()
            .DoNotHaveName("AssemblyReference")
            .GetTypes()
            .Any();

        if (!hasRealTypes)
            return;

        string[] inwardLayers =
        [
            .. AllApplicationAssemblies.Select(a => a.GetName().Name!),
            .. AllDomainAssemblies.Select(a => a.GetName().Name!),
        ];

        var hasInwardDependency = Types.InAssembly(infrastructureAssembly)
            .That()
            .HaveDependencyOnAny(inwardLayers)
            .GetTypes()
            .Any();

        Assert.True(hasInwardDependency,
            $"{infrastructureAssembly.GetName().Name} should implement Application ports / use Domain types — " +
            "an Infrastructure layer with no inward dependency is dead weight.");
    }

    public static TheoryData<Assembly> InfrastructureAssemblies()
    {
        var data = new TheoryData<Assembly>();
        foreach (var assembly in AllInfrastructureAssemblies)
            data.Add(assembly);
        return data;
    }
}
