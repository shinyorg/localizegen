using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Shiny.Extensions.Localization.Generator.Tests;


public class TestAnalyzerConfigOptions : AnalyzerConfigOptions
{
    readonly Func<string, string?> callback;

    public TestAnalyzerConfigOptions(Func<string, string?> callback)
        => this.callback = callback;


    public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
    {
        value = null;
        value = this.callback.Invoke(key);
        return value != null;
    }
}


public class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
{
    public Dictionary<string, string> Options { get; set; } = new();
    /// <summary>
    /// Per-file options keyed by AdditionalText.Path (e.g. build_metadata.EmbeddedResource.WithCulture), layered over Options
    /// </summary>
    public Dictionary<string, Dictionary<string, string>> FileOptions { get; set; } = new();
    readonly TestAnalyzerConfigOptions options;


    public TestAnalyzerConfigOptionsProvider()
    {
        this.options = new TestAnalyzerConfigOptions(key =>
        {
            if (this.Options.ContainsKey(key))
                return this.Options[key];

            return null;
        });
    }

    public override AnalyzerConfigOptions GlobalOptions => this.options;
    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => this.options;
    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
    {
        if (!this.FileOptions.TryGetValue(textFile.Path, out var fileOptions))
            return this.options;

        return new TestAnalyzerConfigOptions(key =>
        {
            if (fileOptions.TryGetValue(key, out var value))
                return value;

            return this.Options.TryGetValue(key, out value) ? value : null;
        });
    }
}
