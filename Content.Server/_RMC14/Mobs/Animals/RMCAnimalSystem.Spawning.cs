using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Mobs.Animals;

public abstract partial class RMCAnimalSystem
{
    protected int CountNearby<T>(MapCoordinates coordinates, float range) where T : IComponent
    {
        var count = 0;
        foreach (var ent in Lookup.GetEntitiesInRange<T>(coordinates, range))
        {
            if (ent.Owner.Valid)
                count++;
        }

        return count;
    }

    protected void SpawnNear(EntProtoId prototype, EntityCoordinates coordinates, float radius)
    {
        var offset = Random.NextAngle().RotateVec(Vector2.UnitX) * Random.NextFloat(0f, radius);
        Spawn(prototype, coordinates.Offset(offset));
    }
}
