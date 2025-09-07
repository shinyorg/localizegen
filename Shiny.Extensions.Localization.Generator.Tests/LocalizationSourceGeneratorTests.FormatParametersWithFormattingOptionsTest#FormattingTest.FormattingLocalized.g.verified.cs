//HintName: FormattingTest.FormattingLocalized.g.cs
namespace FormattingTest;

public partial class FormattingLocalized
{
	readonly global::Microsoft.Extensions.Localization.IStringLocalizer localizer;

	public FormattingLocalized(global::Microsoft.Extensions.Localization.IStringLocalizer<global::FormattingTest.Formatting> localizer)
	{
		this.localizer = localizer;
	}

	public global::Microsoft.Extensions.Localization.IStringLocalizer Localizer => this.localizer;

	/// <summary>
	/// Today is {0:MMM dd yyyy}
	/// </summary>
	public string DateFormatMessage => this.localizer["DateFormatMessage"];
	/// <summary>
	/// Total cost: {0:C2}
	/// </summary>
	public string CurrencyFormatMessage => this.localizer["CurrencyFormatMessage"];
	/// <summary>
	/// Success rate: {0:P2}
	/// </summary>
	public string PercentageFormatMessage => this.localizer["PercentageFormatMessage"];
	/// <summary>
	/// Value: {0:N2}
	/// </summary>
	public string NumberFormatMessage => this.localizer["NumberFormatMessage"];
	/// <summary>
	/// Event scheduled for {0:yyyy-MM-dd HH:mm:ss}
	/// </summary>
	public string CustomDateTimeFormat => this.localizer["CustomDateTimeFormat"];
	/// <summary>
	/// Date: {0:MMM dd yyyy}, Amount: {1:C2}, Rate: {2:P1}
	/// </summary>
	public string MixedFormattingMessage => this.localizer["MixedFormattingMessage"];
	/// <summary>
	/// From {0:yyyy-MM-dd} to {1:yyyy-MM-dd}
	/// </summary>
	public string DateRangeMessage => this.localizer["DateRangeMessage"];
	/// <summary>
	/// Report generated on {0:dddd, MMMM dd, yyyy} at {1:HH:mm:ss} for user {2}
	/// </summary>
	public string DetailedReportMessageFormat(object parameter2)
	{
		return string.Format(this.localizer["DetailedReportMessage"], parameter2);
	}

	/// <summary>
	/// Start: {0:HH:mm}, End: {0:HH:mm:ss}
	/// </summary>
	public string RepeatedFormattedParameter => this.localizer["RepeatedFormattedParameter"];
	/// <summary>
	/// User {0} logged in at {1:yyyy-MM-dd HH:mm} from location {2}
	/// </summary>
	public string MixedParameterTypesFormat(object parameter0, object parameter2)
	{
		return string.Format(this.localizer["MixedParameterTypes"], parameter0, parameter2);
	}

}
