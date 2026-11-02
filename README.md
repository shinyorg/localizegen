# Microsoft.Extensions.Localization Strongly Typed Class Generator

Generates strongly typed classes for resources to be used by IStringLocalizer from Microsoft.Extensions.Localizer
_This is very much adapted (and a bunch copied) code from https://github.com/VocaDB/ResXFileCodeGenerator_

This directory structure:
- MyViewModel.cs
- MyViewModel.resx
- MyViewModel.fr-ca.rex
- Folder1\FolderViewModel.cs
- Folder1\FolderViewModel.resx
- Folder1\FolderViewModel.fr-ca.resx

Will generate:
- MyViewModelLocalized.g.cs (RootNamespace.MyViewModelLocalized)
- Folder1.FolderViewModelLocalized.g.cs (RootNamespace.Folder1.FolderViewModelLocalized)
- ServiceCollections.g.cs 


## To use this:
1. Install Microsoft.Extensions.Localization
2. Install Shiny.Extensions.Localization.Generator
3. In your MauiProgram.cs, do the following
```csharp
builder.Services.AddLocalization();
builder.Services.AddStronglyTypedLocalizations();
```
4. Now add an `.resx` file to your project that matches the name of class (ie. ViewModel, Controller, Service, etc).
5. Now inject the strongly typed classes
```csharp
public class MyViewModel
{
    public MyViewModel(MyViewModelLocalized localizer) // same namespace and class name with "Localized" suffix
        => this.Localizer = localizer;

    public MyViewModelLocalized Localizer { get; }
}
```
6.Now bind (xaml intellisense will pick it up)
```xml
<Label Text="{Binding Localizer.MyKey}" />
```

> [!WARNING]
> If the "class" beside the resource does not exist, a compile error will occur with the generated code

## Generate Classes with Internal Accessor

```xml
<PropertyGroup>
    <GenerateLocalizersInternal>True</GenerateLocalizersInternal>
</PropertyGroup>
```

## Commercial Support

Shipping this to production? [ShinySoft Technologies](https://shinysoft.biz), the company that builds and maintains the Shiny libraries, offers priority issue response, support agreements, architecture reviews and private office hours. See [Commercial Support](https://shinylib.net/foundation/support/?utm_source=github&utm_medium=readme#commercial-support) or [get in touch](https://shinysoft.biz/contact?utm_source=github&utm_medium=readme). Community support through GitHub issues and discussions stays free.
