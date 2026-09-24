using Content.Shared.Examine;
using Content.Shared.Mobs;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Examine.Pose;

public abstract class SharedRMCSetPoseSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCSetPoseComponent, GetVerbsEvent<Verb>>(OnSetPoseGetVerbs);
        SubscribeLocalEvent<RMCSetPoseComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<RMCSetPoseComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnSetPoseGetVerbs(Entity<RMCSetPoseComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract)
            return;

        if (args.User != args.Target)
            return;

        Verb verb = new()
        {
            Text = Loc.GetString("rmc-set-pose-title"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/character.svg.192dpi.png")),
            Priority = -5,
            Act = () => SetPose(ent),
        };

        args.Verbs.Add(verb);
    }

    private void OnExamine(Entity<RMCSetPoseComponent> ent, ref ExaminedEvent args)
    {
        var comp = ent.Comp;

        if (comp.Pose.Trim() == string.Empty)
            return;

        using (args.PushGroup(nameof(RMCSetPoseComponent)))
        {
            var pose = Loc.GetString("rmc-set-pose-examined", ("ent", ent), ("pose", FormattedMessage.EscapeText(comp.Pose)));
            args.PushMarkup(pose, -5);
        }
    }

    private void OnMobStateChanged(Entity<RMCSetPoseComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive)
            return;

        ent.Comp.Pose = string.Empty; // reset the pose on death/crit
        Dirty(ent);
    }

    protected virtual void SetPose(Entity<RMCSetPoseComponent> ent)
    {
    }
}
