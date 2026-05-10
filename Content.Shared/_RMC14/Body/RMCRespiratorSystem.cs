using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Medical.Stasis;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Body;

public sealed class RMCRespiratorSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedRMCEmoteSystem _emote = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedRMCDamageableSystem _rmcDamageable = default!;
    [Dependency] private readonly CMStasisBagSystem _stasisBag = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly ProtoId<DamageGroupPrototype> AirlossGroup = "Airloss";
    private static readonly ProtoId<EmotePrototype> GaspEmote = "Gasp";

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<RMCRespiratorComponent>();
        while (query.MoveNext(out var uid, out var respirator))
        {
            if (time < respirator.NextBreathAt)
                continue;

            respirator.NextBreathAt = time + respirator.BreathInterval;
            Dirty(uid, respirator);

            if (_mobState.IsDead(uid))
                continue;

            if (!_stasisBag.CanBodyMetabolize(uid))
                continue;

            ProcessBreath((uid, respirator));
        }
    }

    /// <summary>
    ///     Processes a single breath cycle for the entity.
    ///     Runs once per <see cref="RMCRespiratorComponent.BreathInterval"/>.
    /// </summary>
    private void ProcessBreath(Entity<RMCRespiratorComponent> ent)
    {
        if (ent.Comp.LoseBreath > 0)
        {
            ent.Comp.LoseBreath--;
            Dirty(ent, ent.Comp);

            if (_random.Prob(0.2f))
                _emote.TryEmoteWithChat(ent, GaspEmote, forceEmote: true);

            return;
        }

        if (ent.Comp.BreathHealAmount > FixedPoint2.Zero)
        {
            if (!TryComp(ent, out DamageableComponent? damageable))
                return;

            if (damageable.DamagePerGroup.GetValueOrDefault(AirlossGroup) > FixedPoint2.Zero)
            {
                var heal = _rmcDamageable.DistributeHealingCached(ent.Owner, AirlossGroup, ent.Comp.BreathHealAmount);
                _damageable.TryChangeDamage(ent, heal, ignoreResistances: true, interruptsDoAfters: false);
            }
        }
    }

    /// <summary>
    ///     Sets the entity's <see cref="RMCRespiratorComponent.LoseBreath"/> to <paramref name="value"/>.
    /// </summary>
    /// <remarks>Positive values increase suffocation; negative values reduce it.</remarks>
    public void SetLoseBreath(Entity<RMCRespiratorComponent?> ent, float value)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.LoseBreath = value;
        Dirty(ent, ent.Comp);
    }
}
