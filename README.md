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
4. Now inject the strongly typed classes
```csharp
public class MyViewModel
{
    public MyViewModel(MyViewModelLocalized localizer)
        => this.Localizer = localizer;

    public MyViewModelLocalized Localizer { get; }
}
```
5. Now bind (xaml intellisense will pick it up)
```xml
<Label Text="{Binding Localizer.MyKey}" />
```

WARNING: If the "class" beside the resource does not exist, a compile error will occur with the generated code