using SixLabors.ImageSharp;
using SpaceWizards.RsiLib.Directions;
using SpaceWizards.RsiLib.RSI;

namespace Content.Scripts;

public static class MetaFixer
{
    public static void Run()
    {
        var directories = DirectoryFinder.FindRSIs();
        foreach (var directory in directories)
        {
            Console.WriteLine($"Fixing {directory}");

            var meta = $"{directory}{Path.DirectorySeparatorChar}meta.json";
            if (!File.Exists($"{directory}/meta.json"))
            {
                Console.WriteLine($"No meta.json found in {directory}");
                continue;
            }

            Rsi rsi;
            using (var stream = File.OpenRead(meta))
            {
                try
                {
                    rsi = Rsi.FromMetaJson(stream);
                }
                catch
                {
                    rsi = new Rsi();
                }
            }

            foreach (var file in Directory.EnumerateFiles(directory, "*.png"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var size = Image.Identify(file).Size;
                DirectionType directions;
                if (size.Height == rsi.Size.Y && size.Width == rsi.Size.X)
                {
                    directions = DirectionType.None;
                }
                else if (size.Height == rsi.Size.Y * 2 && size.Width == rsi.Size.X * 2)
                {
                    directions = DirectionType.Cardinal;
                }
                else
                {
                    directions = DirectionType.None;
                }

                rsi.States.Add(new RsiState(name, directions, null, null, null));
            }

            rsi.TryLoadFolderImages(directory);
            rsi.SaveMetadataToFolder(directory);

            Console.WriteLine($"Saved {meta}");
        }

        Console.WriteLine($"Fixed {directories.Length} RSIs");
        Console.WriteLine("Check each meta.json for missing license, copyright or state directions");
    }
}
