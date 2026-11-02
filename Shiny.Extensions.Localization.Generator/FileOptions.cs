using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Shiny.Extensions.Localization.Generator;


public class FileOptions
{
    public FileOptions(
        AdditionalText additionalText,
        AnalyzerConfigOptions options,
        GlobalOptions globalOptions
    )
    {
        this.AdditionalText = additionalText;
        this.GlobalOptions = globalOptions;
        this.FilePath = additionalText.Path;
        this.NameWithoutExtension = Path.GetFileNameWithoutExtension(additionalText.Path);

        // set by MSBuild's AssignCulture task - the same answer MSBuild uses to build satellite assemblies
        if (options.TryGetValue("build_metadata.EmbeddedResource.WithCulture", out var withCulture) && !String.IsNullOrWhiteSpace(withCulture))
            this.WithCulture = withCulture.Equals("true", StringComparison.OrdinalIgnoreCase);

        if (options.TryGetValue("build_metadata.EmbeddedResource.Culture", out var culture) && !String.IsNullOrWhiteSpace(culture))
            this.Culture = culture;

        var folderNamespace = Utils.GetLocalNamespace(
            additionalText.Path,
            globalOptions.ProjectFullPath ?? String.Empty,
            globalOptions.ProjectName ?? String.Empty,
            globalOptions.RootNamespace
        );

        // Errors.Validation.resx -> namespace {folder}.Errors, class Validation
        // this matches the manifest resource name MSBuild embeds ({RootNamespace}.{Folder}.Errors.Validation)
        // which is what IStringLocalizer<T> looks up from T's full name
        var lastDot = this.NameWithoutExtension.LastIndexOf('.');
        if (lastDot < 0)
        {
            this.FileNamespace = folderNamespace;
            this.AssociatedClassName = this.NameWithoutExtension;
        }
        else
        {
            var prefix = Utils.SanitizeNamespace(this.NameWithoutExtension.Substring(0, lastDot));
            this.FileNamespace = String.IsNullOrEmpty(folderNamespace) ? prefix : $"{folderNamespace}.{prefix}";
            this.AssociatedClassName = this.NameWithoutExtension.Substring(lastDot + 1);
        }
        this.LocalizedClassName = this.AssociatedClassName + "Localized";
    }


    public AdditionalText AdditionalText { get; }
    public GlobalOptions GlobalOptions { get; }
    public string FilePath { get; }
    public string NameWithoutExtension { get; }

    /// <summary>
    /// MSBuild's verdict on whether this is a culture file - null when the build didn't supply it
    /// </summary>
    public bool? WithCulture { get; }
    public string? Culture { get; }

    // only meaningful for default (non-culture) files
    public string AssociatedClassName { get; }
    public string LocalizedClassName { get; }
    public string FileNamespace { get; }
    public string FileContent => this.AdditionalText.GetText()!.ToString();


    public static FileOptions Select(
        AdditionalText additionalText,
        AnalyzerConfigOptionsProvider options,
        GlobalOptions globalOptions
    ) => new(
        additionalText,
        options.GetOptions(additionalText),
        globalOptions
    );
}
