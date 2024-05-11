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
            skills.Medical < ent.Comp.Min)
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
            skills.Medical < doAfter.Min)
        {
            return doAfter.DoAfter;
        }

        return default;
    }
}
