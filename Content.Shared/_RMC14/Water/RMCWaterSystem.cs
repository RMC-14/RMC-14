using Content.Shared._RMC14.Map;
using Content.Shared.NameModifier.EntitySystems;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Water;

public sealed class RMCWaterSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly NameModifierSystem _nameModifier = default!;
    [Dependency] private readonly SharedRMCMapSystem _rmcMap = default!;
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
