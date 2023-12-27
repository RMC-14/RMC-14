using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SpaceWizards.RsiLib.Directions;
using SpaceWizards.RsiLib.RSI;

Console.WriteLine("Enter a path:");
var path = Console.ReadLine();
if (!Directory.Exists(path))
{
    Console.WriteLine($"No directory found with path {path}");
    return;
}

var directories = Directory.EnumerateDirectories(path, "*.rsi", SearchOption.AllDirectories).ToArray();
if (directories.Length == 0)
{
    Console.WriteLine($"No RSIs found in path {path}");
    return;
}

Console.WriteLine($"Found:\n{string.Join('\n', directories)}");
Console.WriteLine($"{directories.Length} total");
Console.WriteLine("Is this correct? [Y/N]");
var response = Console.ReadLine()?.Trim().ToUpper();
if (response != "Y")
{
    Console.WriteLine("Cancelling operation");
    return;
}

foreach (var directory in directories)
{
    Console.WriteLine($"Splitting {directory}");

    var rsi = Rsi.FromFolder(directory);
    rsi.TryLoadFolderImages(directory);

    var states = new List<RsiState>();
    foreach (var state in rsi.States)
    {
        var splitState = new RsiState(state.Name, state.Directions, state.Delays, state.Flags, null);

        states.Add(splitState);

        var frames = state.DelayLength;
        foreach (var direction in DirectionExtensions.GetCardinals())
        {
            for (var i = 0; i < frames; i++)
            {
                var frame = state.Frames[(int) direction, i];
                if (frame == null)
                    continue;

                var size = frame.Size();
                var (x1, y1) = direction switch
                {
                    Direction.South => (0, 0),
                    Direction.North => (0, size.Height / 2),
                    Direction.East => (0, size.Height / 2),
                    Direction.West => (size.Width / 2, size.Height / 2),
                    _ => throw new ArgumentOutOfRangeException()
                };

                var first = frame.Clone();
                first.Mutate(c => c.Crop(new Rectangle(x1, y1, size.Width / 2, size.Height / 2)));

                splitState.Frames[(int) direction, i] = first;
            }
        }
    }

    var x = rsi.Size.X / 2;
    var y = rsi.Size.Y / 2;
    var splitRsi = new Rsi(rsi.Version, rsi.License, rsi.Copyright, x, y, states);

    splitRsi.SaveToFolder(directory);
    Console.WriteLine($"Split {states.Count} states");
}

Console.WriteLine($"Finished splitting {directories.Length} RSIs");
