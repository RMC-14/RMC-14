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

public sealed class RMCSmallAnimalSystem : RMCAnimalSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly RMCSizeStunSystem _size = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCTinyLizardComponent, InteractHandEvent>(OnTinyLizardInteractHand, before: [typeof(InteractionPopupSystem)]);
        SubscribeLocalEvent<RMCTinyLizardComponent, DisarmedEvent>(OnTinyLizardDisarmed);
        SubscribeLocalEvent<RMCTinyLizardComponent, DamageChangedEvent>(OnTinyLizardDamageChanged);
        SubscribeLocalEvent<RMCAlienSlugComponent, MapInitEvent>(OnAlienSlugMapInit);
        SubscribeLocalEvent<RMCAlienSlugComponent, InteractHandEvent>(OnAlienSlugInteractHand, before: [typeof(InteractionPopupSystem)]);
        SubscribeLocalEvent<RMCAlienSlugComponent, DisarmedEvent>(OnAlienSlugDisarmed);
        SubscribeLocalEvent<RMCAlienSlugComponent, DamageChangedEvent>(OnAlienSlugDamageChanged);
        SubscribeLocalEvent<RMCAlienSlugComponent, MobStateChangedEvent>(OnAlienSlugMobStateChanged);
        SubscribeLocalEvent<RMCBunnyComponent, MapInitEvent>(OnBunnyMapInit);
        SubscribeLocalEvent<RMCBunnyComponent, InteractHandEvent>(OnBunnyInteractHand, before: [typeof(InteractionPopupSystem)]);
        SubscribeLocalEvent<RMCBunnyComponent, DisarmedEvent>(OnBunnyDisarmed);
        SubscribeLocalEvent<RMCBunnyComponent, DamageChangedEvent>(OnBunnyDamageChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;
        var slugQuery = EntityQueryEnumerator<RMCAlienSlugComponent>();
        while (slugQuery.MoveNext(out var uid, out var slug))
        {
            if (!MobState.IsAlive(uid))
            {
                WakeAlienSlug((uid, slug), false);
                continue;
            }

            if (ActorQuery.HasComp(uid))
            {
                WakeAlienSlug((uid, slug));
                continue;
            }

            if (slug.Sleeping)
            {
                UpdateSleepingAlienSlug((uid, slug), now);
                continue;
            }

            if (slug.NextThinkAt > now)
                continue;

            slug.NextThinkAt = now + slug.ThinkCooldown;
            if (!Container.IsEntityInContainer(uid) && Random.Prob(slug.SleepChance))
            {
                SleepAlienSlug((uid, slug));
                continue;
            }

            TryAlienSlugAmbientEmote((uid, slug), now);
        }

        var bunnyQuery = EntityQueryEnumerator<RMCBunnyComponent>();
        while (bunnyQuery.MoveNext(out var uid, out var bunny))
        {
            if (!MobState.IsAlive(uid) ||
                ActorQuery.HasComp(uid) ||
                Container.IsEntityInContainer(uid) ||
                bunny.NextThinkAt > now)
            {
                continue;
            }

            bunny.NextThinkAt = now + bunny.ThinkCooldown;
            TryBunnyAmbientEmote((uid, bunny));
        }
    }

    private void OnTinyLizardInteractHand(Entity<RMCTinyLizardComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled || args.User == ent.Owner || !MobState.IsAlive(ent.Owner))
            return;

        args.Handled = true;
        Popup.PopupEntity(Loc.GetString("rmc-tiny-lizard-pet", ("user", args.User), ("lizard", ent.Owner)), ent.Owner);

        if (!Random.Prob(ent.Comp.HissChance))
            return;

        Popup.PopupEntity(Loc.GetString("rmc-tiny-lizard-hisses", ("lizard", ent.Owner)), ent.Owner);
        _audio.PlayPvs(ent.Comp.HissSound, ent.Owner);
    }

    private void OnTinyLizardDisarmed(Entity<RMCTinyLizardComponent> ent, ref DisarmedEvent args)
    {
        if (args.Handled || args.Target != ent.Owner || !MobState.IsAlive(ent.Owner))
            return;

        args.Handled = true;
        Popup.PopupEntity(Loc.GetString("rmc-tiny-lizard-shoo", ("user", args.Source), ("lizard", ent.Owner)), ent.Owner);

        if (!XformQuery.HasComp(args.Source))
            return;

        _size.KnockBack(ent.Owner,
            Transform.GetMapCoordinates(args.Source),
            ent.Comp.ShooKnockback,
            ent.Comp.ShooKnockback,
            ent.Comp.ShooKnockbackSpeed,
            true);
    }

    private void OnTinyLizardDamageChanged(Entity<RMCTinyLizardComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased ||
            args.Origin is not { } origin ||
            origin == ent.Owner ||
            !ActorQuery.HasComp(origin) ||
            !MobQuery.HasComp(origin) ||
            ent.Comp.NextStompPopupAt > Timing.CurTime)
        {
            return;
        }

        var lizardCoords = Transform.GetMoverCoordinates(ent.Owner);
        var originCoords = Transform.GetMoverCoordinates(origin);
        if (!lizardCoords.TryDistance(EntityManager, originCoords, out var distance) || distance > 1.75f)
            return;

        ent.Comp.NextStompPopupAt = Timing.CurTime + ent.Comp.StompPopupCooldown;
        Popup.PopupEntity(Loc.GetString("rmc-tiny-lizard-stomp", ("user", origin), ("lizard", ent.Owner)), ent.Owner);
    }

    private void OnAlienSlugMapInit(Entity<RMCAlienSlugComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextThinkAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.ThinkCooldown);
        ent.Comp.NextEmoteAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.EmoteCooldown);
    }

    private void OnAlienSlugInteractHand(Entity<RMCAlienSlugComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled || args.User == ent.Owner || !MobState.IsAlive(ent.Owner))
            return;

        args.Handled = true;
        WakeAlienSlug(ent);
        Popup.PopupEntity(Loc.GetString("rmc-alien-slug-pet", ("user", args.User), ("slug", ent.Owner)), ent.Owner);

        if (Random.Prob(0.35f))
            Popup.PopupEntity(Loc.GetString(PickAlienSlugBlurp(), ("slug", ent.Owner)), ent.Owner);
    }

    private void OnAlienSlugDisarmed(Entity<RMCAlienSlugComponent> ent, ref DisarmedEvent args)
    {
        if (args.Handled || args.Target != ent.Owner || !MobState.IsAlive(ent.Owner))
            return;

        args.Handled = true;
        WakeAlienSlug(ent);
        Popup.PopupEntity(Loc.GetString("rmc-alien-slug-shoo", ("user", args.Source), ("slug", ent.Owner)), ent.Owner);

        if (!XformQuery.HasComp(args.Source))
            return;

        _size.KnockBack(ent.Owner,
            Transform.GetMapCoordinates(args.Source),
            ent.Comp.ShooKnockback,
            ent.Comp.ShooKnockback,
            ent.Comp.ShooKnockbackSpeed,
            true);
    }

    private void OnAlienSlugDamageChanged(Entity<RMCAlienSlugComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        WakeAlienSlug(ent);

        if (args.Origin is not { } origin ||
            origin == ent.Owner ||
            !ActorQuery.HasComp(origin) ||
            !MobQuery.HasComp(origin) ||
            ent.Comp.NextStompPopupAt > Timing.CurTime)
        {
            return;
        }

        var slugCoords = Transform.GetMoverCoordinates(ent.Owner);
        var originCoords = Transform.GetMoverCoordinates(origin);
        if (!slugCoords.TryDistance(EntityManager, originCoords, out var distance) || distance > 1.75f)
            return;

        ent.Comp.NextStompPopupAt = Timing.CurTime + ent.Comp.StompPopupCooldown;
        Popup.PopupEntity(Loc.GetString("rmc-alien-slug-stomp", ("user", origin), ("slug", ent.Owner)), ent.Owner);
    }

    private void OnAlienSlugMobStateChanged(Entity<RMCAlienSlugComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != Content.Shared.Mobs.MobState.Alive)
            WakeAlienSlug(ent, false);
    }

    private void UpdateSleepingAlienSlug(Entity<RMCAlienSlugComponent> ent, TimeSpan now)
    {
        if (ent.Comp.SleepUntil <= now ||
            Random.Prob(ent.Comp.WakeChance))
        {
            WakeAlienSlug(ent);
        }
    }

    private void TryAlienSlugAmbientEmote(Entity<RMCAlienSlugComponent> ent, TimeSpan now)
    {
        if (ent.Comp.NextEmoteAt > now)
            return;

        if (Random.Prob(ent.Comp.BlurpChance))
        {
            ent.Comp.NextEmoteAt = now + ent.Comp.EmoteCooldown;
            Popup.PopupEntity(Loc.GetString(PickAlienSlugBlurp(), ("slug", ent.Owner)), ent.Owner);
            return;
        }

        if (!Random.Prob(ent.Comp.WiggleChance))
            return;

        ent.Comp.NextEmoteAt = now + ent.Comp.EmoteCooldown;
        Popup.PopupEntity(Loc.GetString("rmc-alien-slug-wiggles", ("slug", ent.Owner)), ent.Owner);
    }

    private void SleepAlienSlug(Entity<RMCAlienSlugComponent> ent)
    {
        ent.Comp.Sleeping = true;
        ent.Comp.SleepUntil = Timing.CurTime + RandomTime(ent.Comp.SleepDurationMin, ent.Comp.SleepDurationMax);

        StopMovement(ent.Owner);
        RMCNpc.SleepNPC(ent.Owner);
        Popup.PopupEntity(Loc.GetString("rmc-alien-slug-sleeps", ("slug", ent.Owner)), ent.Owner);
    }

    private void WakeAlienSlug(Entity<RMCAlienSlugComponent> ent, bool popup = true)
    {
        if (!ent.Comp.Sleeping)
            return;

        ent.Comp.Sleeping = false;
        ent.Comp.NextThinkAt = Timing.CurTime + ent.Comp.ThinkCooldown;
        RMCNpc.WakeNPC(ent.Owner);

        if (popup)
            Popup.PopupEntity(Loc.GetString("rmc-alien-slug-wakes", ("slug", ent.Owner)), ent.Owner);
    }

    private string PickAlienSlugBlurp()
    {
        return Random.Next(3) switch
        {
            0 => "rmc-alien-slug-blurb",
            1 => "rmc-alien-slug-blub",
            _ => "rmc-alien-slug-blurp",
        };
    }

    private void OnBunnyMapInit(Entity<RMCBunnyComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextThinkAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.ThinkCooldown);
    }

    private void OnBunnyInteractHand(Entity<RMCBunnyComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled || args.User == ent.Owner || !MobState.IsAlive(ent.Owner))
            return;

        args.Handled = true;
        Popup.PopupEntity(Loc.GetString("rmc-bunny-pet", ("user", args.User), ("bunny", ent.Owner)), ent.Owner);
    }

    private void OnBunnyDisarmed(Entity<RMCBunnyComponent> ent, ref DisarmedEvent args)
    {
        if (args.Handled || args.Target != ent.Owner || !MobState.IsAlive(ent.Owner))
            return;

        args.Handled = true;
        Popup.PopupEntity(Loc.GetString("rmc-bunny-push", ("user", args.Source), ("bunny", ent.Owner)), ent.Owner);

        if (!XformQuery.HasComp(args.Source))
            return;

        _size.KnockBack(ent.Owner,
            Transform.GetMapCoordinates(args.Source),
            ent.Comp.ShooKnockback,
            ent.Comp.ShooKnockback,
            ent.Comp.ShooKnockbackSpeed,
            true);
    }

    private void OnBunnyDamageChanged(Entity<RMCBunnyComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased ||
            args.Origin is not { } origin ||
            origin == ent.Owner ||
            !ActorQuery.HasComp(origin) ||
            !MobQuery.HasComp(origin) ||
            ent.Comp.NextKickPopupAt > Timing.CurTime)
        {
            return;
        }

        var bunnyCoords = Transform.GetMoverCoordinates(ent.Owner);
        var originCoords = Transform.GetMoverCoordinates(origin);
        if (!bunnyCoords.TryDistance(EntityManager, originCoords, out var distance) || distance > 1.75f)
            return;

        ent.Comp.NextKickPopupAt = Timing.CurTime + ent.Comp.KickPopupCooldown;
        Popup.PopupEntity(Loc.GetString("rmc-bunny-kick", ("user", origin), ("bunny", ent.Owner)), ent.Owner);
    }

    private void TryBunnyAmbientEmote(Entity<RMCBunnyComponent> ent)
    {
        if (Random.Prob(ent.Comp.HeardEmoteChance))
        {
            Popup.PopupEntity(Loc.GetString(PickBunnyHeardEmote(), ("bunny", ent.Owner)), ent.Owner);
            return;
        }

        if (!Random.Prob(ent.Comp.SeenEmoteChance))
            return;

        Popup.PopupEntity(Loc.GetString(PickBunnySeenEmote(), ("bunny", ent.Owner)), ent.Owner);
    }

    private string PickBunnyHeardEmote()
    {
        return Random.Next(3) switch
        {
            0 => "rmc-bunny-purrs",
            1 => "rmc-bunny-hums",
            _ => "rmc-bunny-squeaks",
        };
    }

    private string PickBunnySeenEmote()
    {
        return Random.Next(2) == 0
            ? "rmc-bunny-flaps-ears"
            : "rmc-bunny-sniffs";
    }
}
