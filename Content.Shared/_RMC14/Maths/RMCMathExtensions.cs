namespace Content.Shared._RMC14.Maths;

public static class RMCMathExtensions
{
    /// <summary>
    /// Returns the radius of the circle
    /// The circle's area is equal to a square who's area is squareSide * squareside
    /// </summary>
    /// <param name="squareSide"></param>
    /// <returns></returns>
    public static float CircleAreaFromSquareSide(float squareSide)
    {
        return (float) (squareSide / Math.Sqrt(Math.PI));
    }

    /// <summary>
    /// Returns the radius of the circle
    /// The circle's area is equal to a square who's area is (squareRadius * 2) + 1
    /// </summary>
    /// <param name="squareRadius"></param>
    /// <returns></returns>
    public static float CircleAreaFromSquareAbilityRange(float squareRadius)
    {
        return (float)((squareRadius * 2 + 1) / Math.Sqrt(Math.PI));
    }
}
