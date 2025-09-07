using System.Text;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Text.RegularExpressions;

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

				var filename = Path.GetFileName(x.Path);

				var count = filename.ToCharArray().Count(y => y.Equals('.'));
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
			context.LogDiagnostic("SHINY0000", "No Files found", "No .resx files were found to process.", DiagnosticSeverity.Warning);
		
		var generatedClasses = new List<string>();
		var first = ctx.Files.FirstOrDefault();
		var rootNamespace = first?.GlobalOptions.RootNamespace ?? first?.GlobalOptions.ProjectName!;
		var internalAccessor = first?.GlobalOptions.UseInternalAccessor ?? false;
		
		foreach (var file in ctx.Files)
		{
			var generated = GenerateStronglyTypedClass(
				file.FileContent,
				file.FileNamespace,
				file.LocalizedClassName,
				file.AssociatedClassName,
				internalAccessor
			);
			generatedClasses.Add($"{file.FileNamespace}.{file.LocalizedClassName}");
			context.AddSource($"{file.FileNamespace}.{file.LocalizedClassName}.g.cs", generated);
		}

		var generatedService = GenerateServiceCollectionRegistration(
			rootNamespace, 
			generatedClasses, 
			internalAccessor
		);
		context.AddSource("ServiceCollectionExtensions.g.cs", generatedService);
	}


	static string GenerateStronglyTypedClass(
		string resxContent,
		string nameSpace,
		string className,
		string associatedResourceClassName,
		bool useInternalAccessor
	)
	{
		var accessor = useInternalAccessor ? "internal" : "public";
		
		var sb = new StringBuilder()
			.AppendLine($"namespace {nameSpace};")
			.AppendLine()
			.AppendLine($"{accessor} partial class {className}")
			.AppendLine("{")
			.AppendLine("\treadonly global::Microsoft.Extensions.Localization.IStringLocalizer localizer;")
			.AppendLine()
			.AppendLine($"\tpublic {className}(global::Microsoft.Extensions.Localization.IStringLocalizer<global::{nameSpace}.{associatedResourceClassName}> localizer)")
			.AppendLine("\t{")
			.AppendLine("\t\tthis.localizer = localizer;")
			.AppendLine("\t}")
			.AppendLine()
			.AppendLine("\tpublic global::Microsoft.Extensions.Localization.IStringLocalizer Localizer => this.localizer;")
			.AppendLine();

		using (var stream = new StringReader(resxContent))
		{
			var xml = XDocument.Load(stream);

			foreach (var dataElement in xml.XPathSelectElements("//root/data"))
			{
				var resourceKey = dataElement.Attribute("name")?.Value ?? string.Empty;
				var resourceValue = dataElement.Element("value")?.Value ?? string.Empty;
				var propertyName = SafePropertyKey(resourceKey);
				
				var formatParameters = GetFormatParameters(resourceValue);
				
				if (formatParameters.Any())
				{
					// Generate method for format strings
					var methodName = $"{propertyName}Format";
					var parameters = string.Join(", ", formatParameters.Select(i => $"object parameter{i}"));
					var arguments = string.Join(", ", formatParameters.Select(i => $"parameter{i}"));
					
					sb.AppendLine($"\tpublic string {methodName}({parameters})");
					sb.AppendLine("\t{");
					sb.AppendLine($"\t\treturn string.Format(this.localizer[\"{resourceKey}\"], {arguments});");
					sb.AppendLine("\t}");
					sb.AppendLine();
				}
				else
				{
					// Generate property for simple strings
					sb.AppendLine($"\tpublic string {propertyName} => this.localizer[\"{resourceKey}\"];");
				}
			}
		}

		sb.AppendLine("}");

		return sb.ToString();
	}


	static string SafePropertyKey(string keyName) => keyName
		.Replace(".", "_")
		.Replace("-", "_")
		.Replace(" ", "_");


	static List<int> GetFormatParameters(string value)
	{
		var parameters = new List<int>();
		var regex = new Regex(@"\{(\d+)\}");
		var matches = regex.Matches(value);
		
		foreach (Match match in matches)
		{
			if (int.TryParse(match.Groups[1].Value, out int paramIndex))
			{
				if (!parameters.Contains(paramIndex))
				{
					parameters.Add(paramIndex);
				}
			}
		}
		
		parameters.Sort();
		return parameters;
	}


	static string GenerateServiceCollectionRegistration(string rootNamespace, IEnumerable<string> generatedTypes, bool useInternalAccessor)
	{
		var accessor = useInternalAccessor ? "internal" : "public";
		var sb = new StringBuilder()
			.AppendLine("// <auto-generated />")
			.AppendLine("// This file is auto-generated by Shiny.Extensions.Localization.Generator")
			.AppendLine("// Do not edit this file directly, instead edit the .resx files in your project.")
			.AppendLine("using global::Microsoft.Extensions.DependencyInjection;");

		if (!String.IsNullOrWhiteSpace(rootNamespace))
		{
			// use global namespace if not set
			sb
				.AppendLine()
				.AppendLine($"namespace {rootNamespace};");
		}

		sb	
			.AppendLine()
			.AppendLine($"{accessor} static class ServiceCollectionExtensions_Generated")
			.AppendLine("{")
			.AppendLine("\tpublic static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddStronglyTypedLocalizations(this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)")
			.AppendLine("\t{");

		sb.AppendLine("\t\tservices.AddLocalization();");
		foreach (var genType in generatedTypes)
		{
			sb.AppendLine($"\t\tservices.AddSingleton<global::{genType}>();");
		}
		sb.AppendLine("\t\treturn services;");
		sb
			.AppendLine("\t}")
			.Append("}");

		return sb.ToString();
	}
}
