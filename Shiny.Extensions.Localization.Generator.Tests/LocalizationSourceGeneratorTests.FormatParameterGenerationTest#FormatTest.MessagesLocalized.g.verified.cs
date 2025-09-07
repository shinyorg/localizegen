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

	/// <summary>
	/// Hello World
	/// </summary>
	public string SimpleMessage => this.localizer["SimpleMessage"];
	/// <summary>
	/// Welcome to our application
	/// </summary>
	public string AnotherSimple => this.localizer["AnotherSimple"];
	/// <summary>
	/// Hello {0}
	/// </summary>
	public string MessageWithOneParameterFormat(object parameter0)
	{
		return string.Format(this.localizer["MessageWithOneParameter"], parameter0);
	}

	/// <summary>
	/// Hello {0}, you have {1} messages
	/// </summary>
	public string MessageWithTwoParametersFormat(object parameter0, object parameter1)
	{
		return string.Format(this.localizer["MessageWithTwoParameters"], parameter0, parameter1);
	}

	/// <summary>
	/// User {0} logged in at {1} from {2}
	/// </summary>
	public string MessageWithThreeParametersFormat(object parameter0, object parameter1, object parameter2)
	{
		return string.Format(this.localizer["MessageWithThreeParameters"], parameter0, parameter1, parameter2);
	}

	/// <summary>
	/// Value {2} comes before {0} and {1}
	/// </summary>
	public string MessageWithNonSequentialParametersFormat(object parameter0, object parameter1, object parameter2)
	{
		return string.Format(this.localizer["MessageWithNonSequentialParameters"], parameter0, parameter1, parameter2);
	}

	/// <summary>
	/// Hello {0}, {0} is a nice name!
	/// </summary>
	public string MessageWithRepeatedParametersFormat(object parameter0)
	{
		return string.Format(this.localizer["MessageWithRepeatedParameters"], parameter0);
	}

	/// <summary>
	/// Welcome {0}! Today is a great day.
	/// </summary>
	public string MessageWithMixedContentFormat(object parameter0)
	{
		return string.Format(this.localizer["MessageWithMixedContent"], parameter0);
	}

}
