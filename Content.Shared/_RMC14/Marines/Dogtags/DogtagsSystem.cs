using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Access.Components;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Disposal;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Interaction;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Marines.Dogtags;

public sealed class DogtagsSystem : EntitySystem
{
    [Dependency] private readonly SharedRottingSystem _rotting = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;

    readonly EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillPolice";
    readonly int SkillRequired = 2;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TakeableTagsComponent, InventoryRelayedEvent<GetVerbsEvent<EquipmentVerb>>>(GetRelayedTags);
        SubscribeLocalEvent<TakeableTagsComponent, GetVerbsEvent<EquipmentVerb>>(OnGetVerbTags);
        SubscribeLocalEvent<TakeableTagsComponent, ExaminedEvent>(OnTagsExamine);

        SubscribeLocalEvent<InformationTagsComponent, ExaminedEvent>(OnInfoTagsExamine);
        SubscribeLocalEvent<InformationTagsComponent, AfterInteractEvent>(OnInfoTagsUse);

        SubscribeLocalEvent<RMCMemorialComponent, ExaminedEvent>(OnMemorialExamined);
    }

    private void OnTagsExamine(Entity<TakeableTagsComponent> tags, ref ExaminedEvent args)
    {
        if (HasComp<XenoComponent>(args.Examiner))
            return;

        GetTagInformation(tags, out var name, out var job, out var blood);
        args.PushMarkup(Loc.GetString("rmc-dogtags-read", ("name", name), ("assignment", job), ("bloodtype", blood)));
    }

    private void OnInfoTagsExamine(Entity<InformationTagsComponent> tags, ref ExaminedEvent args)
    {
        if (HasComp<XenoComponent>(args.Examiner))
            return;
        //Priorities to make sure they appear in about the right spots
        args.PushMarkup(Loc.GetString("rmc-dogtags-info-read-start", ("tags", tags.Comp.Tags.Count)), -19);
        int count = 1;
        foreach (var tag in tags.Comp.Tags)
        {
            args.PushMarkup(Loc.GetString("rmc-dogtags-info-read", ("number", count++), ("name", tag.Name), ("assignment", tag.Assignment), ("bloodtype", tag.BloodType)), -19 - count);
        }
    }
    private void OnMemorialExamined(Entity<RMCMemorialComponent> memorial, ref ExaminedEvent args)
    {
        if (HasComp<XenoComponent>(args.Examiner) || memorial.Comp.Names.Count == 0)
            return;

        string text = Loc.GetString("rmc-memorial-start") + " ";
        int count = 1;
        foreach(var name in memorial.Comp.Names)
        {
            if (count == memorial.Comp.Names.Count)
                text += name + ".";
            else
                text += name + ", ";
            count++;
        }

        args.PushMarkup(text, -5);
    }

    private void GetRelayedTags(Entity<TakeableTagsComponent> tags, ref InventoryRelayedEvent<GetVerbsEvent<EquipmentVerb>> args)
    {
        OnGetVerbTags(tags, ref args.Args);
    }

    private void OnGetVerbTags(Entity<TakeableTagsComponent> tags, ref GetVerbsEvent<EquipmentVerb> args)
    {
        if (!args.CanInteract || args.Hands == null || tags.Comp.TagsTaken || HasComp<XenoComponent>(args.User))
            return;

        var wearer = Transform(tags).ParentUid;
        var user = args.User;

        if (user == wearer)
            return;

        if (!_rotting.IsRotten(wearer) && !_skills.HasSkill(user, Skill, SkillRequired) || !_mob.IsDead(wearer))
            return;

        var verb = new EquipmentVerb()
        {
            Icon = new SpriteSpecifier.Texture(new("/Textures/_RMC14/Interface/VerbIcons/dogtag.png")),
            Text = Loc.GetString("rmc-dogtags-take"),
        };

        verb.Act = () => TakeTags(tags, user, wearer);

        args.Verbs.Add(verb);
    }

    private void TakeTags(Entity<TakeableTagsComponent> tags, EntityUid user, EntityUid wearer)
    {
        if (user == wearer)
            return;

        if (!_rotting.IsRotten(wearer) && !_skills.HasSkill(user, Skill, SkillRequired))
            return;

        if (tags.Comp.TagsTaken)
        {
            _popup.PopupClient(Loc.GetString("rmc-dogtags-already-taken", ("target", wearer)), user);
            return;
        }

        if (!_mob.IsDead(wearer))
        {
            _popup.PopupClient(Loc.GetString("rmc-dogtags-still-alive"), user);
            return;
        }

        tags.Comp.TagsTaken = true;
        _appearance.SetData(tags, DogtagVisuals.Taken, true);

        if (_net.IsClient)
            return;

        var tag = SpawnNextToOrDrop(tags.Comp.InfoTag, wearer);

        var comp = EnsureComp<InformationTagsComponent>(tag);
        GetTagInformation(tags, out var name, out var job, out var blood);
        InfoTagInfo tagInfo = new InfoTagInfo()
        {
            Name = name,
            Assignment = job,
            BloodType = blood
        };
        comp.Tags.Add(tagInfo);
        _hands.TryPickupAnyHand(user, tag);
    }

    private void OnInfoTagsUse(Entity<InformationTagsComponent> tags, ref AfterInteractEvent args)
    {
        if (args.Target == null)
            return;

        if (TryComp<InformationTagsComponent>(args.Target, out var targTags))
        {
            args.Handled = true;
            _meta.SetEntityName(args.Target.Value, Loc.GetString("rmc-dogtags-info-joined-name"));
            _meta.SetEntityDescription(args.Target.Value, Loc.GetString("rmc-dogtags-info-joined-desc"));

            if (_net.IsClient)
                return;

            var tagsJoinedString = tags.Comp.Tags.Count == 1 && targTags.Tags.Count == 1 ? "rmc-dogtags-single-join" : "rmc-dogtags-join";

            _popup.PopupEntity(Loc.GetString(tagsJoinedString), args.User, args.User);

            targTags.Tags.AddRange(tags.Comp.Tags);
            QueueDel(tags);
        }
        else if (TryComp<RMCMemorialComponent>(args.Target, out var memorial))
        {
            args.Handled = true;

            if (_net.IsClient)
                return;

            _popup.PopupEntity(Loc.GetString("rmc-memorial-add", ("tags", tags), ("slab", args.Target.Value)), args.User, args.User);

            foreach(var tag in tags.Comp.Tags)
            {
                memorial.Names.Add(tag.Name);
                //TODO RMC-14 Ghosts?
            }
            QueueDel(tags);
        }
    }


    private void GetTagInformation(EntityUid dogtag, out string name, out string job, out string bloodtype)
    {
        name = Loc.GetString("rmc-dogtags-unknown");
        job = Loc.GetString("rmc-dogtags-unknown");
        //TODO RMC-14 actual bloodtypes
        bloodtype = "O-";

        if (!TryComp<IdCardComponent>(dogtag, out var id))
            return;

        if(id.FullName != null)
            name = id.FullName;

        if(id.LocalizedJobTitle != null)
            job = id.LocalizedJobTitle;
    }
}
