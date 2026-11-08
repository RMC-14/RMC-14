using System;
using Robust.Shared.Maths;

namespace Content.Shared._RMC14.Vehicle;

public static class VehicleTurretDirectionHelpers
{
    private const double SpriteDirectionBiasRadians = -0.05;

    public static Direction GetRenderAlignedCardinalDir(Angle facing)
    {
        var angle = facing.Reduced().FlipPositive();
        var mod = (Math.Floor(angle.Theta / MathHelper.PiOver2) % 2) - 0.5;
        var modTheta = angle.Theta + mod * SpriteDirectionBiasRadians;

        return ((int) Math.Round(modTheta / MathHelper.PiOver2) % 4) switch
        {
            0 => Direction.South,
            1 => Direction.East,
            2 => Direction.North,
            _ => Direction.West
        };
    }
}
