using Content.Shared._RMC14.Map;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Water;

public sealed class RMCWaterSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly NameModifierSystem _nameModifier = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly List<(EntityUid Id, TimeSpan SpreadAt)> _makeActive = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<PurifiableWaterComponent, MapInitEvent>(OnPurifiableWaterMapInit);
        SubscribeLocalEvent<PurifiableWaterComponent, RefreshNameModifiersEvent>(OnPurifiableWaterRefreshNameModifiers);
    }

    private void OnPurifiableWaterMapInit(Entity<PurifiableWaterComponent> ent, ref MapInitEvent args)
    {
        UpdateAppearance(ent);
    }

    private void OnPurifiableWaterRefreshNameModifiers(Entity<PurifiableWaterComponent> ent, ref RefreshNameModifiersEvent args)
    {
        var loc = ent.Comp.Toxic ? "rmc-water-toxic-name" : "rmc-water-purified-name";
        args.AddModifier(loc);
    }

    private void UpdateAppearance(Entity<PurifiableWaterComponent> ent)
    {
        var visual = ent.Comp.Toxic ? PurifiableWaterVisuals.Toxic : PurifiableWaterVisuals.Purified;
        _appearance.SetData(ent.Owner, PurifiableWaterLayers.Layer, visual);
        _nameModifier.RefreshNameModifiers(ent.Owner);
    }

    public bool CanCollide(Entity<RMCWaterComponent?> water, EntityUid user)
    {
        if (!Resolve(water, ref water.Comp, false))
            return true;

        if (water.Comp.Cover is not { } cover)
            return true;

        var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(water);
        while (anchored.MoveNext(out var anchoredId))
        {
            if (_entityWhitelist.IsWhitelistPass(cover, anchoredId))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Returns true for uncovered RMC water that should apply water contact effects.
    /// </summary>
    public bool IsActiveWater(EntityUid water, EntityUid user, RMCWaterComponent? component = null)
    {
        return IsActiveWater((water, component), user);
    }

    public bool IsActiveWater(Entity<RMCWaterComponent?> water, EntityUid user)
    {
        if (!Resolve(water, ref water.Comp, false))
            return false;

        return CanCollide(water, user);
    }

    /// <summary>
    /// Checks current physics contacts for active RMC water.
    /// </summary>
    public bool IsInWater(EntityUid user, FixturesComponent? fixtures = null)
    {
        if (!Resolve(user, ref fixtures, false))
            return false;

        var contacts = _physics.GetContacts((user, fixtures));
        while (contacts.MoveNext(out var contact))
        {
            if (!contact.IsTouching)
                continue;

            if (IsActiveWater(contact.OtherEnt(user), user))
                return true;
        }

        return false;
    }

    public override void Update(float frameTime)
    {
        _makeActive.Clear();
        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<ActiveWaterComponent, PurifiableWaterComponent>();
        while (query.MoveNext(out var uid, out var active, out var purifiable))
        {
            if (time < active.SpreadAt)
                continue;

            foreach (var cardinal in _rmcMap.CardinalDirections)
            {
                var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(uid, cardinal);
                while (anchored.MoveNext(out var adjacent))
                {
                    if (TryComp(adjacent, out PurifiableWaterComponent? adjacentPurifiable) &&
                        adjacentPurifiable.Toxic != purifiable.Toxic)
                    {
                        adjacentPurifiable.Toxic = purifiable.Toxic;
                        Dirty(adjacent, adjacentPurifiable);
                        UpdateAppearance((adjacent, adjacentPurifiable));

                        _makeActive.Add((adjacent, time + adjacentPurifiable.Delay));
                    }
                }
            }
        }

        foreach (var (id, spreadAt) in _makeActive)
        {
            var adjacentActive = EnsureComp<ActiveWaterComponent>(id);
            adjacentActive.SpreadAt = spreadAt;
            Dirty(id, adjacentActive);
        }
    }
}
