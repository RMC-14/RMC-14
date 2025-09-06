namespace Content.Shared._RMC14.Maths;

public static class RMCMathExtensions
{
    public static float CircleAreaFromSquareSide(float squareSide)
    {
        return (float) (squareSide / Math.Sqrt(Math.PI));
    }
}
