using System.Numerics;
using Content.Shared._RMC14.NewPlayer;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._RMC14.NewPlayer;

public sealed class NewPlayerVisualizerSystem : VisualizerSystem<NewPlayerLabelComponent>
{
    [Dependency] private readonly IPlayerManager _player = default!;

    private EntityQuery<SeeNewPlayersComponent> _seeNewPlayersQuery;

    public override void Initialize()
    {
        base.Initialize();

        _seeNewPlayersQuery = GetEntityQuery<SeeNewPlayersComponent>();

        SubscribeLocalEvent<SeeNewPlayersComponent, LocalPlayerAttachedEvent>(OnSeeNewPlayersLocalAttached);
        SubscribeLocalEvent<SeeNewPlayersComponent, LocalPlayerDetachedEvent>(OnSeeNewPlayersLocalDetached);
    }

    private void OnSeeNewPlayersLocalAttached(Entity<SeeNewPlayersComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        UpdateAllAppearance();
    }

    private void OnSeeNewPlayersLocalDetached(Entity<SeeNewPlayersComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        UpdateAllAppearance();
    }

    protected override void OnAppearanceChange(EntityUid uid, NewPlayerLabelComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        UpdateAppearance((uid, args.Component, args.Sprite));
    }

    private void UpdateAllAppearance()
    {
        var query = AllEntityQuery<NewPlayerLabelComponent, AppearanceComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out _, out var appearance, out var sprite))
        {
            UpdateAppearance((uid, appearance, sprite));
        }
    }

    private void UpdateAppearance(Entity<AppearanceComponent, SpriteComponent> ent)
    {
        if (!ent.Comp2.LayerMapTryGet(NewPlayerLayers.Layer, out var layer))
            return;

        if (!_seeNewPlayersQuery.TryComp(_player.LocalEntity, out var see) ||
            !AppearanceSystem.TryGetData(ent, NewPlayerLayers.Layer, out NewPlayerVisuals visual, ent))
        {
            ent.Comp2.LayerSetVisible(layer, false);
            return;
        }

        var state = visual switch
        {
            NewPlayerVisuals.One => see.OneLabel,
            NewPlayerVisuals.Two => see.TwoLabel,
            NewPlayerVisuals.Three => see.ThreeLabel,
            _ => null,
        };

        if (state == null)
            return;

        ent.Comp2.LayerSetSprite(layer, state);
        ent.Comp2.LayerSetVisible(layer, true);
        ent.Comp2.LayerSetOffset(layer, new Vector2(0, 0.21f));
    }
}
