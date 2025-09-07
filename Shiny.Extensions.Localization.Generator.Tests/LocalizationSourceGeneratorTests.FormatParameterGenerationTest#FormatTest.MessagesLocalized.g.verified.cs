//HintName: FormatTest.MessagesLocalized.g.cs
namespace FormatTest;

public partial class MessagesLocalized
{
	readonly global::Microsoft.Extensions.Localization.IStringLocalizer localizer;

	public MessagesLocalized(global::Microsoft.Extensions.Localization.IStringLocalizer<global::FormatTest.Messages> localizer)
	{
		this.localizer = localizer;
	}

	public global::Microsoft.Extensions.Localization.IStringLocalizer Localizer => this.localizer;

	public string SimpleMessage => this.localizer["SimpleMessage"];
	public string AnotherSimple => this.localizer["AnotherSimple"];
	public string MessageWithOneParameterFormat(object parameter0)
	{
		return string.Format(this.localizer["MessageWithOneParameter"], parameter0);
	}

	public string MessageWithTwoParametersFormat(object parameter0, object parameter1)
	{
		return string.Format(this.localizer["MessageWithTwoParameters"], parameter0, parameter1);
	}

	public string MessageWithThreeParametersFormat(object parameter0, object parameter1, object parameter2)
	{
		return string.Format(this.localizer["MessageWithThreeParameters"], parameter0, parameter1, parameter2);
	}

	public string MessageWithNonSequentialParametersFormat(object parameter0, object parameter1, object parameter2)
	{
		return string.Format(this.localizer["MessageWithNonSequentialParameters"], parameter0, parameter1, parameter2);
	}

	public string MessageWithRepeatedParametersFormat(object parameter0)
	{
		return string.Format(this.localizer["MessageWithRepeatedParameters"], parameter0);
	}

	public string MessageWithMixedContentFormat(object parameter0)
	{
		return string.Format(this.localizer["MessageWithMixedContent"], parameter0);
	}

}
