using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared._RMC14.Stun;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCCorgiSystem : RMCAnimalSystem
{
    [Dependency] private readonly RMCSizeStunSystem _size = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCCorgiComponent, MapInitEvent>(OnCorgiMapInit);
        SubscribeLocalEvent<RMCLisaCorgiComponent, MapInitEvent>(OnLisaMapInit);
        SubscribeLocalEvent<RMCCorgiComponent, InteractHandEvent>(OnCorgiInteractHand, before: [typeof(InteractionPopupSystem)]);
        SubscribeLocalEvent<RMCCorgiComponent, DisarmedEvent>(OnCorgiDisarmed);
        SubscribeLocalEvent<RMCCorgiComponent, DamageChangedEvent>(OnCorgiDamageChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;
        UpdateCorgis(now);
        UpdateLisaCorgis(now);
    }

    private void OnCorgiMapInit(Entity<RMCCorgiComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextThinkAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.ThinkCooldown);
    }

    private void OnCorgiInteractHand(Entity<RMCCorgiComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled || args.User == ent.Owner || !MobState.IsAlive(ent.Owner))
            return;

        args.Handled = true;
        Popup.PopupEntity(Loc.GetString("rmc-corgi-pet", ("user", args.User), ("corgi", ent.Owner)), ent.Owner);
    }

    private void OnCorgiDisarmed(Entity<RMCCorgiComponent> ent, ref DisarmedEvent args)
    {
        if (args.Handled || args.Target != ent.Owner || !MobState.IsAlive(ent.Owner))
            return;

        args.Handled = true;
        Popup.PopupEntity(Loc.GetString("rmc-corgi-bop", ("user", args.Source), ("corgi", ent.Owner)), ent.Owner);

        if (!XformQuery.HasComp(args.Source))
            return;

        _size.KnockBack(ent.Owner,
            Transform.GetMapCoordinates(args.Source),
            ent.Comp.ShooKnockback,
            ent.Comp.ShooKnockback,
            ent.Comp.ShooKnockbackSpeed,
            true);
    }

    private void OnCorgiDamageChanged(Entity<RMCCorgiComponent> ent, ref DamageChangedEvent args)
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

        var corgiCoords = Transform.GetMoverCoordinates(ent.Owner);
        var originCoords = Transform.GetMoverCoordinates(origin);
        if (!corgiCoords.TryDistance(EntityManager, originCoords, out var distance) || distance > 1.75f)
            return;

        ent.Comp.NextKickPopupAt = Timing.CurTime + ent.Comp.KickPopupCooldown;
        Popup.PopupEntity(Loc.GetString("rmc-corgi-kick", ("user", origin), ("corgi", ent.Owner)), ent.Owner);
    }

    private void UpdateCorgis(TimeSpan now)
    {
        var query = EntityQueryEnumerator<RMCCorgiComponent>();
        while (query.MoveNext(out var uid, out var corgi))
        {
            if (!MobState.IsAlive(uid) ||
                ActorQuery.HasComp(uid) ||
                Container.IsEntityInContainer(uid) ||
                corgi.NextThinkAt > now)
            {
                continue;
            }

            corgi.NextThinkAt = now + corgi.ThinkCooldown;
            TryCorgiAmbientEmote((uid, corgi));
        }
    }

    private void TryCorgiAmbientEmote(Entity<RMCCorgiComponent> ent)
    {
        if (Random.Prob(ent.Comp.HeardEmoteChance))
        {
            Popup.PopupEntity(Loc.GetString(PickCorgiHeardPopup(), ("corgi", ent.Owner)), ent.Owner);
            return;
        }

        if (!Random.Prob(ent.Comp.SeenEmoteChance))
            return;

        Popup.PopupEntity(Loc.GetString(PickCorgiSeenPopup(), ("corgi", ent.Owner)), ent.Owner);
    }

    private string PickCorgiHeardPopup()
    {
        return Random.Next(4) switch
        {
            0 => "rmc-corgi-barks",
            1 => "rmc-corgi-woofs",
            2 => "rmc-corgi-yaps",
            _ => "rmc-corgi-pants",
        };
    }

    private string PickCorgiSeenPopup()
    {
        return Random.Next(2) == 0
            ? "rmc-corgi-shakes-head"
            : "rmc-corgi-shivers";
    }
}
