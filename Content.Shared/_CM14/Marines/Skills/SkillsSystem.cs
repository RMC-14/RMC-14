namespace Content.Shared._CM14.Marines.Skills;

public sealed class SkillsSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<MedicallyUnskilledDoAfterComponent, AttemptHyposprayUseEvent>(OnAttemptHyposprayUse);
    }

    private void OnAttemptHyposprayUse(Entity<MedicallyUnskilledDoAfterComponent> ent, ref AttemptHyposprayUseEvent args)
    {
        if (!TryComp(args.User, out SkillsComponent? skills) ||
            skills.Skills.Medical < ent.Comp.Min)
        {
            args.MaxDoAfter(ent.Comp.DoAfter);
        }
    }

    public TimeSpan GetDelay(EntityUid user, EntityUid tool)
    {
        if (!TryComp(tool, out MedicallyUnskilledDoAfterComponent? doAfter) ||
            doAfter.Min <= 0)
        {
            return default;
        }

        if (!TryComp(user, out SkillsComponent? skills) ||
            skills.Skills.Medical < doAfter.Min)
        {
            return doAfter.DoAfter;
        }

        return default;
    }

    public bool HasSkills(Entity<SkillsComponent?> ent, in Skills required)
    {
        if (HasComp<BypassSkillChecksComponent>(ent))
            return true;

        // TODO CM14 turn these into prototypes
        Resolve(ent, ref ent.Comp, false);
        var skills = ent.Comp?.Skills ?? default;
        return skills.Antagonist >= required.Antagonist &&
               skills.Construction >= required.Construction &&
               skills.Cqc >= required.Cqc &&
               skills.Domestics >= required.Domestics &&
               skills.Endurance >= required.Endurance &&
               skills.Engineer >= required.Engineer &&
               skills.Execution >= required.Execution &&
               skills.Firearms >= required.Firearms &&
               skills.Fireman >= required.Fireman &&
               skills.Intel >= required.Intel &&
               skills.Jtac >= required.Jtac &&
               skills.Leadership >= required.Leadership &&
               skills.Medical >= required.Medical &&
               skills.MeleeWeapons >= required.MeleeWeapons &&
               skills.Navigations >= required.Navigations &&
               skills.Overwatch >= required.Overwatch &&
               skills.Pilot >= required.Pilot &&
               skills.Police >= required.Police &&
               skills.PowerLoader >= required.PowerLoader &&
               skills.Research >= required.Research &&
               skills.Smartgun >= required.Smartgun &&
               skills.SpecialistWeapons >= required.SpecialistWeapons &&
               skills.Surgery >= required.Surgery &&
               skills.Vehicles >= required.Vehicles;
    }
}
