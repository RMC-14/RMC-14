using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Deafness;

public abstract class SharedDeafnessSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _net = default!;

    public ProtoId<StatusEffectPrototype> DeafKey = "Deaf";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeafenWhileCritComponent, StatusEffectEndedEvent>(OnCanHear);
        SubscribeLocalEvent<DeafenWhileCritComponent, MobStateChangedEvent>(OnDeafenWhileCritMobState);

        SubscribeLocalEvent<ActiveDeafenWhileCritComponent, MobStateChangedEvent>(OnActiveDeafenWhileCritMobState);
    }

    private void OnCanHear(Entity<DeafenWhileCritComponent> ent, ref StatusEffectEndedEvent args)
    {
        if (args.Key != DeafKey)
            return;

        DoEarLossPopups(ent.Owner, true);
    }

    private void OnDeafenWhileCritMobState(Entity<DeafenWhileCritComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Critical)
            return;

        EnsureComp<ActiveDeafenWhileCritComponent>(ent);
    }

    private void OnActiveDeafenWhileCritMobState(Entity<ActiveDeafenWhileCritComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Critical)
            RemCompDeferred<ActiveDeafenWhileCritComponent>(ent);
    }

    public bool TryDeafen(EntityUid uid, TimeSpan time, bool refresh = false, StatusEffectsComponent? status = null, bool ignoreProtection = false)
    {
        if (!Resolve(uid, ref status, false))
            return false;

        if (time <= TimeSpan.Zero)
            return false;

        if (!ignoreProtection && HasEarProtection(uid))
            return false;

        if (!HasComp<DeafComponent>(uid)) // First time being deafened
            DoEarLossPopups(uid, false);

        if (!_statusEffect.TryAddStatusEffect<DeafComponent>(uid, DeafKey, time, refresh))
            return false;

        var ev = new RMCDeafenedEvent(time);
        RaiseLocalEvent(uid, ref ev);

        return true;
    }

    public void DoEarLossPopups(EntityUid uid, bool end)
    {
        if (_net.IsClient)
            return;

        var msg = Loc.GetString(end ? "rmc-deaf-end" : "rmc-deaf-start");
        _popup.PopupEntity(msg, uid, uid, PopupType.MediumCaution);
    }

    public bool HasEarProtection(EntityUid uid)
    {
        if (_inventory.TryGetContainerSlotEnumerator(uid, out var slots))
        {
            while (slots.NextItem(out var containedEntity, out _))
            {
                if (HasComp<RMCEarProtectionComponent>(containedEntity))
                    return true;
            }
        }

        return HasComp<RMCEarProtectionComponent>(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;

        var activeQuery = EntityQueryEnumerator<ActiveDeafenWhileCritComponent, StatusEffectsComponent>();
        while (activeQuery.MoveNext(out var uid, out var comp, out var status))
        {
            if (comp.AddAt < time)
            {
                comp.AddAt = time + comp.Every;
                TryDeafen(uid, comp.Add, true, status, ignoreProtection: true);
            }
        }
    }
}

/// <summary>
///     Raised directed on an entity when it is made deaf.
/// </summary>
[ByRefEvent]
public record struct RMCDeafenedEvent(TimeSpan Duration);
