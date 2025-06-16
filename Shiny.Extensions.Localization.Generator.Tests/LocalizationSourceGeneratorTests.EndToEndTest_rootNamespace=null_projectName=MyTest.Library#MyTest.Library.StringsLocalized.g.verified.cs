//HintName: MyTest.Library.StringsLocalized.g.cs
namespace MyTest.Library;

public partial class StringsLocalized
{
	readonly global::Microsoft.Extensions.Localization.IStringLocalizer localizer;

	public StringsLocalized(global::Microsoft.Extensions.Localization.IStringLocalizer<global::MyTest.Library.Strings> localizer)
	{
		this.localizer = localizer;
	}

	public string LocalizeKey => this.localizer["LocalizeKey"];
}
