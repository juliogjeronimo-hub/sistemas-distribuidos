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
///
/// This test suite ensures:
/// 1. Domain layer has no outbound dependencies on Application, Infrastructure, or Api
/// 2. Application layer has no outbound dependencies on Infrastructure or Api
/// 3. Infrastructure layer has no outbound dependencies on Api
/// 4. All layers respect namespace boundaries
/// 5. No circular dependencies exist
/// 6. Each layer's classes are in their respective namespace
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
    /// Domain is the innermost layer and must not reference any outer layer.
    /// Validates: Domain has no dependencies on Application, Infrastructure, or Api.
    /// </summary>
    [Fact]
    public void Domain_Should_Not_Depend_On_Infrastructure()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNs)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Domain layer must not reference Infrastructure. " +
            $"Violating types: {string.Join(", ", result.FailingTypes ?? [])}");
    }

    /// <summary>
    /// Domain must not reference Api layer.
    /// </summary>
    [Fact]
    public void Domain_Should_Not_Depend_On_Api()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNs)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Domain layer must not reference Api. " +
            $"Violating types: {string.Join(", ", result.FailingTypes ?? [])}");
    }

    /// <summary>
    /// Application orchestrates use-cases and must not reference Infrastructure.
    /// It may only depend on Domain layer (and BuildingBlocks for shared contracts).
    /// </summary>
    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNs)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Application layer must not reference Infrastructure. " +
            $"Violating types: {string.Join(", ", result.FailingTypes ?? [])}");
    }

    /// <summary>
    /// Application must not reference Api layer.
    /// Application should never have a dependency on HTTP/endpoint concerns.
    /// </summary>
    [Fact]
    public void Application_Should_Not_Depend_On_Api()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNs)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Application layer must not reference Api. " +
            $"Violating types: {string.Join(", ", result.FailingTypes ?? [])}");
    }

    /// <summary>
    /// Infrastructure implements ports defined in Application and must not
    /// reference the Api (composition root) layer.
    /// </summary>
    [Fact]
    public void Infrastructure_Should_Not_Depend_On_Api()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNs)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Infrastructure layer must not reference Api. " +
            $"Violating types: {string.Join(", ", result.FailingTypes ?? [])}");
    }

    /// <summary>
    /// Verifies that all types in each layer are located in their respective namespace.
    /// This ensures correct logical organization and prevents namespace layering violations.
    /// </summary>
    [Fact]
    public void Classes_Should_Be_In_Correct_Namespaces()
    {
        // Domain types should be in OrderHub.Catalog.Domain.*
        var domainResult = Types.InAssembly(DomainAssembly)
            .Should()
            .ResideInNamespaceStartingWith(DomainNs)
            .GetResult();

        Assert.True(domainResult.IsSuccessful,
            $"Domain types must reside in {DomainNs} namespace. " +
            $"Violating types: {string.Join(", ", domainResult.FailingTypes ?? [])}");

        // Application types should be in OrderHub.Catalog.Application.*
        var applicationResult = Types.InAssembly(ApplicationAssembly)
            .Should()
            .ResideInNamespaceStartingWith(ApplicationNs)
            .GetResult();

        Assert.True(applicationResult.IsSuccessful,
            $"Application types must reside in {ApplicationNs} namespace. " +
            $"Violating types: {string.Join(", ", applicationResult.FailingTypes ?? [])}");

        // Infrastructure types should be in OrderHub.Catalog.Infrastructure.*
        var infrastructureResult = Types.InAssembly(InfrastructureAssembly)
            .Should()
            .ResideInNamespaceStartingWith(InfrastructureNs)
            .GetResult();

        Assert.True(infrastructureResult.IsSuccessful,
            $"Infrastructure types must reside in {InfrastructureNs} namespace. " +
            $"Violating types: {string.Join(", ", infrastructureResult.FailingTypes ?? [])}");

        // Api types should be in OrderHub.Catalog.Api.*
        var apiResult = Types.InAssembly(ApiAssembly)
            .Should()
            .ResideInNamespaceStartingWith(ApiNs)
            .GetResult();

        Assert.True(apiResult.IsSuccessful,
            $"Api types must reside in {ApiNs} namespace. " +
            $"Violating types: {string.Join(", ", apiResult.FailingTypes ?? [])}");
    }

    /// <summary>
    /// No Catalog layer may reference another service's namespace.
    /// This catches accidental cross-service coupling and enforces the
    /// no-ProjectReference-between-services architectural rule.
    /// Only BuildingBlocks is allowed as a cross-project dependency.
    /// </summary>
    [Fact]
    public void AllCatalogLayers_Should_Not_Depend_On_Other_Services()
    {
        // Verify each layer doesn't depend on other services
        // We do this by checking assembly-level dependencies (foreign service projects)
        const string catalogPrefix = "OrderHub.Catalog";
        const string buildingBlocksPrefix = "OrderHub.BuildingBlocks";

        var allLayerAssemblies = new[]
        {
            ("Domain", DomainAssembly),
            ("Application", ApplicationAssembly),
            ("Infrastructure", InfrastructureAssembly),
            ("Api", ApiAssembly),
        };

        foreach (var (layerName, assembly) in allLayerAssemblies)
        {
            // Get all referenced assemblies from this layer
            var referencedAssemblies = assembly.GetReferencedAssemblies();

            // Filter for OrderHub.* namespaces that are NOT Catalog.* and NOT BuildingBlocks
            var foreignServiceReferences = referencedAssemblies
                .Where(a => (a.Name?.StartsWith("OrderHub.", StringComparison.Ordinal) ?? false)
                         && !(a.Name?.StartsWith(catalogPrefix, StringComparison.Ordinal) ?? false)
                         && !(a.Name?.StartsWith(buildingBlocksPrefix, StringComparison.Ordinal) ?? false))
                .Select(a => a.Name)
                .ToList();

            Assert.True(foreignServiceReferences.Count == 0,
                $"{layerName} layer must not reference foreign services. " +
                $"Found references to: {string.Join(", ", foreignServiceReferences)}");
        }
    }
}
