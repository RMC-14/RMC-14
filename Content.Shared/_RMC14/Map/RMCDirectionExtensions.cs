namespace Content.Shared._RMC14.Map;

public static class RMCDirectionExtensions
{
    public static (Direction First, Direction Second) GetPerpendiculars(this Direction direction)
    {
        switch (direction)
        {
            case Direction.North:
            case Direction.South:
                return (Direction.West, Direction.East);
            case Direction.SouthEast:
            case Direction.NorthWest:
                return (Direction.SouthWest, Direction.NorthEast);
            case Direction.East:
            case Direction.West:
                return (Direction.North, Direction.South);
            case Direction.NorthEast:
            case Direction.SouthWest:
                return (Direction.NorthWest, Direction.SouthEast);
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }
    }

    public static bool IsCardinal(this Direction direction)
    {
        return direction is Direction.North or Direction.East or Direction.South or Direction.West;
    }
}
