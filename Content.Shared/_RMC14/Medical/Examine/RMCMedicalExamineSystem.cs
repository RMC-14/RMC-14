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
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly RMCUnrevivableSystem _unrevivable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly RMCSizeStunSystem _sizeStun = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCMedicalExamineComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<RMCMedicalExamineComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
    }

    private void OnExamined(Entity<RMCMedicalExamineComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.Simple || !_mobState.IsDead(ent.Owner))
            return;

        using (args.PushGroup(nameof(RMCMedicalExamineSystem), 1))
        {
            args.PushMarkup(Loc.GetString(ent.Comp.DeadText, ("victim", ent.Owner)));
        }
    }

    private void OnGetExamineVerbs(Entity<RMCMedicalExamineComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (ent.Comp.Simple)
            return;

        var user = args.User;

        if (HasComp<RMCBlockMedicalExamineComponent>(user))
            return;

        var detailsRange = _examine.IsInDetailsRange(user, ent.Owner);

        var verb = new ExamineVerb()
        {
            Act = () =>
            {
                var text = GetExamineText(ent);
                _examine.SendExamineTooltip(user, ent, text, false, false);
            },
            Text = Loc.GetString("rmc-medical-examine-verb"),
            Category = VerbCategory.Examine,
            Disabled = !detailsRange,
            Message = detailsRange ? null : Loc.GetString("health-examinable-verb-disabled"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/rejuvenate.svg.192dpi.png"))
        };

        args.Verbs.Add(verb);
    }

    public FormattedMessage GetExamineText(Entity<RMCMedicalExamineComponent> ent)
    {
        var msg = new FormattedMessage();

        if (TryComp<BloodstreamComponent>(ent, out var bloodstream) && bloodstream.BleedAmount > 0)
        {
            msg.AddMarkupOrThrow(Loc.GetString(ent.Comp.BleedText, ("victim", ent.Owner)));
            msg.PushNewline();
        }

        var stateText = ent.Comp.AliveText;

        if (_mobState.IsDead(ent))
            stateText = _unrevivable.IsUnrevivable(ent) ? ent.Comp.UnrevivableText : ent.Comp.DeadText;
        else if (_mobState.IsCritical(ent) || _sizeStun.IsKnockedOut(ent))
            stateText = ent.Comp.CritText;

        msg.AddMarkupOrThrow(Loc.GetString(stateText, ("victim", ent.Owner)));

        return msg;
    }
}
