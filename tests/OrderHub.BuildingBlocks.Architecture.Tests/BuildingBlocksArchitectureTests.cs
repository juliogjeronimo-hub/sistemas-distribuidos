using System.Reflection;
using NetArchTest.Rules;
using OrderHub.BuildingBlocks.Contracts;
using OrderHub.BuildingBlocks.Messaging;
using Xunit;

namespace OrderHub.BuildingBlocks.Architecture.Tests;

/// <summary>
/// Validates that the shared BuildingBlocks never depend on a service bounded context.
/// Rules source: .kiro/steering/structure.md (no ProjectReference between services;
/// BuildingBlocks hold only event contracts and technical utilities).
/// </summary>
public class BuildingBlocksArchitectureTests
{
    private static readonly Assembly ContractsAssembly = typeof(EventEnvelope).Assembly;

    private static readonly Assembly MessagingAssembly = typeof(MessagingAssemblyMarker).Assembly;

    private static readonly string[] ServiceNamespaces =
    [
        "OrderHub.Catalog",
        "OrderHub.Inventory",
        "OrderHub.Orders",
        "OrderHub.Payments",
        "OrderHub.Notifications",
        "OrderHub.Gateway",
    ];

    [Fact]
    public void Contracts_ShouldNot_DependOnAnyService()
    {
        var result = Types.InAssembly(ContractsAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ServiceNamespaces)
            .GetResult();

        Assert.True(result.IsSuccessful,
            "BuildingBlocks.Contracts must not reference any service. " +
            "Violating types: " + string.Join(", ", result.FailingTypes ?? []));
    }

    [Fact]
    public void Messaging_ShouldNot_DependOnAnyService()
    {
        var result = Types.InAssembly(MessagingAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ServiceNamespaces)
            .GetResult();

        Assert.True(result.IsSuccessful,
            "BuildingBlocks.Messaging must not reference any service. " +
            "Violating types: " + string.Join(", ", result.FailingTypes ?? []));
    }

    [Fact]
    public void BuildingBlocks_ShouldNot_ReferenceServiceAssemblies()
    {
        foreach (var (name, assembly) in new[]
        {
            (nameof(ContractsAssembly), ContractsAssembly),
            (nameof(MessagingAssembly), MessagingAssembly),
        })
        {
            var foreignReferences = assembly
                .GetReferencedAssemblies()
                .Select(a => a.Name ?? string.Empty)
                .Where(n => ServiceNamespaces.Any(s => n.StartsWith(s, StringComparison.Ordinal)))
                .ToList();

            Assert.True(foreignReferences.Count == 0,
                $"{name} references foreign services: {string.Join(", ", foreignReferences)}");
        }
    }
}
