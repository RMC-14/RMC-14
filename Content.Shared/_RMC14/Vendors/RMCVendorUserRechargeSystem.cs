using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Vendors;

public sealed partial class RMCVendorUserRechargeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCVendorUserRechargeComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<RMCVendorUserRechargeComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.LastUpdate == TimeSpan.Zero)
            ent.Comp.LastUpdate = _gameTiming.CurTime;
    }
}
