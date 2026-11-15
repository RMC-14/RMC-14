using System.Runtime.InteropServices;
using Content.Server.Body.Systems;
using Content.Server._RMC14.Medical.Wounds;
using Content.Shared._DwarfMC.Medical.DwarfMed;
using Content.Shared._RMC14.Medical.Surgery.Steps.Parts;
using Content.Shared._RMC14.Medical.Wounds;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._DwarfMC.Medical.DwarfMed;

public sealed class DwarfBleedingSystem : EntitySystem
{
    private const float IncisionBleedRate = 0.1f;
    private const float HeartBleedRate = 0.25f;
    private const float HeartBleedThreshold = 0.3f;

    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(WoundsSystem));
        SubscribeLocalEvent<DwarfAnatomyComponent, DwarfBleedingEvent>(OnBleeding);
    }

    public static bool HasBleeding(EntityUid uid, IEntityManager entities)
    {
        var ev = new DwarfBleedingEvent(0);
        entities.EventBus.RaiseLocalEvent(uid, ref ev);
        return ev.Amount > 0;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<DwarfAnatomyComponent, BloodstreamComponent>();
        while (query.MoveNext(out var uid, out _, out var bloodstream))
        {
            var bleedEv = new CMBleedAttemptEvent();
            RaiseLocalEvent(uid, ref bleedEv);

            var dwarfBleed = new DwarfBleedingEvent(0);
            RaiseLocalEvent(uid, ref dwarfBleed);

            float bloodloss = 0;
            if (TryComp<WoundedComponent>(uid, out var wounded))
            {
                var wounds = CollectionsMarshal.AsSpan(wounded.Wounds);
                for (var i = 0; i < wounds.Length; i++)
                {
                    ref var wound = ref wounds[i];
                    if (wound.Healed >= wound.Damage)
                        continue;

                    if (!bleedEv.Cancelled && !wound.Treated && time < wound.StopBleedAt)
                        bloodloss += wound.Bloodloss;
                }
            }

            if (!bleedEv.Cancelled)
            {
                bloodloss += dwarfBleed.Amount;
            }
            else if (TryComp<MobStateComponent>(uid, out var mobState) &&
                     mobState.CurrentState == MobState.Dead)
            {
                bloodloss += dwarfBleed.Amount * 0.5f;
            }

            var delta = bloodloss - bloodstream.BleedAmount;
            _bloodstream.TryModifyBleedAmount(uid, delta);
        }
    }

    private void OnBleeding(Entity<DwarfAnatomyComponent> ent, ref DwarfBleedingEvent args)
    {
        foreach (var (part, _) in _body.GetBodyChildren(ent.Owner))
        {
            if (HasComp<CMIncisionOpenComponent>(part))
                args.Amount += IncisionBleedRate;

            if (!TryComp<DwarfBodyPartConditionComponent>(part, out var condition))
                continue;

            if (condition.StumpBleeding)
                args.Amount += condition.StumpBleedRate;

            if (condition.InternalBleeding)
                args.Amount += condition.InternalBleedRate;
        }

        foreach (var (organId, _) in _body.GetBodyOrgans(ent.Owner))
        {
            if (!TryComp<DwarfOrganHealthComponent>(organId, out var organ) ||
                organ.OrganType != DwarfOrganType.Heart ||
                organ.MaxDamage.Float() <= 0f)
            {
                continue;
            }

            var ratio = organ.Damage.Float() / organ.MaxDamage.Float();
            if (ratio <= HeartBleedThreshold)
                continue;

            args.Amount += HeartBleedRate * ratio * (organ.Ruptured ? 2f : 1f);
        }
    }
}
