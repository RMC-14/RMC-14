namespace Content.Shared._RMC14.Map;

public static class RMCDirectionExtensions
{
    public static (Direction First, Direction Second) GetPerpendiculars(this Direction direction)
    {
        switch (direction)
        {
            case Direction.South:
                return (Direction.West, Direction.East);
            case Direction.SouthEast:
                return (Direction.SouthWest, Direction.NorthEast);
            case Direction.East:
                return (Direction.North, Direction.South);
            case Direction.NorthEast:
                return (Direction.NorthWest, Direction.SouthEast);
            case Direction.North:
                return (Direction.West, Direction.East);
            case Direction.NorthWest:
                return (Direction.SouthWest, Direction.NorthEast);
            case Direction.West:
                return (Direction.North, Direction.South);
            case Direction.SouthWest:
                return (Direction.NorthWest, Direction.SouthEast);
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }
    }
}
