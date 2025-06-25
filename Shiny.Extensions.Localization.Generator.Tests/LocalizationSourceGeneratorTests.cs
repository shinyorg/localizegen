using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using Xunit.Abstractions;
using Xunit;

namespace Shiny.Extensions.Localization.Generator.Tests;


public class LocalizationSourceGeneratorTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("MyTest", "MyTest.Core", false)]
    [InlineData(null, "MyTest", false)]
    [InlineData(null, "MyTest.Library", false)]
    [InlineData(null, "MyTest.Library", true)]
    public async Task EndToEndTest(string? rootNamespace, string projectName, bool generateInternal)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests"
        );
        var generator = new LocalizationSourceGenerator().AsSourceGenerator();
        var options = new TestAnalyzerConfigOptionsProvider();
        options.Options.Add("build_property.MSBuildProjectFullPath", "Shiny.Extensions.Localization.Generator");
        options.Options.Add("build_property.MSBuildProjectName", projectName);
        options.Options.Add("build_property.GenerateLocalizersInternal", generateInternal.ToString().ToLowerInvariant());
        if (rootNamespace != null)
            options.Options.Add("build_property.RootNamespace", rootNamespace);

        var resource1 = new ResxAdditionalText("Strings.resx");
        resource1.AddString("LocalizeKey", "This is a test");
        resource1.AddString("Localized Space", "This is a test with spaces");
        resource1.AddString("Localized  Space Multiple", "This is a test with multiple spaces");
        resource1.AddString("MyNamespace.MyEnum.MyEnumValue", "This is an enum value");

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [generator],
            optionsProvider: options,
            additionalTexts: ImmutableArray.Create<AdditionalText>(resource1)
        );

        driver = driver.RunGenerators(compilation);
        var results = driver.GetRunResult();

        await Verify(results)
            .UseParameters(
                rootNamespace, 
                projectName,
                generateInternal
            );
    }

    //[Fact]
    //public void EndToEndTest()
    //{
    //    var services = new ServiceCollection();
    //    services.AddLocalization();
    //    services.AddStrongTypedLocalizations();
    //    var sp = services.BuildServiceProvider();

    //    sp.GetRequiredService<MyClassLocalized>().Should().NotBeNull("MyClass localization missing in registration");
    //    sp.GetRequiredService<FolderTest1Localized>().Should().NotBeNull("FolderTest1 localization missing in registration");
    //    sp.GetRequiredService<FolderTest2Localized>().Should().NotBeNull("FolderTest2 localization missing in registration");

    //    // TODO: ensure keys are set
    //    // TODO: check keys with spaces
    //}
}

