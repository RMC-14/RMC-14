using Content.Shared.Actions;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._RMC14.Weapons.Ranged.Laser;

public sealed class GunToggleableLaserSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GunToggleableLaserComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<GunToggleableLaserComponent, GunToggleLaserActionEvent>(OnToggleLaser);
    }

    /// <summary>
    ///     Add the action to the entity grabbing this item.
    /// </summary>
    private void OnGetItemActions(Entity<GunToggleableLaserComponent> ent, ref GetItemActionsEvent args)
    {
        args.AddAction(ref ent.Comp.Action, ent.Comp.ActionId);
        Dirty(ent.Owner, ent.Comp);
    }

    /// <summary>
    ///     Toggle activation when the action is used.
    /// </summary>
    private void OnToggleLaser(Entity<GunToggleableLaserComponent> ent, ref GunToggleLaserActionEvent args)
    {
        _audio.PlayPredicted(ent.Comp.ToggleSound, ent, args.Performer);
        ent.Comp.Active = !ent.Comp.Active;
        Dirty(ent);
        args.Handled = true;
    }
}
