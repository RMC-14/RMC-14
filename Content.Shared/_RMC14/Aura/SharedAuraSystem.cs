using Content.Shared._RMC14.Stealth;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Aura;

public abstract class SharedAuraSystem : EntitySystem
{

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;

    /// <summary>
    /// Gives an entity the aura component, and replaces any previous auras
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="auraColor"></param>
    /// <param name="duration">null = lasts forever</param>
    public void GiveAura(EntityUid ent, Color auraColor, TimeSpan? duration, float outlineWidth = 2)
    {
        //No aura to invis lurkers etc
        if (HasComp<EntityActiveInvisibleComponent>(ent))
            return;

        var aura = EnsureComp<AuraComponent>(ent);

        aura.Color = auraColor;
        aura.ExpiresAt = _timing.CurTime + duration;
        aura.OutlineWidth = outlineWidth;

        Dirty(ent, aura);
    }


    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;

        var auraQuery = EntityQueryEnumerator<AuraComponent>();

        while (auraQuery.MoveNext(out var uid, out var aura))
        {
            if (aura.ExpiresAt == null || time < aura.ExpiresAt)
                continue;

            RemCompDeferred<AuraComponent>(uid);
        }
    }
}
