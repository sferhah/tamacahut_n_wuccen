public static class PathHelper
{
    public static string GetSvelteDataPath(string languageCode)
    {
        return Path.Combine(GetSveltePath(), $@"public\data\tamacahut_n_wuccen{languageCode}.json");
    }

    public static string GetSvelteImagePath(string fileName)
    {
        return Path.Combine(GetSveltePath(), $@"public\images\{fileName}.svg");
    }

    public static string GetSveltePath()
    {
        string path = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName;

        while (!Directory.GetFiles(path).Any(x => x.EndsWith(".sln")))
        {
            path = Directory.GetParent(path).FullName;
        }

        return Path.Combine(path, $@"Uccen.SvelteApp");
    }

    public static string GetFullPathWithinProject(string filename)
    {
        string path = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName;

        while (!Directory.GetFiles(path).Any(x => x.EndsWith(".csproj")))
        {
            path = Directory.GetParent(path).FullName;
        }

        return Path.Combine(path, filename);
    }
}