using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.Spreader;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Spreader;

public sealed class RMCSpreaderSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<EdgeSpreaderComponent> _query;

    public override void Initialize()
    {
        _query = GetEntityQuery<EdgeSpreaderComponent>();

        SubscribeLocalEvent<ActiveEdgeSpreaderComponent, ComponentStartup>(OnActiveEdgeSpreaderStartup);
    }

    private void OnActiveEdgeSpreaderStartup(Entity<ActiveEdgeSpreaderComponent> entity, ref ComponentStartup args)
    {
        if (_query.TryComp(entity, out EdgeSpreaderComponent? edgeSpreader))
        {
            entity.Comp.NextSpread = _timing.CurTime + edgeSpreader.SpreadDelay;
        }
        else
        {
            entity.Comp.NextSpread = _timing.CurTime + TimeSpan.FromSeconds(1);
        }
    }
}
