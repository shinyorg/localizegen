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
    
    [Fact]
    public Task NoResourcesAddEmptyExtensionMethod()
    {
        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests"
        );
        var generator = new LocalizationSourceGenerator().AsSourceGenerator();
        var options = new TestAnalyzerConfigOptionsProvider();
        options.Options.Add("build_property.MSBuildProjectFullPath", "Shiny.Extensions.Localization.Generator");
        options.Options.Add("build_property.MSBuildProjectName", "MyTest.Core");

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [generator],
            optionsProvider: options
        );

        driver = driver.RunGenerators(compilation);
        var results = driver.GetRunResult();

        return Verify(results);
    }
    
    
    [Fact]
    public async Task FormatParameterGenerationTest()
    {
        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests"
        );
        var generator = new LocalizationSourceGenerator().AsSourceGenerator();
        var options = new TestAnalyzerConfigOptionsProvider();
        options.Options.Add("build_property.MSBuildProjectFullPath", "Shiny.Extensions.Localization.Generator");
        options.Options.Add("build_property.MSBuildProjectName", "FormatTest");

        var resource1 = new ResxAdditionalText("Messages.resx");
        // Simple strings should generate properties
        resource1.AddString("SimpleMessage", "Hello World");
        resource1.AddString("AnotherSimple", "Welcome to our application");
        
        // Format strings should generate methods
        resource1.AddString("MessageWithOneParameter", "Hello {0}");
        resource1.AddString("MessageWithTwoParameters", "Hello {0}, you have {1} messages");
        resource1.AddString("MessageWithThreeParameters", "User {0} logged in at {1} from {2}");
        
        // Complex format scenarios
        resource1.AddString("MessageWithNonSequentialParameters", "Value {2} comes before {0} and {1}");
        resource1.AddString("MessageWithRepeatedParameters", "Hello {0}, {0} is a nice name!");
        resource1.AddString("MessageWithMixedContent", "Welcome {0}! Today is a great day.");

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [generator],
            optionsProvider: options,
            additionalTexts: ImmutableArray.Create<AdditionalText>(resource1)
        );

        driver = driver.RunGenerators(compilation);
        var results = driver.GetRunResult();

        await Verify(results)
            .UseMethodName("FormatParameterGenerationTest");
    }
    
    [Fact]
    public async Task EdgeCaseFormatParameterTest()
    {
        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests"
        );
        var generator = new LocalizationSourceGenerator().AsSourceGenerator();
        var options = new TestAnalyzerConfigOptionsProvider();
        options.Options.Add("build_property.MSBuildProjectFullPath", "Shiny.Extensions.Localization.Generator");
        options.Options.Add("build_property.MSBuildProjectName", "EdgeCaseTest");

        var resource1 = new ResxAdditionalText("EdgeCases.resx");
        // Edge cases that should still generate properties (no valid format parameters)
        resource1.AddString("BracesButNotFormat", "This has {braces} but not format parameters");
        resource1.AddString("EmptyBraces", "This has {} empty braces");
        resource1.AddString("InvalidFormat", "This has {abc} invalid format");
        resource1.AddString("MixedValidInvalid", "Valid {0} and invalid {abc} parameters");
        
        // Edge cases that should generate methods
        resource1.AddString("SingleParameter", "Only {0} parameter");
        resource1.AddString("HighNumberParameter", "Parameter with high number {10}");
        resource1.AddString("ZeroParameter", "Starting with {0}");

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [generator],
            optionsProvider: options,
            additionalTexts: ImmutableArray.Create<AdditionalText>(resource1)
        );

        driver = driver.RunGenerators(compilation);
        var results = driver.GetRunResult();

        await Verify(results)
            .UseMethodName("EdgeCaseFormatParameterTest");
    }
    
    [Fact]
    public async Task FormatParametersWithFormattingOptionsTest()
    {
        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests"
        );
        var generator = new LocalizationSourceGenerator().AsSourceGenerator();
        var options = new TestAnalyzerConfigOptionsProvider();
        options.Options.Add("build_property.MSBuildProjectFullPath", "Shiny.Extensions.Localization.Generator");
        options.Options.Add("build_property.MSBuildProjectName", "FormattingTest");

        var resource1 = new ResxAdditionalText("Formatting.resx");
        
        // Format strings with formatting options - should generate methods
        resource1.AddString("DateFormatMessage", "Today is {0:MMM dd yyyy}");
        resource1.AddString("CurrencyFormatMessage", "Total cost: {0:C2}");
        resource1.AddString("PercentageFormatMessage", "Success rate: {0:P2}");
        resource1.AddString("NumberFormatMessage", "Value: {0:N2}");
        resource1.AddString("CustomDateTimeFormat", "Event scheduled for {0:yyyy-MM-dd HH:mm:ss}");
        
        // Multiple parameters with different formatting
        resource1.AddString("MixedFormattingMessage", "Date: {0:MMM dd yyyy}, Amount: {1:C2}, Rate: {2:P1}");
        resource1.AddString("DateRangeMessage", "From {0:yyyy-MM-dd} to {1:yyyy-MM-dd}");
        
        // Complex formatting with text
        resource1.AddString("DetailedReportMessage", "Report generated on {0:dddd, MMMM dd, yyyy} at {1:HH:mm:ss} for user {2}");
        
        // Formatting with repeated parameters
        resource1.AddString("RepeatedFormattedParameter", "Start: {0:HH:mm}, End: {0:HH:mm:ss}");
        
        // Mixed - some with formatting, some without
        resource1.AddString("MixedParameterTypes", "User {0} logged in at {1:yyyy-MM-dd HH:mm} from location {2}");

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [generator],
            optionsProvider: options,
            additionalTexts: ImmutableArray.Create<AdditionalText>(resource1)
        );

        driver = driver.RunGenerators(compilation);
        var results = driver.GetRunResult();

        await Verify(results)
            .UseMethodName("FormatParametersWithFormattingOptionsTest");
    }
}
