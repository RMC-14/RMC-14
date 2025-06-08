namespace Content.Shared._RMC14.Maths;

public static class RMCDirectionExtensions
{
    public static string GetShorthand(this Direction direction)
    {
        return direction switch
        {
            Direction.South => "S",
            Direction.SouthEast => "SE",
            Direction.East => "E",
            Direction.NorthEast => "NE",
            Direction.North => "N",
            Direction.NorthWest => "NW",
            Direction.West => "W",
            Direction.SouthWest => "SW",
            _ => string.Empty,
        };
    }
}
