using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using Xunit.Abstractions;
using Xunit;

namespace Shiny.Extensions.Localization.Generator.Tests;


public class LocalizationSourceGeneratorTests
{
    readonly ITestOutputHelper output;


    public LocalizationSourceGeneratorTests(ITestOutputHelper output)
    {
        this.output = output;
    }


    [Theory]
    [InlineData("MyTest", "MyTest.Core")]
    [InlineData(null, "MyTest")]
    [InlineData(null, "MyTest.Library")]
    public void EndToEndTest(string? rootNamespace, string projectName)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests"
        );
        var generator = new LocalizationSourceGenerator().AsSourceGenerator();
        var options = new TestAnalyzerConfigOptionsProvider();
        options.Options.Add("build_property.MSBuildProjectFullPath", "Shiny.Extensions.Localization.Generator");
        options.Options.Add("build_property.MSBuildProjectName", projectName);
        if (rootNamespace != null)
            options.Options.Add("build_property.RootNamespace", rootNamespace);
        

        var resource1 = new ResxAdditionalText("Strings.resx");
        resource1.AddString("LocalizeKey", "This is a test");

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new[] { generator },
            optionsProvider: options,
            additionalTexts: ImmutableArray.Create<AdditionalText>(resource1)
        );

        driver = driver.RunGenerators(compilation);
        var results = driver.GetRunResult();

        foreach (var result in results.GeneratedTrees)
        {
            var source = result.GetText().ToString();

            this.output.WriteLine("File: " + result.FilePath);
            this.output.WriteLine("Source: " + source);
        }
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

