namespace Content.Scripts;

public static class DirectoryFinder
{
    public static string[] FindRSIs()
    {
        Console.WriteLine("Enter a path:");
        var path = Console.ReadLine();
        if (!Directory.Exists(path))
        {
            Console.WriteLine($"No directory found with path {path}");
            return Array.Empty<string>();
        }

        var directoriesEnumerable = Directory.EnumerateDirectories(path, "*.rsi", SearchOption.AllDirectories);
        if (path.EndsWith(".rsi"))
        {
            directoriesEnumerable = directoriesEnumerable.Prepend(path);
        }

        var directories = directoriesEnumerable.ToArray();
        if (directories.Length == 0)
        {
            Console.WriteLine($"No RSIs found in path {path}");
            return Array.Empty<string>();
        }

        Console.WriteLine($"Found:\n{string.Join('\n', directories)}");
        Console.WriteLine($"{directories.Length} total");
        Console.WriteLine("Is this correct? [Y/N]");
        var response = Console.ReadLine()?.Trim().ToUpper();
        if (response != "Y")
        {
            Console.WriteLine("Cancelling operation");
            return Array.Empty<string>();
        }

        return directories;
    }
}
