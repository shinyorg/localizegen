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
#pragma warning disable RS2008 // no analyzer release tracking in this project
	static readonly DiagnosticDescriptor OrphanedCultureFile = new(
		"SHINY0001",
		"Culture resource file has no default resource file",
		"'{0}' is a '{1}' culture file, but '{2}' does not exist, so no localizer was generated for it. Add '{2}' with the default strings.",
		"Shiny.Localization",
		DiagnosticSeverity.Warning,
		true
	);

	static readonly DiagnosticDescriptor NameConflict = new(
		"SHINY0002",
		"Resource file name conflicts with an existing type or namespace",
		"'{0}' would generate {1} '{2}', which conflicts with {3} '{2}', so no localizer was generated for it. Rename or remove the file.",
		"Shiny.Localization",
		DiagnosticSeverity.Error,
		true
	);

	static readonly DiagnosticDescriptor DottedDefaultFile = new(
		"SHINY0003",
		"Dotted resource file is not a culture file",
		"'{0}': '{1}' is not a culture, so it is a default resource file and generates localizer '{2}'",
		"Shiny.Localization",
		DiagnosticSeverity.Info,
		true
	);
#pragma warning restore RS2008

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var globalOptions = context
			.AnalyzerConfigOptionsProvider
			.Select(GlobalOptions.Select);

		var resxFiles = context
			.AdditionalTextsProvider
			// culture files (Strings.de.resx) are sorted out in Generate once all files are known
			.Where(static x => x.Path.EndsWith(".resx", StringComparison.OrdinalIgnoreCase));

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
		var classification = ResxClassifier.Classify(ctx.Files);
		var defaultFiles = classification.DefaultFiles;
		foreach (var orphan in classification.OrphanedCultureFiles)
		{
			context.ReportDiagnostic(Diagnostic.Create(
				OrphanedCultureFile,
				Location.Create(orphan.File.FilePath, default, default),
				Path.GetFileName(orphan.File.FilePath),
				orphan.Culture,
				orphan.DefaultFileName
			));
		}

		if (!defaultFiles.Any())
			context.LogDiagnostic("SHINY0000", "No Files found", "No .resx files were found to process.", DiagnosticSeverity.Warning);
		
		var generatedClasses = new List<string>();
		var registry = new List<(string Name, string ResourceSource, string LocalizedType)>();
		var first = defaultFiles.FirstOrDefault();
		var rootNamespace = first?.GlobalOptions.RootNamespace ?? first?.GlobalOptions.ProjectName!;
		var internalAccessor = first?.GlobalOptions.UseInternalAccessor ?? false;

		// every type the generator could emit - a dotted file's namespace must not land on one of these
		var plannedTypes = new HashSet<string>(StringComparer.Ordinal);
		foreach (var file in defaultFiles)
		{
			plannedTypes.Add($"{file.FileNamespace}.{file.AssociatedClassName}");
			plannedTypes.Add($"{file.FileNamespace}.{file.LocalizedClassName}");
		}
		var userNamespaces = new Lazy<HashSet<string>>(() => GetNamespaces(ctx.Compilation.Assembly.GlobalNamespace));
		
		foreach (var file in defaultFiles)
		{
			if (file.NameWithoutExtension.IndexOf('.') >= 0)
			{
				context.ReportDiagnostic(Diagnostic.Create(
					DottedDefaultFile,
					Location.Create(file.FilePath, default, default),
					Path.GetFileName(file.FilePath),
					file.NameWithoutExtension.Substring(file.NameWithoutExtension.LastIndexOf('.') + 1),
					$"{file.FileNamespace}.{file.LocalizedClassName}"
				));
			}

			// emitting code that collides fails with CS0101 somewhere unrelated (often a .Designer.cs) - say why instead
			var conflict = FindNameConflict(file, ctx.Compilation, plannedTypes, userNamespaces);
			if (conflict != null)
			{
				context.ReportDiagnostic(Diagnostic.Create(
					NameConflict,
					Location.Create(file.FilePath, default, default),
					Path.GetFileName(file.FilePath),
					conflict.Value.Generates,
					conflict.Value.Name,
					conflict.Value.ConflictsWith
				));
				continue;
			}

			// match the visibility of the user's associated class when it exists in this assembly,
			// otherwise fall back to GenerateLocalizersInternal (and stub the class below)
			var associatedMetadataName = $"{file.FileNamespace}.{file.AssociatedClassName}";
			var associated = ctx.Compilation.Assembly.GetTypeByMetadataName(associatedMetadataName);
			var fileInternal = associated == null
				? internalAccessor
				: associated.DeclaredAccessibility != Accessibility.Public;

			var generated = GenerateStronglyTypedClass(
				file.FileContent,
				file.FileNamespace,
				file.LocalizedClassName,
				file.AssociatedClassName,
				fileInternal
			);
			generatedClasses.Add($"{file.FileNamespace}.{file.LocalizedClassName}");
			registry.Add((
				file.NameWithoutExtension, // Errors.Validation.resx -> "Errors.Validation" so names stay unique
				$"{file.FileNamespace}.{file.AssociatedClassName}",
				$"{file.FileNamespace}.{file.LocalizedClassName}"
			));
			context.AddSource($"{file.FileNamespace}.{file.LocalizedClassName}.g.cs", generated);

			// IStringLocalizer<T> needs T to exist - if the user hasn't declared it in this assembly, stub it
			if (associated == null)
			{
				var stub = GenerateAssociatedClassStub(
					file.FileNamespace,
					file.AssociatedClassName,
					internalAccessor
				);
				context.AddSource($"{associatedMetadataName}.g.cs", stub);
			}
		}

		var generatedService = GenerateServiceCollectionRegistration(
			rootNamespace, 
			generatedClasses, 
			internalAccessor
		);
		context.AddSource("ServiceCollectionExtensions.g.cs", generatedService);

		var generatedRegistry = GenerateLocalizerRegistry(
			rootNamespace,
			registry,
			classification.Cultures,
			internalAccessor
		);
		context.AddSource("GeneratedLocalizers.g.cs", generatedRegistry);
	}


	static (string Generates, string Name, string ConflictsWith)? FindNameConflict(
		FileOptions file,
		Compilation compilation,
		HashSet<string> plannedTypes,
		Lazy<HashSet<string>> userNamespaces
	)
	{
		// the file's namespace (or any parent of it) is already a type - Strings.orig.resx -> namespace PoolMath.Strings vs class PoolMath.Strings
		var segments = file.FileNamespace.Split('.');
		for (var i = 1; i <= segments.Length; i++)
		{
			var ns = String.Join(".", segments, 0, i);
			if (String.IsNullOrEmpty(ns))
				continue;

			if (compilation.Assembly.GetTypeByMetadataName(ns) != null)
				return ("namespace", ns, "type");

			if (plannedTypes.Contains(ns))
				return ("namespace", ns, "the generated type");
		}

		// the type the file generates is already a namespace in user code
		foreach (var type in new[] { file.AssociatedClassName, file.LocalizedClassName })
		{
			var full = String.IsNullOrEmpty(file.FileNamespace) ? type : $"{file.FileNamespace}.{type}";
			if (userNamespaces.Value.Contains(full))
				return ("type", full, "namespace");
		}

		return null;
	}


	static HashSet<string> GetNamespaces(INamespaceSymbol root)
	{
		var set = new HashSet<string>(StringComparer.Ordinal);
		var stack = new Stack<INamespaceSymbol>();
		stack.Push(root);
		while (stack.Count > 0)
		{
			foreach (var ns in stack.Pop().GetNamespaceMembers())
			{
				set.Add(ns.ToDisplayString());
				stack.Push(ns);
			}
		}
		return set;
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
				var resourceComment = dataElement.Element("comment")?.Value ?? string.Empty;
				var propertyName = SafePropertyKey(resourceKey);
				
				var formatParameters = GetFormatParameters(resourceValue);
				
				// Use comment if available, otherwise use value as fallback
				var summaryText = !string.IsNullOrEmpty(resourceComment) 
					? resourceComment 
					: resourceValue;
				
				if (formatParameters.Any())
				{
					// Generate method for format strings
					// Avoid double "Format" suffix if property name already ends with Format
					var methodName = propertyName.EndsWith("Format", StringComparison.Ordinal) 
						? propertyName 
						: $"{propertyName}Format";
					var parameters = string.Join(", ", formatParameters.Select(i => $"object parameter{i}"));
					var arguments = string.Join(", ", formatParameters.Select(i => $"parameter{i}"));
					
					sb.AppendLine("\t/// <summary>");
					sb.AppendLine($"\t/// {EscapeXmlComment(summaryText)}");
					sb.AppendLine("\t/// </summary>");
					sb.AppendLine($"\tpublic string {methodName}({parameters})");
					sb.AppendLine("\t{");
					sb.AppendLine($"\t\treturn string.Format(this.localizer[\"{resourceKey}\"], {arguments});");
					sb.AppendLine("\t}");
					sb.AppendLine();
				}
				else
				{
					// Generate property for simple strings
					sb.AppendLine("\t/// <summary>");
					sb.AppendLine($"\t/// {EscapeXmlComment(summaryText)}");
					sb.AppendLine("\t/// </summary>");
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
		var regex = new Regex(@"\{(\d+)(?::[^}]*)?\}");
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


	static string EscapeXmlComment(string value)
	{
		if (string.IsNullOrEmpty(value))
			return string.Empty;

		return value
			.Replace("&", "&amp;")
			.Replace("<", "&lt;")
			.Replace(">", "&gt;")
			.Replace("\"", "&quot;")
			.Replace("'", "&apos;")
			.Replace("\r\n", " ")
			.Replace("\r", " ")
			.Replace("\n", " ");
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


	static string GenerateLocalizerRegistry(
		string rootNamespace,
		IEnumerable<(string Name, string ResourceSource, string LocalizedType)> entries,
		IEnumerable<string> cultures,
		bool useInternalAccessor
	)
	{
		var accessor = useInternalAccessor ? "internal" : "public";
		var sb = new StringBuilder()
			.AppendLine("// <auto-generated />")
			.AppendLine("// This file is auto-generated by Shiny.Extensions.Localization.Generator")
			.AppendLine("// Do not edit this file directly, instead edit the .resx files in your project.");

		if (!String.IsNullOrWhiteSpace(rootNamespace))
		{
			sb
				.AppendLine()
				.AppendLine($"namespace {rootNamespace};");
		}

		sb
			.AppendLine()
			.AppendLine("/// <summary>")
			.AppendLine("/// Reflection-free list of every strongly typed localizer generated for this assembly.")
			.AppendLine("/// Safe to enumerate under trimming and NativeAOT.")
			.AppendLine("/// </summary>")
			.AppendLine($"{accessor} static class GeneratedLocalizers")
			.AppendLine("{")
			.AppendLine("\t/// <summary>")
			.AppendLine("\t/// Name is the resource class name, ResourceSource is the type used with IStringLocalizer&lt;T&gt;,")
			.AppendLine("\t/// LocalizedType is the generated wrapper registered by AddStronglyTypedLocalizations.")
			.AppendLine("\t/// </summary>")
			.AppendLine("\tpublic static readonly (string Name, global::System.Type ResourceSource, global::System.Type LocalizedType)[] All = new (string Name, global::System.Type ResourceSource, global::System.Type LocalizedType)[]")
			.AppendLine("\t{");

		foreach (var entry in entries)
			sb.AppendLine($"\t\t(\"{entry.Name}\", typeof(global::{entry.ResourceSource}), typeof(global::{entry.LocalizedType})),");

		sb
			.AppendLine("\t};")
			.AppendLine()
			.AppendLine("\t/// <summary>")
			.AppendLine("\t/// Cultures with at least one translated .resx in this assembly (Strings.fr-CA.resx -> \"fr-CA\"), sorted.")
			.AppendLine("\t/// The neutral/default resources are not included. Use for language pickers without probing satellite assemblies.")
			.AppendLine("\t/// </summary>")
			.AppendLine("\tpublic static readonly string[] Cultures = new string[]")
			.AppendLine("\t{");

		foreach (var culture in cultures)
			sb.AppendLine($"\t\t\"{culture.Replace("\\", "\\\\").Replace("\"", "\\\"")}\",");

		sb
			.AppendLine("\t};")
			.Append("}");

		return sb.ToString();
	}


	static string GenerateAssociatedClassStub(string nameSpace, string className, bool useInternalAccessor)
	{
		var accessor = useInternalAccessor ? "internal" : "public";
		return new StringBuilder()
			.AppendLine("// <auto-generated />")
			.AppendLine("// This file is auto-generated by Shiny.Extensions.Localization.Generator because no type")
			.AppendLine($"// named {nameSpace}.{className} exists. Declare your own partial class to extend it.")
			.AppendLine($"namespace {nameSpace};")
			.AppendLine()
			.AppendLine($"{accessor} partial class {className}")
			.AppendLine("{")
			.Append("}")
			.ToString();
	}
}
