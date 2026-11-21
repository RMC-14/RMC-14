// ReSharper disable CheckNamespace

using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    public void SwapBallisticAmmo(
        Entity<BallisticAmmoProviderComponent> ent,
        BaseContainer stored,
        List<EntityUid> storedEntities,
        ref int storedUnspawned)
    {
        var loaded = new List<EntityUid>(ent.Comp.Entities);
        var toLoad = new List<EntityUid>(storedEntities);

        foreach (var round in loaded)
        {
            Containers.Insert(round, stored);
        }

        foreach (var round in toLoad)
        {
            Containers.Insert(round, ent.Comp.Container);
        }

        ent.Comp.Entities.Clear();
        ent.Comp.Entities.AddRange(toLoad);
        storedEntities.Clear();
        storedEntities.AddRange(loaded);

        (ent.Comp.UnspawnedCount, storedUnspawned) = (storedUnspawned, ent.Comp.UnspawnedCount);

        DirtyField(ent, ent.Comp, nameof(BallisticAmmoProviderComponent.Entities));
        DirtyField(ent, ent.Comp, nameof(BallisticAmmoProviderComponent.UnspawnedCount));
        UpdateBallisticAppearance(ent, ent.Comp);
        UpdateAmmoCount(ent);
    }

    public bool TryTransferBallisticRound(Entity<BallisticAmmoProviderComponent> ent, BaseContainer target, out EntityUid? round)
    {
        round = null;
        if (ent.Comp.Entities.Count > 0)
        {
            var next = ent.Comp.Entities[^1];
            ent.Comp.Entities.RemoveAt(ent.Comp.Entities.Count - 1);
            DirtyField(ent, ent.Comp, nameof(BallisticAmmoProviderComponent.Entities));
            Containers.Insert(next, target);
            round = next;
        }
        else if (ent.Comp.UnspawnedCount > 0)
        {
            ent.Comp.UnspawnedCount--;
            DirtyField(ent, ent.Comp, nameof(BallisticAmmoProviderComponent.UnspawnedCount));
        }
        else
        {
            return false;
        }

        UpdateBallisticAppearance(ent, ent.Comp);
        UpdateAmmoCount(ent);
        return true;
    }

    public void EjectBallisticRound(Entity<BallisticAmmoProviderComponent> ent)
    {
        Cycle(ent, ent.Comp, TransformSystem.GetMapCoordinates(ent));
        UpdateBallisticAppearance(ent, ent.Comp);
        UpdateAmmoCount(ent);
    }
}
