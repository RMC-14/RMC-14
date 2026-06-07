using Content.Server.Administration;
using Content.Shared._RMC14.Synth;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Synth;

/// <summary>
/// Handles the synthetic K9 name changer dialog and applies the chosen name to the user.
/// </summary>
public sealed class RMCK9NameChangerSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCK9NameChangerComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(Entity<RMCK9NameChangerComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<SynthComponent>(args.User))
        {
            _popup.PopupEntity(Loc.GetString("rmc-k9-name-changer-not-synth"), args.User, args.User, PopupType.SmallCaution);
            args.Handled = true;
            return;
        }

        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        var user = args.User;
        var item = ent.Owner;
        var maxNameLength = ent.Comp.MaxNameLength;

        // The dialog callback can run after the entity state changes, so capture IDs and revalidate them below.
        _quickDialog.OpenDialog(actor.PlayerSession,
            Loc.GetString("rmc-k9-name-changer-title"),
            Loc.GetString("rmc-k9-name-changer-prompt"),
            (string name) =>
            {
                if (Deleted(user) || Deleted(item))
                    return;

                var trimmed = name.Trim();
                if (trimmed.Length == 0 || trimmed.Length > maxNameLength)
                {
                    _popup.PopupEntity(
                        Loc.GetString("rmc-k9-name-changer-invalid", ("max", maxNameLength)),
                        user,
                        user,
                        PopupType.SmallCaution);
                    return;
                }

                _metaData.SetEntityName(user, trimmed);
                _popup.PopupEntity(Loc.GetString("rmc-k9-name-changer-success", ("name", trimmed)), user, user);
            });

        args.Handled = true;
    }
}
