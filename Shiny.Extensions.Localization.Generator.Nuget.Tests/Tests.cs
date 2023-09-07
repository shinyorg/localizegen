using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Shiny.Extensions.Localization.Generator.Nuget.Tests;


public class Tests
{
    [Fact]
    public void EndToEnd()
    {
        var services = new ServiceCollection();
        //services.AddStronglyTypedLocalizations();
        var sp = services.BuildServiceProvider();
        //sp.GetRequiredService<ResourcesLocalized>().LocalizeKey.Should().Be("This is test localization");
    }
}

