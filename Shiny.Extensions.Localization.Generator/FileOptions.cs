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
        //var classNameFromFileName = Utilities.GetClassNameFromPath(resxFilePath);

        //var detectedNamespace = Utilities.GetLocalNamespace(
        //    resxFilePath,
        //    options.TryGetValue("build_metadata.EmbeddedResource.Link", out var link) &&
        //    link is { Length: > 0 }
        //        ? link
        //        : null,
        //    globalOptions.ProjectFullPath,
        //    globalOptions.ProjectName,
        //    globalOptions.RootNamespace);

        this.FileNamespace = Utils.GetLocalNamespace(
            additionalText.Path,
            globalOptions.ProjectFullPath,
            globalOptions.ProjectName,
            globalOptions.RootNamespace
        );
        this.FileContent = additionalText.GetText()!.ToString();

        this.AssociatedClassName = Utils.GetClassNameFromPath(additionalText.Path);
        this.LocalizedClassName = this.AssociatedClassName + "Localized";
    }


    public string AssociatedClassName { get; }
    public string LocalizedClassName { get; }
    public string FileNamespace { get; }
    public string FileContent { get; }


    public static FileOptions Select(
        AdditionalText additionalText,
        AnalyzerConfigOptionsProvider options,
        GlobalOptions globalOptions
    ) => new FileOptions(
        additionalText,
        options.GetOptions(additionalText),
        globalOptions
    );
}