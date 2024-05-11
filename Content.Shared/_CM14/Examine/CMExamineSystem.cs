using Content.Shared.Access.Systems;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.HealthExaminable;
using Content.Shared.Verbs;

namespace Content.Shared._CM14.Examine;

public sealed class CMExamineSystem : EntitySystem
{
    [Dependency] private readonly HealthExaminableSystem _healthExaminable = default!;
    [Dependency] private readonly IdExaminableSystem _idExaminable = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TransformComponent, GetVerbsEvent<ExamineVerb>>(OnGetExaminedVerbs, after: [typeof(HealthExaminableSystem), typeof(IdExaminableSystem)]);
        SubscribeLocalEvent<TransformComponent, ExaminedEvent>(OnExamined, after: [typeof(HealthExaminableSystem), typeof(IdExaminableSystem)]);
    }

    private void OnGetExaminedVerbs(Entity<TransformComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        args.Verbs.RemoveWhere(v =>
            v.Text == Loc.GetString("health-examinable-verb-text") ||
            v.Text == Loc.GetString("id-examinable-component-verb-text")
        );
    }

    private void OnExamined(Entity<TransformComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(CMExamineSystem), 1))
        {
            var id = _idExaminable.GetMessage(ent);
            args.PushMarkup(id);
        }

        using (args.PushGroup(nameof(CMExamineSystem), -1))
        {
            if (TryComp(ent, out HealthExaminableComponent? examinable) &&
                TryComp(ent, out DamageableComponent? damageable))
            {
                args.PushMessage(_healthExaminable.CreateMarkup(ent, examinable, damageable));
            }
        }
    }
}
