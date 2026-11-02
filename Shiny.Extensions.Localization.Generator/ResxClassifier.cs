using System.Globalization;

namespace Shiny.Extensions.Localization.Generator;


public class OrphanedCultureFile
{
    public OrphanedCultureFile(FileOptions file, string culture, string defaultFileName)
    {
        this.File = file;
        this.Culture = culture;
        this.DefaultFileName = defaultFileName;
    }

    public FileOptions File { get; }
    public string Culture { get; }
    public string DefaultFileName { get; }
}


/// <summary>
/// Splits .resx files into default resource files (which get a localizer) and culture files
/// (Strings.de.resx, Errors.Validation.fr-CA.resx) which only feed satellite assemblies
/// </summary>
public static class ResxClassifier
{
    static readonly Lazy<HashSet<string>> knownCultures = new(LoadKnownCultures);


    /// <param name="knownCultureNames">culture names to recognise when MSBuild doesn't supply WithCulture - defaults to this machine's cultures (tests pass their own)</param>
    public static List<FileOptions> GetDefaultFiles(
        IEnumerable<FileOptions> files,
        out List<OrphanedCultureFile> orphanedCultureFiles,
        IReadOnlyCollection<string>? knownCultureNames = null
    )
    {
        var known = knownCultureNames == null
            ? knownCultures.Value
            : new HashSet<string>(knownCultureNames, StringComparer.OrdinalIgnoreCase);

        var all = files.ToList();
        var allPaths = new HashSet<string>(all.Select(x => x.FilePath), StringComparer.OrdinalIgnoreCase);
        var defaults = new List<FileOptions>();
        var cultures = new List<(FileOptions File, string Culture, string DefaultPath)>();

        foreach (var file in all)
        {
            if (TryGetCulture(file, allPaths, known, out var culture, out var defaultPath))
                cultures.Add((file, culture, defaultPath));
            else
                defaults.Add(file);
        }

        var defaultPaths = new HashSet<string>(defaults.Select(x => x.FilePath), StringComparer.OrdinalIgnoreCase);
        orphanedCultureFiles = cultures
            .Where(x => !defaultPaths.Contains(x.DefaultPath))
            .Select(x => new OrphanedCultureFile(x.File, x.Culture, Path.GetFileName(x.DefaultPath)))
            .ToList();

        return defaults;
    }


    static bool TryGetCulture(FileOptions file, HashSet<string> allPaths, HashSet<string> known, out string culture, out string defaultPath)
    {
        culture = String.Empty;
        defaultPath = String.Empty;

        var name = file.NameWithoutExtension;
        var lastDot = name.LastIndexOf('.');
        var segment = lastDot < 0 ? null : name.Substring(lastDot + 1);

        bool isCulture;
        if (file.WithCulture != null)
        {
            // MSBuild already decided - use its answer so we always agree with the satellite assemblies it builds
            isCulture = file.WithCulture.Value;
            if (isCulture && file.Culture != null && name.EndsWith("." + file.Culture, StringComparison.OrdinalIgnoreCase))
            {
                lastDot = name.Length - file.Culture.Length - 1;
                segment = file.Culture;
            }
        }
        else if (segment == null)
        {
            isCulture = false;
        }
        else if (known.Count > 0)
        {
            // same rule as MSBuild: the last segment must be a culture name this machine knows
            isCulture = known.Contains(segment);
        }
        else
        {
            // no culture data (invariant globalization) - treat it as a culture file when its default file is present
            isCulture = allPaths.Contains(GetDefaultPath(file.FilePath, name.Substring(0, lastDot)));
        }

        if (!isCulture || segment == null || lastDot < 0)
            return false;

        culture = segment;
        defaultPath = GetDefaultPath(file.FilePath, name.Substring(0, lastDot));
        return true;
    }


    static string GetDefaultPath(string filePath, string defaultName)
        => Path.Combine(Path.GetDirectoryName(filePath) ?? String.Empty, defaultName + Path.GetExtension(filePath));


    static HashSet<string> LoadKnownCultures()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var culture in CultureInfo.GetCultures(CultureTypes.AllCultures))
            {
                if (!String.IsNullOrEmpty(culture.Name))
                    set.Add(culture.Name);
            }
        }
        catch
        {
            // culture data unavailable - fall back to the sibling file check
        }
        return set;
    }
}
