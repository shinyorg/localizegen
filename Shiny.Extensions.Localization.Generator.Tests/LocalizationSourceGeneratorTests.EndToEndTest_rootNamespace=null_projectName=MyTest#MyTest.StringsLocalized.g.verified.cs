//HintName: MyTest.StringsLocalized.g.cs
namespace MyTest;

public partial class StringsLocalized
{
	readonly global::Microsoft.Extensions.Localization.IStringLocalizer localizer;

	public StringsLocalized(global::Microsoft.Extensions.Localization.IStringLocalizer<global::MyTest.Strings> localizer)
	{
		this.localizer = localizer;
	}

	public string LocalizeKey => this.localizer["LocalizeKey"];
}
