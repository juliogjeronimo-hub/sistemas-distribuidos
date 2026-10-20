using NetArchTest.Rules;
using OrderHub.Catalog.Api;
using OrderHub.Catalog.Application;
using OrderHub.Catalog.Domain;
using OrderHub.Catalog.Infrastructure;
using Xunit;

namespace OrderHub.Catalog.Architecture.Tests;

/// <summary>
/// Validates Clean Architecture dependency rules for the Catalog bounded context.
/// Rules source: .kiro/steering/clean-architecture.md
/// </summary>
public class CatalogArchitectureTests
{
    // ─── Assemblies ────────────────────────────────────────────────────────────

    private static readonly System.Reflection.Assembly DomainAssembly
        = typeof(DomainAssemblyMarker).Assembly;

    private static readonly System.Reflection.Assembly ApplicationAssembly
        = typeof(ApplicationAssemblyMarker).Assembly;

    private static readonly System.Reflection.Assembly InfrastructureAssembly
        = typeof(InfrastructureAssemblyMarker).Assembly;

    private static readonly System.Reflection.Assembly ApiAssembly
        = typeof(ApiAssemblyMarker).Assembly;

    // ─── Namespace constants ────────────────────────────────────────────────────

    private const string DomainNs         = "OrderHub.Catalog.Domain";
    private const string ApplicationNs    = "OrderHub.Catalog.Application";
    private const string InfrastructureNs = "OrderHub.Catalog.Infrastructure";
    private const string ApiNs            = "OrderHub.Catalog.Api";

    // Shared technical contracts/utilities — the only permitted cross-project dependency.
    private const string BuildingBlocksNs = "OrderHub.BuildingBlocks";

    // ─── Tests ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Domain is the innermost layer — it must not reference any outer layer.
    /// </summary>
    [Fact]
    public void Domain_ShouldNot_DependOnApplicationInfrastructureOrApi()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNs, InfrastructureNs, ApiNs)
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Domain layer must not reference Application, Infrastructure, or Api. " +
            "Violating types: " + string.Join(", ", result.FailingTypes ?? []));
    }

    /// <summary>
    /// Application orchestrates use-cases and may only reference Domain.
    /// It must not leak into Infrastructure or Api concerns.
    /// </summary>
    [Fact]
    public void Application_ShouldNot_DependOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNs, ApiNs)
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Application layer must not reference Infrastructure or Api. " +
            "Violating types: " + string.Join(", ", result.FailingTypes ?? []));
    }

    /// <summary>
    /// Infrastructure implements ports defined in Application; it must not
    /// reference the Api (composition root) layer.
    /// </summary>
    [Fact]
    public void Infrastructure_ShouldNot_DependOnApi()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNs)
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Infrastructure layer must not reference Api. " +
            "Violating types: " + string.Join(", ", result.FailingTypes ?? []));
    }

    /// <summary>
    /// No Catalog layer may reference another service's namespace.
    /// Catches accidental cross-service coupling (violates the no-ProjectReference-between-services rule).
    /// Shared BuildingBlocks are explicitly allowed — they are the only permitted
    /// cross-project dependency per structure.md.
    /// </summary>
    [Fact]
    public void AllCatalogLayers_ShouldNot_DependOnOtherServices()
    {
        // Any OrderHub namespace that is NOT Catalog and NOT BuildingBlocks is a foreign service.
        const string foreignServicePrefix = "OrderHub.";
        string[] allowedPrefixes =
            [DomainNs, ApplicationNs, InfrastructureNs, ApiNs, BuildingBlocksNs];

        foreach (var (name, assembly) in new[]
        {
            (nameof(DomainAssembly),          DomainAssembly),
            (nameof(ApplicationAssembly),     ApplicationAssembly),
            (nameof(InfrastructureAssembly),  InfrastructureAssembly),
            (nameof(ApiAssembly),             ApiAssembly),
        })
        {
            // Collect all referenced namespaces that start with "OrderHub." but are not Catalog layers.
            var referencedNamespaces = assembly
                .GetReferencedAssemblies()
                .Select(a => a.Name ?? string.Empty)
                .Where(n => n.StartsWith(foreignServicePrefix, StringComparison.Ordinal)
                         && !allowedPrefixes.Any(p => n.StartsWith(p, StringComparison.Ordinal)))
                .ToList();

            Assert.True(referencedNamespaces.Count == 0,
                $"{name} references foreign services: {string.Join(", ", referencedNamespaces)}");
        }
    }
}
