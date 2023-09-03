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
    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => this.options;
}
