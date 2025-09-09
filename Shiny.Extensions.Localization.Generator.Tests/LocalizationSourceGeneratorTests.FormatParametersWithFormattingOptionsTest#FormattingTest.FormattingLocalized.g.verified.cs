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
	public string DateFormatMessageFormat(object parameter0)
	{
		return string.Format(this.localizer["DateFormatMessage"], parameter0);
	}

	/// <summary>
	/// Total cost: {0:C2}
	/// </summary>
	public string CurrencyFormatMessageFormat(object parameter0)
	{
		return string.Format(this.localizer["CurrencyFormatMessage"], parameter0);
	}

	/// <summary>
	/// Success rate: {0:P2}
	/// </summary>
	public string PercentageFormatMessageFormat(object parameter0)
	{
		return string.Format(this.localizer["PercentageFormatMessage"], parameter0);
	}

	/// <summary>
	/// Value: {0:N2}
	/// </summary>
	public string NumberFormatMessageFormat(object parameter0)
	{
		return string.Format(this.localizer["NumberFormatMessage"], parameter0);
	}

	/// <summary>
	/// Event scheduled for {0:yyyy-MM-dd HH:mm:ss}
	/// </summary>
	public string CustomDateTimeFormatFormat(object parameter0)
	{
		return string.Format(this.localizer["CustomDateTimeFormat"], parameter0);
	}

	/// <summary>
	/// Date: {0:MMM dd yyyy}, Amount: {1:C2}, Rate: {2:P1}
	/// </summary>
	public string MixedFormattingMessageFormat(object parameter0, object parameter1, object parameter2)
	{
		return string.Format(this.localizer["MixedFormattingMessage"], parameter0, parameter1, parameter2);
	}

	/// <summary>
	/// From {0:yyyy-MM-dd} to {1:yyyy-MM-dd}
	/// </summary>
	public string DateRangeMessageFormat(object parameter0, object parameter1)
	{
		return string.Format(this.localizer["DateRangeMessage"], parameter0, parameter1);
	}

	/// <summary>
	/// Report generated on {0:dddd, MMMM dd, yyyy} at {1:HH:mm:ss} for user {2}
	/// </summary>
	public string DetailedReportMessageFormat(object parameter0, object parameter1, object parameter2)
	{
		return string.Format(this.localizer["DetailedReportMessage"], parameter0, parameter1, parameter2);
	}

	/// <summary>
	/// Start: {0:HH:mm}, End: {0:HH:mm:ss}
	/// </summary>
	public string RepeatedFormattedParameterFormat(object parameter0)
	{
		return string.Format(this.localizer["RepeatedFormattedParameter"], parameter0);
	}

	/// <summary>
	/// User {0} logged in at {1:yyyy-MM-dd HH:mm} from location {2}
	/// </summary>
	public string MixedParameterTypesFormat(object parameter0, object parameter1, object parameter2)
	{
		return string.Format(this.localizer["MixedParameterTypes"], parameter0, parameter1, parameter2);
	}

}
