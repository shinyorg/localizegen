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

        AssertNoWarnings(run);
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
        AssertNoWarnings(run);
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
        var info = Assert.Single(run.Diagnostics);
        Assert.Equal("SHINY0003", info.Id);
        Assert.Equal(DiagnosticSeverity.Info, info.Severity);
        Assert.Equal("'Page.aspx.resx': 'aspx' is not a culture, so it is a default resource file and generates localizer 'MyTest.Page.aspxLocalized'", info.GetMessage());
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


    // https://github.com/shinyorg/localizegen/issues/6 - a dotted name whose namespace lands on a type must not emit CS0101
    [Theory]
    [InlineData(true)]  // Strings.Designer.cs from PublicResXFileCodeGenerator
    [InlineData(false)] // the generator's own stub for Strings.resx
    public void DottedNameCollidingWithTypeReportsErrorInsteadOfCS0101(bool userDeclaresStrings)
    {
        var (run, errors) = Run(
            [
                new ResxAdditionalText("Strings.resx").AddString("Hello", "Hello"),
                new ResxAdditionalText("Strings.orig.resx").AddString("Hello", "Hello")
            ],
            userCode: userDeclaresStrings ? "namespace MyTest { public class Strings { } }" : null
        );

        Assert.Empty(errors); // the user's build fails on SHINY0002 alone, not on generated code
        var error = Assert.Single(run.Diagnostics, x => x.Id == "SHINY0002");
        Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        Assert.Equal("Strings.orig.resx", error.Location.GetLineSpan().Path);
        Assert.Contains("'Strings.orig.resx' would generate namespace 'MyTest.Strings', which conflicts with", error.GetMessage());

        var hints = run.Results[0].GeneratedSources.Select(x => x.HintName).ToList();
        Assert.Contains("MyTest.StringsLocalized.g.cs", hints);
        Assert.DoesNotContain(hints, x => x.Contains("orig"));
        var registry = Source(run, "GeneratedLocalizers.g.cs");
        Assert.DoesNotContain("orig", registry);
    }


    [Fact]
    public void GeneratedTypeCollidingWithUserNamespaceReportsError()
    {
        var (run, errors) = Run(
            [new ResxAdditionalText("Strings.resx").AddString("Hello", "Hello")],
            userCode: "namespace MyTest.Strings { class Helper { } }"
        );

        Assert.Empty(errors);
        var error = Assert.Single(run.Diagnostics, x => x.Id == "SHINY0002");
        Assert.Equal("'Strings.resx' would generate type 'MyTest.Strings', which conflicts with namespace 'MyTest.Strings', so no localizer was generated for it. Rename or remove the file.", error.GetMessage());
        Assert.DoesNotContain(run.Results[0].GeneratedSources, x => x.HintName.Contains("StringsLocalized"));
    }


    [Fact]
    public void DottedNameWithoutCollisionStillGenerates()
    {
        // Errors.Validation.resx -> namespace MyTest.Errors is fine as long as nothing named MyTest.Errors is a type
        var (run, errors) = Run(
            [
                new ResxAdditionalText("Strings.resx").AddString("Hello", "Hello"),
                new ResxAdditionalText("Errors.Validation.resx").AddString("Required", "Required")
            ],
            userCode: "namespace MyTest { public class Strings { } }"
        );

        Assert.Empty(errors);
        Assert.DoesNotContain(run.Diagnostics, x => x.Id == "SHINY0002");
        Assert.Contains(run.Results[0].GeneratedSources, x => x.HintName == "MyTest.Errors.ValidationLocalized.g.cs");
    }


    // https://github.com/shinyorg/localizegen/issues/5 - shipped cultures without probing satellite assemblies
    [Fact]
    public void ShippedCulturesAreEmitted()
    {
        var (run, errors) = Run(
            [
                new ResxAdditionalText("Strings.resx").AddString("Hello", "Hello"),
                new ResxAdditionalText("Strings.fr-ca.resx").AddString("Hello", "Bonjour"),
                new ResxAdditionalText("Strings.de.resx").AddString("Hello", "Hallo"),
                new ResxAdditionalText("Errors.Validation.resx").AddString("Required", "Required"),
                new ResxAdditionalText("Errors.Validation.de.resx").AddString("Required", "Erforderlich"),
                new ResxAdditionalText("Orphan.es.resx").AddString("Hello", "Hola") // no Orphan.resx - not shipped
            ],
            userCode: "static class Probe { public static string[] Get() => MyTest.GeneratedLocalizers.Cultures; }"
        );

        Assert.Empty(errors);
        var registry = Source(run, "GeneratedLocalizers.g.cs");
        Assert.Contains("public static readonly string[] Cultures = new string[]\n\t{\n\t\t\"de\",\n\t\t\"fr-CA\",\n\t};", registry.Replace("\r\n", "\n"));
    }


    [Fact]
    public void ShippedCulturesUseMSBuildCulture()
    {
        var (run, _) = Run(
            [
                new ResxAdditionalText("Strings.resx").AddString("Hello", "Hello"),
                new ResxAdditionalText("Strings.xyz.resx").AddString("Hello", "Hello xyz")
            ],
            new()
            {
                ["Strings.resx"] = new() { ["build_metadata.EmbeddedResource.WithCulture"] = "false" },
                ["Strings.xyz.resx"] = new() { ["build_metadata.EmbeddedResource.WithCulture"] = "true", ["build_metadata.EmbeddedResource.Culture"] = "xyz" }
            }
        );

        Assert.Contains("\t\t\"xyz\",", Source(run, "GeneratedLocalizers.g.cs"));
    }


    [Fact]
    public void NoTranslationsEmitsEmptyCultures()
    {
        var (run, errors) = Run(
            [new ResxAdditionalText("Strings.resx").AddString("Hello", "Hello")],
            userCode: "static class Probe { public static int Count => MyTest.GeneratedLocalizers.Cultures.Length; }"
        );

        Assert.Empty(errors);
        Assert.Contains("public static readonly string[] Cultures = new string[]\n\t{\n\t};", Source(run, "GeneratedLocalizers.g.cs").Replace("\r\n", "\n"));
    }


    [Fact]
    public void ClassifyReturnsDistinctNormalizedCultures()
    {
        var options = new TestAnalyzerConfigOptionsProvider();
        options.Options.Add("build_property.MSBuildProjectFullPath", "Shiny.Extensions.Localization.Generator");
        options.Options.Add("build_property.MSBuildProjectName", "MyTest");
        var global = new GlobalOptions(options.GlobalOptions);
        var files = new[] { "Strings.resx", "Strings.FR-ca.resx", "Other.resx", "Other.fr-CA.resx", "Orphan.de.resx" }
            .Select(x => FileOptions.Select(new ResxAdditionalText(x), options, global));

        var result = ResxClassifier.Classify(files, ["fr-CA", "de"]);

        Assert.Equal(["fr-CA"], result.Cultures);
        Assert.Single(result.OrphanedCultureFiles);
    }


    static string Source(GeneratorDriverRunResult run, string hint)
        => run.Results[0].GeneratedSources.Single(x => x.HintName == hint).SourceText.ToString();


    static void AssertNoWarnings(GeneratorDriverRunResult run)
        => Assert.DoesNotContain(run.Diagnostics, x => x.Severity >= DiagnosticSeverity.Warning);


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
        string projectPath = "Shiny.Extensions.Localization.Generator",
        string? userCode = null
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
            syntaxTrees: userCode == null ? null : [CSharpSyntaxTree.ParseText(userCode)],
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
