using NetArchTest.Rules;
using Shouldly;
using AspNetVue.Server.Common.Endpoints;
using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.ArchitectureTests.Layers;

public class LayerTests : BaseTest
{
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
            .ResideInNamespace("AspNetVue.Server.Authentication")
            .Or()
            .ResideInNamespace("AspNetVue.Server.Authorization")
            .Or()
            .ResideInNamespace("AspNetVue.Server.Database")
            .Or()
            .ResideInNamespace("AspNetVue.Server.Common")
            .ShouldNot()
            .HaveDependencyOn("AspNetVue.Server.Features")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }
}
