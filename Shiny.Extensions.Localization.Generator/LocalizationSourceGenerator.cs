using System.Text;
using System.Resources.NetStandard;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Shiny.Extensions.Localization.Generator;


[Generator]
public class LocalizationSourceGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var globalOptions = context
            .AnalyzerConfigOptionsProvider
            .Select(GlobalOptions.Select);

		var resxFiles = context
			.AdditionalTextsProvider
			.Where(static x =>
			{
				// we only care about the default resource files, the ones
				// without the local Locale.resx vs Locale.fr-CA.resx
				if (!x.Path.EndsWith(".resx"))
					return false;

				var count = x.Path.ToCharArray().Count(y => y.Equals('.'));
				if (count != 1)
					return false;

				return true;
			});

		var inputs = resxFiles
			.Combine(globalOptions)
			.Combine(context.AnalyzerConfigOptionsProvider)
			.Select(static (x, _) => FileOptions.Select(
				x.Left.Left,
				x.Right,
				x.Left.Right
			));

		var monitor = context.CompilationProvider.Combine(inputs.Collect());

        context.RegisterSourceOutput(
			monitor,
			(productionContext, sourceContext) => Generate(productionContext, sourceContext)
		);
    }


    void Generate(SourceProductionContext context, (Compilation Compilation, ImmutableArray<FileOptions> Files) ctx)
	{
		if (!ctx.Files.Any())
		{
			context.LogDiagnostic("SHINY0000", "No Files found", "No .resx files were found to process.", DiagnosticSeverity.Warning);
			return;
		}
		
		var generatedClasses = new List<string>();
		var first = ctx.Files.First();
		var rootNamespace = first.GlobalOptions.RootNamespace ?? first.GlobalOptions.ProjectName!;

		foreach (var file in ctx.Files)
		{
			try
			{
				var generated = GenerateStronglyTypedClass(
					file.FileContent,
					file.FileNamespace,
					file.LocalizedClassName,
					file.AssociatedClassName
				);
				generatedClasses.Add($"{file.FileNamespace}.{file.LocalizedClassName}");
				context.AddSource($"{file.FileNamespace}.{file.LocalizedClassName}.g.cs", generated);
			}
			catch (Exception ex)
			{
				context.LogDiagnostic("SHINY0001", "Error Processing File", $"Processing File: {file.FileNamespace}, Error: {ex}", DiagnosticSeverity.Error);
			}
		}

        var generatedService = GenerateServiceCollectionRegistration(rootNamespace, generatedClasses);
		context.AddSource("ServiceCollectionExtensions.g.cs", generatedService);
    }


    static string GenerateStronglyTypedClass(
		string resxContent,
		string nameSpace,
		string className,
		string associatedResourceClassName
	)
	{
		var sb = new StringBuilder()
			.AppendLine($"namespace {nameSpace};")
			.AppendLine()
			.AppendLine($"public partial class {className}")
			.AppendLine("{")
			.AppendLine("\treadonly global::Microsoft.Extensions.Localization.IStringLocalizer localizer;")
			.AppendLine()
			.AppendLine($"\tpublic {className}(global::Microsoft.Extensions.Localization.IStringLocalizer<global::{nameSpace}.{associatedResourceClassName}> localizer)")
			.AppendLine("\t{")
			.AppendLine("\t\tthis.localizer = localizer;")
			.AppendLine("\t}")
			.AppendLine();

		using (var stream = new StringReader(resxContent))
		{
			var reader = new ResXResourceReader(stream).GetEnumerator();
			while (reader.MoveNext())
			{
				var resourceKey = (string)reader.Key;
				var propertyName = SafePropertyKey(resourceKey);
                sb.AppendLine($"\tpublic string {propertyName} => this.localizer[\"{resourceKey}\"];");
            }
		}

        sb.AppendLine("}");

		return sb.ToString();
	}


	static string SafePropertyKey(string keyName)
	{
		// TODO: spaces to _, other?
		return keyName;
	}


    static string GenerateServiceCollectionRegistration(string rootNamespace, IEnumerable<string> generatedTypes)
    {
        var sb = new StringBuilder()
            .AppendLine("using global::Microsoft.Extensions.Localization;")
            .AppendLine("using global::Microsoft.Extensions.DependencyInjection;")
            .AppendLine()
            .AppendLine($"namespace {rootNamespace};")
            .AppendLine()
            .Append("public static class ServiceCollectionExtensions_Generated")
            .AppendLine("{")
            .AppendLine("\tpublic static void AddStronglyTypedLocalizations(this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)")
            .AppendLine("\t{");

		// TODO: should I force install localization, add args to allow it to be installed, or just ignore?
        foreach (var genType in generatedTypes)
        {
            sb.AppendLine($"\t\tservices.AddSingleton<global::{genType}>();");
        }
        sb
            .Append("\t}")
            .Append("}");

        return sb.ToString();
    }
}