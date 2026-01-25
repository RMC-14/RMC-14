using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Rangefinder.Spotting;
using Content.Shared._RMC14.Weapons.Ranged.AimedShot.FocusedShooting;
using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client._RMC14.Weapons.Ranged.Sniper.Focused;

public sealed class RMCFocusingSystem : EntitySystem
{
    private const string FocusedKey = "focused";

    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCFocusingComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnComponentRemove(Entity<RMCFocusingComponent> ent, ref ComponentRemove args)
    {
        var entity = _player.LocalEntity;

        if(ent.Owner != entity)
            return;

        _appearance.SetData(ent.Comp.FocusTarget, FocusedVisuals.Focused, false);
    }

    /// <summary>
    ///     Enable or disable a visualizer only on the client of the entity that is targeting and any spotters.
    ///     Since aimed shot is never ran clientside this has to be an update.
    /// </summary>
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RMCFocusingComponent>();

        while (query.MoveNext(out var uid, out var component))
        {
            var entity = _player.LocalEntity;

            if (component.OldTarget != null)
            {
                _appearance.SetData(component.OldTarget.Value, FocusedVisuals.Focused, false);
                component.OldTarget = null;
            }

            // Only the sniper and Spotters can see what snipers are focusing on.
            if(uid != entity && !HasComp<SpotterWhitelistComponent>(uid))
                return;

            if(!TryComp(component.FocusTarget, out SpriteComponent? sprite) || !sprite.LayerExists(FocusedKey))
                return;

            sprite.LayerMapTryGet(FocusedKey, out var layerIndex);
            sprite.TryGetLayer(layerIndex, out var layer);

            if(layer != null && layer.Visible)
                return;

            _appearance.SetData(component.FocusTarget, FocusedVisuals.Focused, true);


            if (!TryComp(uid, out SquadMemberComponent? squadMember))
                return;

            sprite.LayerSetColor(FocusedKey, squadMember.BackgroundColor);
        }
    }
}
