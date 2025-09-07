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

	/// <summary>
	/// This has {braces} but not format parameters
	/// </summary>
	public string BracesButNotFormat => this.localizer["BracesButNotFormat"];
	/// <summary>
	/// This has {} empty braces
	/// </summary>
	public string EmptyBraces => this.localizer["EmptyBraces"];
	/// <summary>
	/// This has {abc} invalid format
	/// </summary>
	public string InvalidFormat => this.localizer["InvalidFormat"];
	/// <summary>
	/// Valid {0} and invalid {abc} parameters
	/// </summary>
	public string MixedValidInvalidFormat(object parameter0)
	{
		return string.Format(this.localizer["MixedValidInvalid"], parameter0);
	}

	/// <summary>
	/// Only {0} parameter
	/// </summary>
	public string SingleParameterFormat(object parameter0)
	{
		return string.Format(this.localizer["SingleParameter"], parameter0);
	}

	/// <summary>
	/// Parameter with high number {10}
	/// </summary>
	public string HighNumberParameterFormat(object parameter10)
	{
		return string.Format(this.localizer["HighNumberParameter"], parameter10);
	}

	/// <summary>
	/// Starting with {0}
	/// </summary>
	public string ZeroParameterFormat(object parameter0)
	{
		return string.Format(this.localizer["ZeroParameter"], parameter0);
	}

}
