using System.Reflection;
using AspNetVue.Server;

namespace AspNetVue.Server.ArchitectureTests;

public abstract class BaseTest
{
    protected static readonly Assembly WebApiAssembly = typeof(DependencyInjection).Assembly;
}
