using Content.Server.Construction;
using Content.Shared._RMC14.Barricade;
using Content.Shared._RMC14.Barricade.Components;

namespace Content.Server._RMC14.Barricade;

public sealed class BarbedSystem : SharedBarbedSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BarbedComponent, ConstructionChangeEntityEvent>(OnBarbedEntityConstructionChange);
    }

    private void OnBarbedEntityConstructionChange(EntityUid ent, BarbedComponent comp, ConstructionChangeEntityEvent args)
    {
        var newComp = EnsureComp<BarbedComponent>(args.New);
        newComp.IsBarbed = comp.IsBarbed;
        base.UpdateBarricade((args.New, newComp));
    }
}
