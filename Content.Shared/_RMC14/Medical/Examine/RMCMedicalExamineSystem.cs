using Content.Shared._RMC14.Medical.Unrevivable;
using Content.Shared._RMC14.Stun;
using Content.Shared.Body.Components;
using Content.Shared.Examine;
using Content.Shared.Mobs.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Medical.Examine;

public sealed class RMCMedicalExamineSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly RMCSizeStunSystem _sizeStun = default!;
    [Dependency] private readonly RMCUnrevivableSystem _unrevivable = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCMedicalExamineComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<RMCMedicalExamineComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(RMCMedicalExamineSystem), -1))
        {
            if (ent.Comp.Simple && _mobState.IsDead(ent.Owner))
            {
                args.PushMarkup(Loc.GetString(ent.Comp.DeadText, ("victim", ent.Owner)));
                return;
            }

            if (HasComp<RMCBlockMedicalExamineComponent>(args.Examiner))
                return;

            args.PushMessage(GetExamineText(ent));
        }
    }

    public FormattedMessage GetExamineText(Entity<RMCMedicalExamineComponent> ent)
    {
        var msg = new FormattedMessage();

        if (TryComp<BloodstreamComponent>(ent, out var bloodstream) && bloodstream.BleedAmount > 0)
        {
            msg.AddMarkupOrThrow(Loc.GetString(ent.Comp.BleedText, ("victim", ent.Owner)));
            msg.PushNewline();
        }

        LocId? stateText = null;

        if (_mobState.IsDead(ent))
            stateText = _unrevivable.IsUnrevivable(ent) ? ent.Comp.UnrevivableText : ent.Comp.DeadText;
        else if (_mobState.IsCritical(ent) || _sizeStun.IsKnockedOut(ent))
            stateText = ent.Comp.CritText;

        if (stateText != null)
            msg.AddMarkupOrThrow(Loc.GetString(stateText, ("victim", ent.Owner)));

        return msg;
    }
}
