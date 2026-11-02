using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using Xunit;

namespace Shiny.Extensions.Localization.Generator.Tests;

// https://github.com/shinyorg/localizegen/issues/3 - dotted default resource names and culture file detection
public class CultureFileTests
{
    [Fact]
    public void DottedDefaultFileGeneratesIntoNamespaceSegment()
    {
        var (run, errors) = Run([
            new ResxAdditionalText("Errors.Validation.resx").AddString("Required", "Required"),
            new ResxAdditionalText("Errors.Validation.fr-CA.resx").AddString("Required", "Requis")
        ]);

        var hints = run.Results[0].GeneratedSources.Select(x => x.HintName).ToList();
        Assert.Contains("MyTest.Errors.ValidationLocalized.g.cs", hints);
        Assert.Contains("MyTest.Errors.Validation.g.cs", hints);
        Assert.DoesNotContain(hints, x => x.Contains("fr", StringComparison.OrdinalIgnoreCase));

        var registry = run.Results[0].GeneratedSources.Single(x => x.HintName == "GeneratedLocalizers.g.cs").SourceText.ToString();
        Assert.Contains("(\"Errors.Validation\", typeof(global::MyTest.Errors.Validation), typeof(global::MyTest.Errors.ValidationLocalized))", registry);

        Assert.Empty(run.Diagnostics);
        Assert.Empty(errors);
    }


    [Fact]
    public void CultureFilesAreNotGenerated()
    {
        var (run, errors) = Run([
            new ResxAdditionalText("Strings.resx").AddString("Hello", "Hello"),
            new ResxAdditionalText("Strings.de.resx").AddString("Hello", "Hallo"),
            new ResxAdditionalText("Strings.zh-Hans.resx").AddString("Hello", "你好")
        ]);

        var localizers = run.Results[0].GeneratedSources.Where(x => x.HintName.EndsWith("Localized.g.cs")).Select(x => x.HintName);
        Assert.Equal(["MyTest.StringsLocalized.g.cs"], localizers);
        Assert.Empty(run.Diagnostics);
        Assert.Empty(errors);
    }


    [Fact]
    public void OrphanedCultureFileWarns()
    {
        var (run, _) = Run([
            new ResxAdditionalText("Strings.resx").AddString("Hello", "Hello"),
            new ResxAdditionalText("Orphan.de.resx").AddString("Hello", "Hallo")
        ]);

        var diagnostic = Assert.Single(run.Diagnostics);
        Assert.Equal("SHINY0001", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("'Orphan.de.resx' is a 'de' culture file, but 'Orphan.resx' does not exist", diagnostic.GetMessage());
        Assert.DoesNotContain(run.Results[0].GeneratedSources, x => x.HintName.Contains("Orphan"));
    }


    [Fact]
    public void MSBuildCultureMetadataWins()
    {
        // "xyz" is not a culture this machine knows - MSBuild saying it is must still be respected
        var (run, errors) = Run(
            [
                new ResxAdditionalText("Strings.resx").AddString("Hello", "Hello"),
                new ResxAdditionalText("Strings.xyz.resx").AddString("Hello", "Hello xyz")
            ],
            new()
            {
                ["Strings.resx"] = new() { ["build_metadata.EmbeddedResource.WithCulture"] = "false", ["build_metadata.EmbeddedResource.Culture"] = "" },
                ["Strings.xyz.resx"] = new() { ["build_metadata.EmbeddedResource.WithCulture"] = "true", ["build_metadata.EmbeddedResource.Culture"] = "xyz" }
            }
        );

        var localizers = run.Results[0].GeneratedSources.Where(x => x.HintName.EndsWith("Localized.g.cs")).Select(x => x.HintName);
        Assert.Equal(["MyTest.StringsLocalized.g.cs"], localizers);
        Assert.Empty(run.Diagnostics);
        Assert.Empty(errors);
    }


    [Fact]
    public void DottedFileInFolderCombinesFolderAndNameSegments()
    {
        var (run, errors) = Run(
            [new ResxAdditionalText("/proj/Features/Errors.Validation.resx").AddString("Required", "Required")],
            projectPath: "/proj/MyTest.csproj"
        );

        var hints = run.Results[0].GeneratedSources.Select(x => x.HintName).ToList();
        Assert.True(hints.Contains("MyTest.Features.Errors.ValidationLocalized.g.cs"), String.Join(", ", hints));
        Assert.Contains("MyTest.Features.Errors.Validation.g.cs", hints);
        Assert.Empty(errors);
    }


    [Fact]
    public void MultipleDotsBecomeMultipleNamespaceSegments()
    {
        var (run, errors) = Run([new ResxAdditionalText("A.B.Strings.resx").AddString("Hello", "Hello")]);

        Assert.Contains(run.Results[0].GeneratedSources, x => x.HintName == "MyTest.A.B.StringsLocalized.g.cs");
        Assert.Empty(errors);
    }


    [Fact]
    public void SameClassNameInDifferentDottedFilesStaysUnique()
    {
        var (run, errors) = Run([
            new ResxAdditionalText("Errors.Validation.resx").AddString("Hello", "Hello"),
            new ResxAdditionalText("Forms.Validation.resx").AddString("Hello", "Hello")
        ]);

        var registry = run.Results[0].GeneratedSources.Single(x => x.HintName == "GeneratedLocalizers.g.cs").SourceText.ToString();
        Assert.Contains("(\"Errors.Validation\", typeof(global::MyTest.Errors.Validation)", registry);
        Assert.Contains("(\"Forms.Validation\", typeof(global::MyTest.Forms.Validation)", registry);
        Assert.Empty(errors);
    }


    [Fact]
    public void MSBuildSayingNotACultureWins()
    {
        // "de" is a real culture, but MSBuild's WithCulture=false is what the build does with the file
        var (run, errors) = Run(
            [new ResxAdditionalText("Strings.de.resx").AddString("Hello", "Hello")],
            new() { ["Strings.de.resx"] = new() { ["build_metadata.EmbeddedResource.WithCulture"] = "false" } }
        );

        Assert.Contains(run.Results[0].GeneratedSources, x => x.HintName == "MyTest.Strings.deLocalized.g.cs");
        Assert.Empty(run.Diagnostics);
        Assert.Empty(errors);
    }


    [Fact]
    public void EmptyMSBuildMetadataFallsBackToCultureNames()
    {
        // design-time builds may hand us the keys with no values
        var (run, _) = Run(
            [
                new ResxAdditionalText("Strings.resx").AddString("Hello", "Hello"),
                new ResxAdditionalText("Strings.de.resx").AddString("Hello", "Hallo")
            ],
            new() { ["Strings.de.resx"] = new() { ["build_metadata.EmbeddedResource.WithCulture"] = "", ["build_metadata.EmbeddedResource.Culture"] = "" } }
        );

        var localizers = run.Results[0].GeneratedSources.Where(x => x.HintName.EndsWith("Localized.g.cs")).Select(x => x.HintName);
        Assert.Equal(["MyTest.StringsLocalized.g.cs"], localizers);
    }


    [Fact]
    public void OrphanedDottedCultureFileFromMSBuildNamesItsDefaultFile()
    {
        var (run, _) = Run(
            [new ResxAdditionalText("Errors.Validation.fr-CA.resx").AddString("Required", "Requis")],
            new() { ["Errors.Validation.fr-CA.resx"] = new() { ["build_metadata.EmbeddedResource.WithCulture"] = "true", ["build_metadata.EmbeddedResource.Culture"] = "fr-CA" } }
        );

        var orphan = Assert.Single(run.Diagnostics, x => x.Id == "SHINY0001");
        Assert.Contains("'Errors.Validation.fr-CA.resx' is a 'fr-CA' culture file, but 'Errors.Validation.resx' does not exist", orphan.GetMessage());
        Assert.Equal("Errors.Validation.fr-CA.resx", orphan.Location.GetLineSpan().Path);
    }


    [Fact]
    public void OnlyCultureFilesReportsNoFiles()
    {
        var (run, _) = Run([new ResxAdditionalText("Orphan.de.resx").AddString("Hello", "Hallo")]);

        Assert.Contains(run.Diagnostics, x => x.Id == "SHINY0001");
        Assert.Contains(run.Diagnostics, x => x.Id == "SHINY0000");
        Assert.DoesNotContain(run.Results[0].GeneratedSources, x => x.HintName.EndsWith("Localized.g.cs"));
    }


    [Fact]
    public void UnknownLastSegmentIsADefaultFile()
    {
        // "aspx" isn't a culture, so this is a default file named after the manifest resource MSBuild embeds
        var (run, errors) = Run([new ResxAdditionalText("Page.aspx.resx").AddString("Hello", "Hello")]);

        Assert.Contains(run.Results[0].GeneratedSources, x => x.HintName == "MyTest.Page.aspxLocalized.g.cs");
        Assert.Empty(run.Diagnostics);
        Assert.Empty(errors);
    }


    // no culture data on the machine (invariant globalization) - falls back to "does the default file exist"
    [Fact]
    public void NoCultureData_SiblingDefaultFileMarksCultureFile()
    {
        var defaults = Classify([], ["Strings.resx", "Strings.de.resx", "Errors.Validation.resx"], out var orphans);

        Assert.Equal(["Strings.resx", "Errors.Validation.resx"], defaults);
        Assert.Empty(orphans);
    }


    [Fact]
    public void NoCultureData_NoSiblingIsADefaultFile()
    {
        // can't tell Orphan.de.resx from Errors.Validation.resx without culture data - generating beats skipping silently
        var defaults = Classify([], ["Orphan.de.resx"], out var orphans);

        Assert.Equal(["Orphan.de.resx"], defaults);
        Assert.Empty(orphans);
    }


    [Fact]
    public void KnownCultures_MatchIsCaseInsensitive()
    {
        var defaults = Classify(["fr-CA"], ["Strings.resx", "Strings.FR-ca.resx"], out var orphans);

        Assert.Equal(["Strings.resx"], defaults);
        Assert.Empty(orphans);
    }


    static List<string> Classify(string[] knownCultures, string[] paths, out List<OrphanedCultureFile> orphans)
    {
        var options = new TestAnalyzerConfigOptionsProvider();
        options.Options.Add("build_property.MSBuildProjectFullPath", "Shiny.Extensions.Localization.Generator");
        options.Options.Add("build_property.MSBuildProjectName", "MyTest");
        var global = new GlobalOptions(options.GlobalOptions);

        var files = paths.Select(x => FileOptions.Select(new ResxAdditionalText(x), options, global));
        return ResxClassifier
            .GetDefaultFiles(files, out orphans, knownCultures)
            .Select(x => x.FilePath)
            .ToList();
    }


    static (GeneratorDriverRunResult Run, List<Diagnostic> Errors) Run(
        ResxAdditionalText[] files,
        Dictionary<string, Dictionary<string, string>>? fileOptions = null,
        string projectPath = "Shiny.Extensions.Localization.Generator"
    )
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Concat([
                typeof(Microsoft.Extensions.Localization.IStringLocalizer).Assembly.Location,
                typeof(Microsoft.Extensions.DependencyInjection.LocalizationServiceCollectionExtensions).Assembly.Location,
                typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly.Location,
                typeof(Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions).Assembly.Location
            ])
            .Distinct()
            .Select(x => MetadataReference.CreateFromFile(x));

        var compilation = CSharpCompilation.Create(
            "MyTest",
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
        var options = new TestAnalyzerConfigOptionsProvider();
        options.Options.Add("build_property.MSBuildProjectFullPath", projectPath);
        options.Options.Add("build_property.MSBuildProjectName", "MyTest");
        options.Options.Add("build_property.RootNamespace", "MyTest"); // the SDK always sets this
        if (fileOptions != null)
            options.FileOptions = fileOptions;

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new LocalizationSourceGenerator().AsSourceGenerator()],
            optionsProvider: options,
            additionalTexts: ImmutableArray.Create<AdditionalText>(files)
        );
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);

        var errors = output.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error).ToList();
        return (driver.GetRunResult(), errors);
    }
}
