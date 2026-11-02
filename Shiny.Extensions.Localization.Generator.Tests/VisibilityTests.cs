using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using Xunit;
using Xunit.Abstractions;

namespace Shiny.Extensions.Localization.Generator.Tests;

// https://github.com/shinyorg/localizegen/issues/1 - generated localizers follow the visibility of their associated class
public class VisibilityTests(ITestOutputHelper output)
{
    const string UserCode = """
namespace MyApp;

// internal service class with its own Service.resx
internal class Service
{
    public Service(ServiceLocalized l) { }
}

// Blazor components must be public; injects its localizer via a public [Inject] property
public class Component
{
    public ComponentLocalized L { get; set; } = default!;
}
""";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MixedVisibility(bool generateInternal)
    {
        var refs = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(x => MetadataReference.CreateFromFile(x))
            .Cast<MetadataReference>()
            .ToList();
        refs.Add(MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.Localization.IStringLocalizer).Assembly.Location));
        refs.Add(MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.DependencyInjection.LocalizationServiceCollectionExtensions).Assembly.Location));
        refs.Add(MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly.Location));

        var compilation = CSharpCompilation.Create(
            "MyApp",
            [CSharpSyntaxTree.ParseText(UserCode)],
            refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
        var options = new TestAnalyzerConfigOptionsProvider();
        options.Options.Add("build_property.MSBuildProjectFullPath", "MyApp");
        options.Options.Add("build_property.MSBuildProjectName", "MyApp");
        options.Options.Add("build_property.GenerateLocalizersInternal", generateInternal.ToString().ToLowerInvariant());

        var service = new ResxAdditionalText("Service.resx").AddString("Hello", "Hello");
        var component = new ResxAdditionalText("Component.resx").AddString("Title", "Title");

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new LocalizationSourceGenerator().AsSourceGenerator()],
            optionsProvider: options,
            additionalTexts: ImmutableArray.Create<AdditionalText>(service, component)
        );
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outComp, out _);

        var errors = outComp.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        foreach (var e in errors)
            output.WriteLine(e.ToString());

        Assert.Empty(errors);
    }
}
