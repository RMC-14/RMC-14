using Content.Shared.Actions;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._RMC14.Weapons.Ranged.Laser;

public sealed class GunToggleableLaserSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

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
        if (args.Handled)
            return;

        if (ToggleLaser(ent, args.Performer))
            args.Handled = true;
    }

    /// <summary>
    ///     Toggle the active laser.
    /// </summary>
    private bool ToggleLaser(Entity<GunToggleableLaserComponent> ent, EntityUid user)
    {
        _audio.PlayPredicted(ent.Comp.ToggleSound, ent, user);
        ent.Comp.Active = !ent.Comp.Active;

        if (ent.Comp.Settings.Count == 0)
            return false;

        // Change icon
        ref var settingIndex = ref ent.Comp.Setting;
        settingIndex++;
        if (settingIndex >= ent.Comp.Settings.Count)
            settingIndex = 0;

        var setting = ent.Comp.Settings[settingIndex];

        if (ent.Comp.Action is { } action)
            _actions.SetIcon(action, setting.Icon);

        Dirty(ent);
        return true;
    }
}
