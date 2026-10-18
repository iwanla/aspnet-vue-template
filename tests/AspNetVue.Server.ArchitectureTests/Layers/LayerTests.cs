using NetArchTest.Rules;
using Shouldly;
using AspNetVue.Server.Common.Endpoints;
using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.ArchitectureTests.Layers;

public class LayerTests : BaseTest
{
    [Fact]
    public void CommonTypes_Should_ResideInCapabilityNamespaces()
    {
        string[] rootTypes = WebApiAssembly.GetTypes()
            .Where(type => type.Namespace == "AspNetVue.Server.Common")
            .Select(type => type.FullName!)
            .ToArray();

        rootTypes.ShouldBeEmpty("Common types must belong to a capability namespace.");
    }

    [Fact]
    public void Common_Should_NotDependOn_OtherServerAreas()
    {
        string[] otherNamespaces = WebApiAssembly.GetTypes()
            .Select(type => type.Namespace)
            .OfType<string>()
            .Where(ns => ns.StartsWith("AspNetVue.Server.", StringComparison.Ordinal) &&
                         !ns.StartsWith("AspNetVue.Server.Common.", StringComparison.Ordinal))
            .Distinct()
            .ToArray();

        TestResult result = Types.InAssembly(WebApiAssembly)
            .That()
            .ResideInNamespaceStartingWith("AspNetVue.Server.Common")
            .ShouldNot()
            .HaveDependencyOnAny(otherNamespaces)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            "Common must not depend on other server areas. Offending types: " +
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void CommandAndQueryHandlers_Should_BeSealed()
    {
        TestResult result = Types.InAssembly(WebApiAssembly)
            .That()
            .ImplementInterface(typeof(ICommandHandler<>))
            .Or()
            .ImplementInterface(typeof(ICommandHandler<,>))
            .Or()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Endpoints_Should_BeSealed()
    {
        TestResult result = Types.InAssembly(WebApiAssembly)
            .That()
            .ImplementInterface(typeof(IEndpoint))
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void InfrastructureNamespaces_Should_NotDependOn_Features()
    {
        // Authentication/ + Authorization/ are consumed by features, never the other way around:
        // the dependency points Features -> Authentication/Authorization/Database/Common.
        TestResult result = Types.InAssembly(WebApiAssembly)
            .That()
            .ResideInNamespaceStartingWith("AspNetVue.Server.Authentication")
            .Or()
            .ResideInNamespaceStartingWith("AspNetVue.Server.Authorization")
            .Or()
            .ResideInNamespaceStartingWith("AspNetVue.Server.Database")
            .Or()
            .ResideInNamespaceStartingWith("AspNetVue.Server.Common")
            .ShouldNot()
            .HaveDependencyOn("AspNetVue.Server.Features")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Features_Should_NotDependOn_EachOther()
    {
        // Features are independent vertical slices: anything shared (errors, helpers) goes to
        // Common/, never from one feature to another (see ARCHITECTURE.md: Dependency boundaries).
        string[] features = ["Todos", "Users", "Identity"];

        foreach (string feature in features)
        {
            string[] otherFeatures = [.. features
                .Where(other => other != feature)
                .Select(other => $"AspNetVue.Server.Features.{other}")];

            TestResult result = Types.InAssembly(WebApiAssembly)
                .That()
                .ResideInNamespaceStartingWith($"AspNetVue.Server.Features.{feature}")
                .ShouldNot()
                .HaveDependencyOnAny(otherFeatures)
                .GetResult();

            result.IsSuccessful.ShouldBeTrue(
                $"Features/{feature} must not depend on {string.Join(", ", otherFeatures)}; " +
                $"move the shared type to Common/. Offending types: " +
                string.Join(", ", result.FailingTypeNames ?? []));
        }
    }
}
