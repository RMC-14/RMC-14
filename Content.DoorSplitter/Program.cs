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

var firstPath = $"{path}/one.rsi";
var secondPath = $"{path}/two.rsi";
Directory.CreateDirectory(firstPath);
Directory.CreateDirectory(secondPath);

var rsi = Rsi.FromFolder(path);
rsi.TryLoadFolderImages(path);

var firstStates = new List<RsiState>();
var secondStates = new List<RsiState>();
foreach (var state in rsi.States)
{
    var firstState = new RsiState(state.Name, state.Directions, state.Delays, state.Flags, null);
    var secondState = new RsiState(state.Name, state.Directions, state.Delays, state.Flags, null);

    firstStates.Add(firstState);
    secondStates.Add(secondState);

    var frames = state.DelayLength;
    foreach (var direction in DirectionExtensions.GetCardinals())
    {
        for (var i = 0; i < frames; i++)
        {
            var frame = state.Frames[(int) direction, i];
            if (frame == null)
                continue;

            var size = frame.Size();
            var (x1, y1, x2, y2) = direction switch
            {
                Direction.South => (0, 0, 0, size.Height / 2),
                Direction.North => (0, size.Height / 2, 0, 0),
                Direction.East => (0, size.Height / 2, size.Width / 2, size.Height / 2),
                Direction.West => (size.Width / 2, size.Height / 2, 0, size.Height / 2),
                _ => throw new ArgumentOutOfRangeException()
            };

            var first = frame.Clone();
            first.Mutate(c => c.Crop(new Rectangle(x1, y1, size.Width / 2, size.Height / 2)));

            var second = frame.Clone();
            second.Mutate(c => c.Crop(new Rectangle(x2, y2, size.Width / 2, size.Height / 2)));

            firstState.Frames[(int) direction, i] = first;
            secondState.Frames[(int) direction, i] = second;
        }
    }
}

var x = rsi.Size.X / 2;
var y = rsi.Size.Y / 2;
var firstRsi = new Rsi(rsi.Version, rsi.License, rsi.Copyright, x, y, firstStates);
var secondRsi = new Rsi(rsi.Version, rsi.License, rsi.Copyright, x, y, secondStates);

firstRsi.SaveToFolder($"{firstPath}");
secondRsi.SaveToFolder($"{secondPath}");
Console.WriteLine($"Converted {firstStates.Count} states");
