# API Reference

## Installation

```bash
dotnet add package Shiny.Extensions.Localization.Generator
dotnet add package Microsoft.Extensions.Localization
```

The NuGet package is a development dependency that includes the Roslyn incremental source generator. It produces no runtime assembly in the consuming project.

## NuGet Packages

| Package | Purpose |
|---------|---------|
| `Shiny.Extensions.Localization.Generator` | Source generator (compile-time only) |
| `Microsoft.Extensions.Localization` | Runtime localization framework |
| `Microsoft.Extensions.DependencyInjection` | DI container (usually included transitively) |

## Generated Types

### {ClassName}Localized

For each `.resx` file, the generator produces a `partial` class named `{ClassName}Localized`:

```csharp
public partial class {ClassName}Localized
{
    readonly IStringLocalizer localizer;

    public {ClassName}Localized(IStringLocalizer<{ClassName}> localizer)
    {
        this.localizer = localizer;
    }

    // Exposes the raw IStringLocalizer for advanced scenarios
    public IStringLocalizer Localizer => this.localizer;

    // For simple strings: generates a property
    public string {PropertyName} => this.localizer["{ResourceKey}"];

    // For format strings: generates a method
    public string {PropertyName}Format(object parameter0, object parameter1, ...)
    {
        return string.Format(this.localizer["{ResourceKey}"], parameter0, parameter1, ...);
    }
}
```

**Key behaviors:**
- Constructor takes `IStringLocalizer<{AssociatedClass}>` for proper resource resolution
- Simple strings (no `{0}` placeholders) → property
- Format strings (with `{0}`, `{1}`, etc.) → method with `Format` suffix
- If the resource key already ends with `Format`, no additional suffix is added
- XML documentation comments are generated from `.resx` `<comment>` elements (falls back to `<value>` if no comment)

### ServiceCollectionExtensions_Generated

A static extension class is generated for DI registration:

```csharp
public static class ServiceCollectionExtensions_Generated
{
    public static IServiceCollection AddStronglyTypedLocalizations(this IServiceCollection services)
    {
        services.AddLocalization();
        services.AddSingleton<{Namespace}.{ClassName}Localized>();
        // ... one line per generated Localized class
        return services;
    }
}
```

**Key behaviors:**
- Calls `AddLocalization()` automatically
- Registers all generated `*Localized` classes as singletons
- Namespace is the project's `RootNamespace` (or `ProjectName` if `RootNamespace` is not set)
- Uses `internal` accessor when `GenerateLocalizersInternal` is `true`

### GeneratedLocalizers

A reflection-free list of every generated localizer, safe under trimming and NativeAOT:

```csharp
public static class GeneratedLocalizers
{
    public static readonly (string Name, Type ResourceSource, Type LocalizedType)[] All = new (string Name, Type ResourceSource, Type LocalizedType)[]
    {
        ("MyViewModel", typeof(MyApp.MyViewModel), typeof(MyApp.MyViewModelLocalized)),
        ("Errors.Validation", typeof(MyApp.Errors.Validation), typeof(MyApp.Errors.ValidationLocalized)),
    };

    // cultures with at least one translated .resx beside a default file (MyViewModel.fr-ca.resx -> "fr-CA")
    public static readonly string[] Cultures = new string[]
    {
        "de",
        "fr-CA",
    };
}
```

- `Name` is the `.resx` file name without extension; `ResourceSource` is the `T` for `IStringLocalizer<T>`; `LocalizedType` is the generated wrapper
- Same namespace and accessor rules as `ServiceCollectionExtensions_Generated`; empty array when there are no `.resx` files
- Build a name lookup with `GeneratedLocalizers.All.ToDictionary(x => x.Name, x => factory.Create(x.ResourceSource))` using `IStringLocalizerFactory`
- `Cultures` is the distinct, sorted union of translation cultures in the assembly, normalized to `CultureInfo.Name` casing. The neutral/default language is not included, and neither are orphaned culture files (`SHINY0001`). Use it for in-app language pickers instead of probing satellite folders, which fails under NativeAOT, single-file and Blazor WASM: `GeneratedLocalizers.Cultures.Select(CultureInfo.GetCultureInfo)`

### Associated Class

- If no class `{Namespace}.{ClassName}` exists in the project being compiled, an empty `partial class` is generated for it
- The `*Localized` class is `public` when the associated class is public and `internal` otherwise; `GenerateLocalizersInternal` applies only to generated associated classes

## MSBuild Properties

Configure the generator via MSBuild properties in your `.csproj`:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `GenerateLocalizersInternal` | bool | `false` | Generate classes with `internal` instead of `public` access modifier |
| `RootNamespace` | string | (project name) | Root namespace for generated `ServiceCollectionExtensions` class |

### Example Configuration
```xml
<PropertyGroup>
    <GenerateLocalizersInternal>True</GenerateLocalizersInternal>
</PropertyGroup>
```

## Resource Key Transformation

Resource keys from `.resx` files are transformed into valid C# identifiers:

| Character in Key | Replacement |
|-----------------|-------------|
| `.` (dot) | `_` (underscore) |
| `-` (hyphen) | `_` (underscore) |
| ` ` (space) | `_` (underscore) |

**Examples:**
| Resource Key | Generated Property/Method Name |
|-------------|-------------------------------|
| `Welcome` | `Welcome` |
| `Error.Required` | `Error_Required` |
| `my-key` | `my_key` |
| `hello world` | `hello_world` |
| `Greeting` (with `{0}`) | `GreetingFormat(object parameter0)` |
| `MyFormat` (with `{0}`) | `MyFormat(object parameter0)` (no double suffix) |

## Format String Detection

The generator uses regex pattern `\{(\d+)(?::[^}]*)?\}` to detect format parameters:

- `{0}` → detected as parameter 0
- `{1:C2}` → detected as parameter 1 (format specifier ignored for detection)
- `{0:MMM dd yyyy}` → detected as parameter 0
- `{braces}` → NOT detected (non-numeric, treated as plain text)
- `{}` → NOT detected (empty braces)
- Duplicate parameter indices are deduplicated
- Parameters are sorted numerically (0, 1, 2...)

## Namespace Resolution

The generated class namespace is computed from:
1. The project's `RootNamespace` (or `ProjectName` if not set)
2. The relative folder path of the `.resx` file from the project root

**Examples:**
| RootNamespace | File Path | Generated Namespace |
|--------------|-----------|-------------------|
| `MyApp` | `MyClass.resx` | `MyApp` |
| `MyApp` | `Folder1/MyClass.resx` | `MyApp.Folder1` |
| `MyApp` | `Features/Auth/LoginPage.resx` | `MyApp.Features.Auth` |
| `MyApp` | `Errors.Validation.resx` | `MyApp.Errors` (class `Validation`) |

## File Filtering

Only default `.resx` files generate a class; culture files (`MyClass.fr-ca.resx`, `MyClass.de.resx`) are used at runtime by `IStringLocalizer`. A file is a culture file when:
1. MSBuild says so (`WithCulture` metadata on the `EmbeddedResource`, the same check that builds satellite assemblies) - always used when available
2. Otherwise, its last segment is a culture name known to the machine
3. Otherwise (no culture data, e.g. invariant globalization), its default file (`MyClass.resx`) exists

Extra dots in a default file name become namespace segments (`Errors.Validation.resx` → `MyApp.Errors.ValidationLocalized`), matching the manifest resource name MSBuild embeds.

> **Changed in 2.1:** 2.0.x skipped every `.resx` with a dot in its name. Since 2.1, a dotted file whose last segment is not a culture (e.g. a leftover `Strings.orig.resx` or `Strings.bak.resx`) is a default file and generates a localizer. Each one reports info diagnostic `SHINY0003`. If its namespace collides with a type (`Strings.orig.resx` → namespace `MyApp.Strings` next to class `MyApp.Strings`), error `SHINY0002` is reported and that file is skipped. Delete or rename stale backup files.

### Diagnostics

| Id | Severity | When |
|----|----------|------|
| `SHINY0000` | Warning | No default `.resx` files were found |
| `SHINY0001` | Warning | A culture file has no default file (e.g. `Orphan.de.resx` without `Orphan.resx`) |
| `SHINY0002` | Error | A file would generate a namespace that is already a type, or a type that is already a namespace (e.g. `Strings.orig.resx` → namespace `MyApp.Strings` while class `MyApp.Strings` exists). The file is skipped; rename or remove it |
| `SHINY0003` | Info | A dotted file name's last segment is not a culture, so the file generates a localizer (e.g. `Strings.orig.resx` → `MyApp.Strings.origLocalized`) |

## Troubleshooting

### Compile error in generated code
- `CS0101 ... already contains a definition for 'X'` should now surface as `SHINY0002` instead; it usually means a stale dotted copy such as `X.orig.resx` sits next to `X.resx`
- If you declared the associated class, ensure it is in the same namespace as the generated `*Localized` class
- A `.Designer.cs` from Visual Studio's `ResXFileCodeGenerator` is picked up as the associated class; set the `.resx` Access Modifier to "No code generation" to remove it

### AddStronglyTypedLocalizations not found
- Ensure `Shiny.Extensions.Localization.Generator` NuGet is installed
- Clean and rebuild the project
- The extension method is in the `RootNamespace` — add the correct `using` directive

### Generated class not appearing
- Verify the `.resx` file is included as `EmbeddedResource` in the project
- The `.props` file included with the package automatically configures `AdditionalFiles`
- Clean and rebuild the project
- Check that the file isn't being treated as a culture file - a last segment like `.de` or `.fr-ca` marks a translation, and a translation with no default file raises `SHINY0001`

### Wrong namespace on generated class
- Check your `RootNamespace` in `.csproj`
- Namespace is computed from the file's relative path to the project root
- Move the `.resx` file to match your desired namespace structure

### Format method not generated
- Ensure the resource value contains format placeholders like `{0}`, `{1}`
- Non-numeric braces like `{name}` are not recognized as format parameters
- Empty braces `{}` are not recognized

### XML docs showing value instead of comment
- Add `<comment>` elements to your `.resx` data entries
- The generator uses the comment if present, otherwise falls back to the value
