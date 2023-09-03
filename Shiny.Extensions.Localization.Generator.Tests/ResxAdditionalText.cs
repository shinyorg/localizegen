using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Shiny.Extensions.Localization.Generator.Tests;


public class ResxAdditionalText : AdditionalText
{
	readonly Dictionary<string, string> strings = new();
	public ResxAdditionalText(string path) => this.Path = path;

    public ResxAdditionalText AddString(string key, string value)
	{
		this.strings.Add(key, value);
		return this;
	}

    public override string Path { get; }
    public override SourceText GetText(CancellationToken cancellationToken = default)
	{
		var sb = new StringBuilder();
		sb.Append(
"""
<?xml version="1.0" encoding="utf-8"?>
<root>
	<resheader name="resmimetype">
		<value>text/microsoft-resx</value>
	</resheader>
	<resheader name="version">
		<value>2.0</value>
	</resheader>
	<resheader name="reader">
		<value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
	</resheader>
	<resheader name="writer">
		<value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
	</resheader>
""");
		foreach (var pair in this.strings)
		{
			sb.AppendLine($"<data name=\"{pair.Key}\" xml:space=\"preserve\">");
			sb.AppendLine($"<value>{pair.Value}</value>");
			sb.AppendLine("</data>");
		}
		sb.Append("</root>");

		return SourceText.From(sb.ToString(), Encoding.UTF8);
	}
}
/*
 <?xml version="1.0" encoding="utf-8"?>
<root>
	<resheader name="resmimetype">
		<value>text/microsoft-resx</value>
	</resheader>
	<resheader name="version">
		<value>2.0</value>
	</resheader>
	<resheader name="reader">
		<value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
	</resheader>
	<resheader name="writer">
		<value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
	</resheader>
	<data name="LocalizeKey" xml:space="preserve">
		<value>This is test localization</value>
	</data>
</root>* 
 */