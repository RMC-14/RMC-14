using System.Linq;
using System.Numerics;
using Content.Server._RMC14.Atmos;
using Content.Server._RMC14.Barricade;
using Content.Server._RMC14.NPC;
using Content.Shared._RMC14.Barricade;
using Content.Shared._RMC14.CameraShake;
using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Vents;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Damage;
using Content.Shared._RMC14.Xenonids.Leap;
using Content.Shared.Actions;
using Content.Shared.Atmos.Components;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Physics;
using Content.Shared.Placeable;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Spider;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed class RMCRodentSystem : RMCAnimalSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCRodentBehaviorComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<RMCRodentBehaviorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RMCRodentBehaviorComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RMCRodentBehaviorComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnMapInit(Entity<RMCRodentBehaviorComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextThinkAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.ThinkCooldown);
        ent.Comp.SleepUntil = TimeSpan.Zero;
    }

    private void OnDamageChanged(Entity<RMCRodentBehaviorComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageIncreased)
            WakeRodent(ent);
    }

    private void OnMobStateChanged(Entity<RMCRodentBehaviorComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != Content.Shared.Mobs.MobState.Alive)
            WakeRodent(ent);
    }

    private void OnStartCollide(Entity<RMCRodentBehaviorComponent> ent, ref StartCollideEvent args)
    {
        if (!MobState.IsAlive(ent.Owner))
            return;

        if (ent.Comp.Sleeping)
            WakeRodent(ent);

        if (ent.Comp.NextSqueakAt > Timing.CurTime ||
            !MobQuery.TryComp(args.OtherEntity, out var otherMob) ||
            !MobState.IsAlive(args.OtherEntity, otherMob) ||
            !Random.Prob(ent.Comp.SqueakOnCollideChance))
        {
            return;
        }

        ent.Comp.NextSqueakAt = Timing.CurTime + ent.Comp.SqueakCooldown;
        _audio.PlayPvs(ent.Comp.SqueakSound, ent.Owner);
        Popup.PopupEntity(Loc.GetString("rmc-rodent-squeaks", ("rodent", ent.Owner)), ent.Owner, args.OtherEntity);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCRodentBehaviorComponent>();
        while (query.MoveNext(out var uid, out var rodent))
        {
            if (!MobState.IsAlive(uid))
                continue;

            if (ActorQuery.HasComp(uid))
            {
                WakeRodent((uid, rodent));
                continue;
            }

            if (rodent.Sleeping)
            {
                UpdateSleepingRodent((uid, rodent), now);
                continue;
            }

            if (rodent.NextThinkAt > now)
                continue;

            rodent.NextThinkAt = now + rodent.ThinkCooldown;
            if (!Container.IsEntityInContainer(uid) && Random.Prob(rodent.SleepChance))
                SleepRodent((uid, rodent));
        }
    }

    private void UpdateSleepingRodent(Entity<RMCRodentBehaviorComponent> ent, TimeSpan now)
    {
        if (ent.Comp.SleepUntil <= now ||
            Random.Prob(ent.Comp.WakeChance))
        {
            WakeRodent(ent);
            return;
        }

        if (ent.Comp.NextSnuffleAt > now ||
            !Random.Prob(ent.Comp.SnuffleChance))
        {
            return;
        }

        ent.Comp.NextSnuffleAt = now + ent.Comp.SnuffleCooldown;
        Popup.PopupEntity(Loc.GetString("rmc-rodent-snuffles", ("rodent", ent.Owner)), ent.Owner);
    }

    private void SleepRodent(Entity<RMCRodentBehaviorComponent> ent)
    {
        ent.Comp.Sleeping = true;
        ent.Comp.SleepUntil = Timing.CurTime + RandomTime(ent.Comp.SleepDurationMin, ent.Comp.SleepDurationMax);
        ent.Comp.NextSnuffleAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.SnuffleCooldown);

        StopMovement(ent.Owner);
        RMCNpc.SleepNPC(ent.Owner);
    }

    private void WakeRodent(Entity<RMCRodentBehaviorComponent> ent)
    {
        if (!ent.Comp.Sleeping)
            return;

        ent.Comp.Sleeping = false;
        ent.Comp.NextThinkAt = Timing.CurTime + ent.Comp.ThinkCooldown;
        RMCNpc.WakeNPC(ent.Owner);
    }
}
