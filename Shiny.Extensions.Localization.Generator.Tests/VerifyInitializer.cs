using System.Runtime.CompilerServices;

namespace Shiny.Extensions.Localization.Generator.Tests;


public class VerifyInitializer
{
    [ModuleInitializer]
    public static void Init() =>
        VerifySourceGenerators.Initialize();
}