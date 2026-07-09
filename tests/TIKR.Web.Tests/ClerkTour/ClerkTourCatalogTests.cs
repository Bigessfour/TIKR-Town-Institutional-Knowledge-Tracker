using TIKR.Web.ClerkTour;

namespace TIKR.Web.Tests.ClerkTour;

public sealed class ClerkTourCatalogTests
{
    [Fact]
    public void Full_tour_steps_reference_defined_tour_ids()
    {
        var ids = typeof(ClerkTourIds)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.IsLiteral && !f.IsInitOnly)
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToHashSet();

        foreach (var step in ClerkTourCatalog.GetFullTourSteps())
        {
            var id = step.Element.Replace("[data-tour='", "", StringComparison.Ordinal)
                .TrimEnd('\'', ']');
            Assert.True(ids.Contains(id), $"Unknown tour id in catalog: {id}");
        }
    }

    [Fact]
    public void Each_clerk_route_has_page_tour_steps()
    {
        string[] routes = ["/", "/requirements", "/calendar", "/documents", "/assistant", "/vault", "/settings"];
        foreach (var route in routes)
        {
            Assert.True(ClerkTourCatalog.GetPageSteps(route).Count >= 2, $"Expected page steps for {route}");
        }
    }

    [Fact]
    public void CurrentVersion_is_set_for_deployment_bump()
    {
        Assert.Equal("v2", ClerkTourCatalog.CurrentVersion);
        Assert.True(ClerkTourCatalog.GetFullTourSteps().Count >= 20);
    }
}
