using Content.Server.Medical;
using Content.Shared._RMC14.Chemistry;

namespace Content.Server._RMC14.Chemistry;

public sealed class RMCVomitSystem : EntitySystem
{
    [Dependency] private readonly VomitSystem _vomitSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCVomitEvent>(OnRMCVomit);
    }

    private void OnRMCVomit(ref RMCVomitEvent args)
    {
        _vomitSystem.Vomit(args.Target, args.ThirstAmount, args.HungerAmount);
    }
}
