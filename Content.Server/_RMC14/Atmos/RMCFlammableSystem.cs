using Content.Server.Atmos.Components;
using Content.Shared._RMC14.Atmos;

namespace Content.Server._RMC14.Atmos;

public sealed class RMCFlammableSystem : SharedRMCFlammableSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlammableComponent, ShowFireAlertEvent>(OnShowFireAlert);
    }

    private void OnShowFireAlert(Entity<FlammableComponent> ent, ref ShowFireAlertEvent args)
    {
        if (ent.Comp.OnFire)
            args.Show = true;
    }
}
