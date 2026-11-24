using System.Reflection;
using Inventory.Domain;
using NetArchTest.Rules;

namespace Inventory.ArchitectureTests;

/// <summary>
/// Clean Architecture dependency rule, enforced by the build: inner layers never reference outer layers
/// or infrastructure frameworks.
/// </summary>
public class LayerDependencyTests
{
    private const string Application = "Inventory.Application";
    private const string Infrastructure = "Inventory.Infrastructure";
    private const string Api = "Inventory.Api";

    private static readonly Assembly DomainAssembly = typeof(Product).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Inventory.Application.Result).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Inventory.Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly ApiAssembly = typeof(Program).Assembly;

    [Fact]
    public void Domain_depends_on_nothing_else() =>
        AssertNoDependency(DomainAssembly, Application, Infrastructure, Api, "Microsoft.EntityFrameworkCore", "FluentValidation", "Microsoft.Extensions");

    [Fact]
    public void Application_does_not_depend_on_infrastructure_or_web_frameworks() =>
        AssertNoDependency(ApplicationAssembly, Infrastructure, Api, "Microsoft.EntityFrameworkCore", "Microsoft.Data.Sqlite", "Microsoft.AspNetCore");

    [Fact]
    public void Infrastructure_does_not_depend_on_the_api() =>
        AssertNoDependency(InfrastructureAssembly, Api, "Microsoft.AspNetCore");

    [Fact]
    public void Endpoints_talk_to_use_cases_and_queries_never_to_the_database()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That().ResideInNamespace("Inventory.Api.Endpoints")
            .ShouldNot().HaveDependencyOnAny(Infrastructure, "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Domain_classes_are_sealed()
    {
        var result = Types.InAssembly(DomainAssembly).That().AreClasses().And().AreNotAbstract().Should().BeSealed().GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    private static void AssertNoDependency(Assembly assembly, params string[] forbidden)
    {
        var result = Types.InAssembly(assembly).ShouldNot().HaveDependencyOnAny(forbidden).GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    private static string Describe(NetArchTest.Rules.TestResult result) =>
        "Violations: " + string.Join(", ", result.FailingTypeNames ?? []);
}
