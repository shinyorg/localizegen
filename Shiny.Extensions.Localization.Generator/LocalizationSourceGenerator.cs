﻿using System.Text;
using System.Resources.NetStandard;
using Microsoft.CodeAnalysis;

namespace Shiny.Extensions.Localization.Generator;


[Generator]
public class LocalizationSourceGenerator : IIncrementalGenerator
{
	List<string> generatedClasses;


	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		this.generatedClasses = new();

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

        context.RegisterSourceOutput(
			inputs,
			(productionContext, sourceContext) => Generate(productionContext, sourceContext)
		);

		// this happens BEFORE register source output, so no good
		//context.RegisterPostInitializationOutput(x =>
		//{
		//	GenerateServiceCollectionRegistration("TODO", this.generatedClasses);
		//});
    }


    void Generate(SourceProductionContext context, FileOptions file)
	{
		var generated = GenerateStronglyTypedClass(
			file.FileContent,
			file.FileNamespace,
			file.LocalizedClassName,
			file.AssociatedClassName
		);
		this.generatedClasses.Add($"{file.FileNamespace}.{file.LocalizedClassName}");
        context.AddSource($"{file.FileNamespace}.{file.LocalizedClassName}.g.cs", generated);
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
			.AppendLine("\treadonly Microsoft.Extensions.Localization.IStringLocalizer localizer;")
			.AppendLine()
			.AppendLine($"\tpublic {className}(IStringLocalizer<{associatedResourceClassName}> localizer)")
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
            .AppendLine("using Microsoft.Extensions.Localization;")
            .AppendLine()
            .AppendLine($"namespace {rootNamespace};")
            .AppendLine()
            .Append("public static class Generated")
            .AppendLine("{")
            .AppendLine("\tpublic static void AddStrongTypedLocalizations(this Microsoft.Extensions.DependencyInjection.IServiceCollection services)")
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