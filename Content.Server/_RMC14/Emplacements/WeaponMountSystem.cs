using Content.Server.Destructible;
using Content.Shared._RMC14.Emplacements;
using Content.Shared._RMC14.Repairable;
using Content.Shared._RMC14.Xenonids.Acid;

namespace Content.Server._RMC14.Emplacements;

public sealed class WeaponMountSystem : SharedWeaponMountSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WeaponMountComponent, RMCRepairableDoAfterEvent>(OnRepair);
    }

    private void OnRepair(Entity<WeaponMountComponent> ent, ref RMCRepairableDoAfterEvent args)
    {
        if (!TryComp(ent, out DestructibleComponent? destructible))
            return;

        if (destructible.IsBroken)
            return;

        if (TryComp(ent, out CorrodibleComponent? corrodible))
        {
            XenoAcid.SetCorrodible(corrodible, false);
            Dirty(ent, corrodible);
        }

        ent.Comp.Broken = false;
        Dirty(ent);
    }
}
