//HintName: ServiceCollectionExtensions.g.cs
namespace MyTest.Library;

public static class ServiceCollectionExtensions_Generated
{
	public static void AddStronglyTypedLocalizations(this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)
	{
		services.AddSingleton<global::MyTest.Library.StringsLocalized>();
	}
}