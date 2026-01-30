using Content.Server.Administration;
using Content.Shared._RMC14.IconLabel;
using Robust.Shared.Player;

namespace Content.Server._RMC14.IconLabel;

public sealed class RMCIconLabelSystem : SharedRMCIconLabelSystem
{
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;

    protected override void TrySetIconLabel(EntityUid user, EntityUid target, int maxLength)
    {
        base.TrySetIconLabel(user, target, maxLength);

        if (!TryComp<ActorComponent>(user, out var actor))
            return;
        if (!HasComp<IconLabelComponent>(target))
            return;

        var prompt = Loc.GetString("rmc-set-icon-label-dialog-prompt", ("item", target), ("max", maxLength));
        var netTarget = GetNetEntity(target);
        var netUser = GetNetEntity(user);
        _quickDialog.OpenDialog(actor.PlayerSession,
            Loc.GetString("rmc-set-icon-label-dialog-title"),
            prompt,
            (string label) =>
            {
                if (!TryGetEntity(netTarget, out var targetEnt) || !TryGetEntity(netUser, out var userEnt))
                    return;

                SetIconLabel(userEnt.Value, targetEnt.Value, label);
            }
        );
    }
}
