using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.HealthExaminable;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Examine;

public sealed class CMExamineSystem : EntitySystem
{
    [Dependency] private readonly SkillsSystem _skillsSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly HealthExaminableSystem _healthExaminable = default!;
    [Dependency] private readonly IdExaminableSystem _idExaminable = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCGenericExamineComponent, ExaminedEvent>(OnGenericExamined);
        SubscribeLocalEvent<RMCGenericExamineComponent, GetVerbsEvent<ExamineVerb>>(OnGenericExamineVerb, after: [typeof(HealthExaminableSystem), typeof(IdExaminableSystem)]);

        SubscribeLocalEvent<ShortExamineComponent, GetVerbsEvent<ExamineVerb>>(OnGetExaminedVerbs, after: [typeof(HealthExaminableSystem), typeof(IdExaminableSystem)]);

        SubscribeLocalEvent<IdExaminableComponent, ExaminedEvent>(OnIdExamined);

        SubscribeLocalEvent<HealthExaminableComponent, ExaminedEvent>(OnHealthExamined);
    }

    private void OnGenericExamined(Entity<RMCGenericExamineComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.DisplayMode != RMCExamineDisplayMode.Direct)
            return;

        var user = args.Examiner;

        if (ent.Comp.SkillsRequired is { } skillsRequired && !_skillsSystem.HasSkills(user, skillsRequired))
            return;

        if (!_entityWhitelist.CheckBoth(user, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return;

        using (args.PushGroup(nameof(CMExamineSystem), ent.Comp.ExaminePriority))
        {
            args.PushMarkup(Loc.GetString(ent.Comp.Message));
        }
    }

    private void OnGenericExamineVerb(Entity<RMCGenericExamineComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (ent.Comp.DisplayMode != RMCExamineDisplayMode.DetailedVerb)
            return;

        if (ent.Comp.DetailedVerbConfig == null)
            return;

        var user = args.User;

        if (ent.Comp.SkillsRequired is { } skillsRequired && !_skillsSystem.HasSkills(user, skillsRequired))
            return;

        if (!_entityWhitelist.CheckBoth(user, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return;

        var message = FormattedMessage.FromMarkupOrThrow(ent.Comp.Message);
        var verbText = Loc.GetString(ent.Comp.DetailedVerbConfig.Title);
        var hoverMessage = Loc.GetString(ent.Comp.DetailedVerbConfig.HoverMessageId);

        _examine.AddDetailedExamineVerb(args, ent.Comp, message, verbText, ent.Comp.DetailedVerbConfig.VerbIcon, hoverMessage);
    }

    private void OnGetExaminedVerbs(Entity<ShortExamineComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        args.Verbs.RemoveWhere(v =>
            v.Text == Loc.GetString("health-examinable-verb-text") ||
            v.Text == Loc.GetString("id-examinable-component-verb-text")
        );
    }

    private void OnIdExamined(Entity<IdExaminableComponent> ent, ref ExaminedEvent args)
    {
        if (HasComp<BlockIdExamineComponent>(args.Examiner))
            return;

        using (args.PushGroup(nameof(CMExamineSystem), 1))
        {
            if (_idExaminable.GetInfo(ent) is { } info)
                args.PushMarkup(info);
        }
    }

    private void OnHealthExamined(Entity<HealthExaminableComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(CMExamineSystem), -1))
        {
            if (TryComp(ent, out DamageableComponent? damageable))
            {
                args.PushMessage(_healthExaminable.CreateMarkup(ent, ent.Comp, damageable));
            }
        }
    }

    public bool CanExamine(Entity<BlockExamineComponent?> block, EntityUid user)
    {
        if (!Resolve(block, ref block.Comp, false))
            return true;

        return !_entityWhitelist.IsWhitelistPass(block.Comp.Whitelist, user);
    }
}
