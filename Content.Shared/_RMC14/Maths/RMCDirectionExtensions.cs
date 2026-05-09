namespace Content.Shared._RMC14.Maths;

public static class RMCDirectionExtensions
{
    public static string GetShorthand(this Direction direction)
    {
        return direction switch
        {
            Direction.South => "Ю",
            Direction.SouthEast => "С-В",
            Direction.East => "В",
            Direction.NorthEast => "С-В",
            Direction.North => "С",
            Direction.NorthWest => "С-З",
            Direction.West => "З",
            Direction.SouthWest => "Ю-З",
            _ => string.Empty,
        };
    }
}
