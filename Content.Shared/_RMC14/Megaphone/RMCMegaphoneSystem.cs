using Content.Shared.Interaction.Events;
using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Megaphone;

public sealed class RMCMegaphoneSystem : EntitySystem
{
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private static readonly EntProtoId<SkillDefinitionComponent> LeadershipSkill = "RMCSkillLeadership";
    private const float Step = 5f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCMegaphoneComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<RMCMegaphoneComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<RMCMegaphoneComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
    }

    private void OnUseInHand(Entity<RMCMegaphoneComponent> ent, ref UseInHandEvent args)
    {
        args.Handled = true;

        var ev = new MegaphoneInputEvent(
            GetNetEntity(args.User),
            VoiceRangeMultiplier: ent.Comp.VoiceRangeMultiplier,
            HushedEffectDuration: ent.Comp.HushedEffectDuration,
            MaxHushedEffectRange: ent.Comp.MaxHushedEffectRange,
            CurrentHushedEffectRange: ent.Comp.CurrentHushedEffectRange);
        _dialog.OpenInput(args.User, Loc.GetString("rmc-megaphone-ui-text"), ev, largeInput: false, characterLimit: 150);
    }

    private void OnExamined(Entity<RMCMegaphoneComponent> ent, ref ExaminedEvent args)
    {
        if (HasComp<XenoComponent>(args.Examiner))
            return;

        args.PushMarkup(Loc.GetString("rmc-megaphone-examine"));

        var radius = MathF.Min(ent.Comp.CurrentHushedEffectRange, ent.Comp.MaxHushedEffectRange);
        if (radius <= 0.1f)
        {
            args.PushMarkup(Loc.GetString("rmc-megaphone-examine-hushed-range-off"));
        }
        else
        {
            args.PushMarkup(Loc.GetString("rmc-megaphone-examine-hushed-range", ("range", (int) radius)));
        }
    }

    private void OnGetVerbs(Entity<RMCMegaphoneComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (HasComp<XenoComponent>(args.User))
            return;

        var user = args.User;

        // Add verbs to set hushed effect range in 5-tile steps up to max hushed range, including 0 (disabled).
        var maxRange = ent.Comp.MaxHushedEffectRange;
        if (maxRange >= Step)
        {
            var options = new List<float>();
            options.Add(0f);
            var steps = (int) MathF.Floor(maxRange / Step);
            for (var i = 1; i <= steps; i++)
            {
                options.Add(i * Step);
            }

            if (options.Count > 0)
            {
                var priority = 0;
                for (var i = options.Count - 1; i >= 0; i--)
                {
                    var value = options[i];
                    var verb = new AlternativeVerb
                    {
                        Category = VerbCategory.PowerLevel,
                        Priority = priority,
                        Text = value <= 0.1f
                            ? Loc.GetString("rmc-megaphone-verb-hushed-range-off")
                            : Loc.GetString("rmc-megaphone-verb-hushed-range", ("range", (int) value)),
                        Message = value <= 0.1f
                            ? Loc.GetString("rmc-megaphone-verb-hushed-range-off-desc")
                            : Loc.GetString("rmc-megaphone-verb-hushed-range-desc", ("range", (int) value)),
                        Act = () =>
                        {
                            // Check leadership skill or squad leader status before changing hush radius.
                            if (!_skills.HasSkill(user, LeadershipSkill, 1) &&
                                !HasComp<SquadLeaderComponent>(user))
                            {
                                var msg = Loc.GetString("rmc-megaphone-no-skill");
                                _popup.PopupClient(msg, user, user, PopupType.SmallCaution);
                                return;
                            }

                            ent.Comp.CurrentHushedEffectRange = value;
                            Dirty(ent, ent.Comp);

                            if (ent.Comp.ToggleSound != null)
                                _audio.PlayPredicted(ent.Comp.ToggleSound, ent, user);
                        }
                    };

                    args.Verbs.Add(verb);
                    priority--;
                }
            }
        }
    }
}

[Serializable, NetSerializable]
public sealed record MegaphoneInputEvent(
    NetEntity Actor,
    string Message = "",
    float VoiceRangeMultiplier = 1.5f,
    TimeSpan HushedEffectDuration = default,
    float MaxHushedEffectRange = 15f,
    float CurrentHushedEffectRange = 15f) : DialogInputEvent(Message);
