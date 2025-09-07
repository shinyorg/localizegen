//HintName: EdgeCaseTest.EdgeCasesLocalized.g.cs
namespace EdgeCaseTest;

public partial class EdgeCasesLocalized
{
	readonly global::Microsoft.Extensions.Localization.IStringLocalizer localizer;

	public EdgeCasesLocalized(global::Microsoft.Extensions.Localization.IStringLocalizer<global::EdgeCaseTest.EdgeCases> localizer)
	{
		this.localizer = localizer;
	}

	public global::Microsoft.Extensions.Localization.IStringLocalizer Localizer => this.localizer;

	public string BracesButNotFormat => this.localizer["BracesButNotFormat"];
	public string EmptyBraces => this.localizer["EmptyBraces"];
	public string InvalidFormat => this.localizer["InvalidFormat"];
	public string MixedValidInvalidFormat(object parameter0)
	{
		return string.Format(this.localizer["MixedValidInvalid"], parameter0);
	}

	public string SingleParameterFormat(object parameter0)
	{
		return string.Format(this.localizer["SingleParameter"], parameter0);
	}

	public string HighNumberParameterFormat(object parameter10)
	{
		return string.Format(this.localizer["HighNumberParameter"], parameter10);
	}

	public string ZeroParameterFormat(object parameter0)
	{
		return string.Format(this.localizer["ZeroParameter"], parameter0);
	}

}
