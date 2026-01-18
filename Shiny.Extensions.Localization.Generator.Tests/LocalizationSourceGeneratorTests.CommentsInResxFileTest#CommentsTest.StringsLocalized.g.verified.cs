//HintName: CommentsTest.StringsLocalized.g.cs
namespace CommentsTest;

public partial class StringsLocalized
{
	readonly global::Microsoft.Extensions.Localization.IStringLocalizer localizer;

	public StringsLocalized(global::Microsoft.Extensions.Localization.IStringLocalizer<global::CommentsTest.Strings> localizer)
	{
		this.localizer = localizer;
	}

	public global::Microsoft.Extensions.Localization.IStringLocalizer Localizer => this.localizer;

	/// <summary>
	/// Displayed on the home screen when user first opens the app
	/// </summary>
	public string WelcomeMessage => this.localizer["WelcomeMessage"];
	/// <summary>
	/// Goodbye and see you later
	/// </summary>
	public string GoodbyeMessage => this.localizer["GoodbyeMessage"];
	/// <summary>
	/// Greeting message where {0} is the user&apos;s name
	/// </summary>
	public string GreetingFormat(object parameter0)
	{
		return string.Format(this.localizer["GreetingFormat"], parameter0);
	}

	/// <summary>
	/// Goodbye {0}, see you tomorrow!
	/// </summary>
	public string FarewellFormat(object parameter0)
	{
		return string.Format(this.localizer["FarewellFormat"], parameter0);
	}

	/// <summary>
	/// Full greeting with user name, message count, and sender name
	/// </summary>
	public string DetailedGreetingFormat(object parameter0, object parameter1, object parameter2)
	{
		return string.Format(this.localizer["DetailedGreeting"], parameter0, parameter1, parameter2);
	}

	/// <summary>
	/// Comment with &lt;special&gt; &amp; &quot;characters&quot; to &apos;escape&apos;
	/// </summary>
	public string SpecialComment => this.localizer["SpecialComment"];
	/// <summary>
	/// Shows the current date in long format
	/// </summary>
	public string DateGreetingFormat(object parameter0)
	{
		return string.Format(this.localizer["DateGreeting"], parameter0);
	}

}
