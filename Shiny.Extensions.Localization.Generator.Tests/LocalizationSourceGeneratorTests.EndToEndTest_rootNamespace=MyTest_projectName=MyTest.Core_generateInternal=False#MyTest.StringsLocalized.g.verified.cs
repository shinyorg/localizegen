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

	/// <summary>
	/// This is a test
	/// </summary>
	public string LocalizeKey => this.localizer["LocalizeKey"];
	/// <summary>
	/// This is a test with spaces
	/// </summary>
	public string Localized_Space => this.localizer["Localized Space"];
	/// <summary>
	/// This is a test with multiple spaces
	/// </summary>
	public string Localized__Space_Multiple => this.localizer["Localized  Space Multiple"];
	/// <summary>
	/// This is an enum value
	/// </summary>
	public string MyNamespace_MyEnum_MyEnumValue => this.localizer["MyNamespace.MyEnum.MyEnumValue"];
}
