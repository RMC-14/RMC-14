using Content.Server.Body.Systems;
using Content.Shared.Body.Components;
using Content.Shared._RMC14.Embeds;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Embeds;

public sealed class ForeignObjectEmbeddedSystem : SharedForeignObjectEmbeddedSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        var embeddedQuery = EntityQueryEnumerator<ForeignObjectEmbeddedComponent>();
        while (embeddedQuery.MoveNext(out var uid, out var component))
        {
            if (time < component.NextDamageAt)
                continue;

            ForeignObjectEmbeddedUtility.SetNextDamageAt(component, time + TimeSpan.FromSeconds(1));
            Dirty(uid, component);
        }
    }
}
