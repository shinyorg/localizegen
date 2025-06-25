//HintName: MyTest.StringsLocalized.g.cs
namespace MyTest;

public partial class StringsLocalized
{
	readonly global::Microsoft.Extensions.Localization.IStringLocalizer localizer;

	public StringsLocalized(global::Microsoft.Extensions.Localization.IStringLocalizer<global::MyTest.Strings> localizer)
	{
		this.localizer = localizer;
	}

	public global::Microsoft.Extensions.Localization.IStringLocalizer Localizer => this.localizer;

	public string LocalizeKey => this.localizer["LocalizeKey"];
	public string Localized_Space => this.localizer["Localized Space"];
	public string Localized__Space_Multiple => this.localizer["Localized  Space Multiple"];
	public string MyNamespace_MyEnum_MyEnumValue => this.localizer["MyNamespace.MyEnum.MyEnumValue"];
}
