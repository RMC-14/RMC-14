namespace Content.Shared._RMC14.Extensions;

public static class IntExtensions
{
    public static void Cap(this ref int value, int at)
    {
        at = Math.Abs(at);
        if (value > at)
            value = at;
        else if (value < -at)
            value = -at;
    }
}
