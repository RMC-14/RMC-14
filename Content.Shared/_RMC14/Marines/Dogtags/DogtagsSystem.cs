using Content.Shared._RMC14.Medical.Defibrillator;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Access.Components;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared.Coordinates;
using Content.Shared._RMC14.Medical.Unrevivable;

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
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly RMCUnrevivableSystem _unrevivableSystem = default!;

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

        string text = Loc.GetString("rmc-memorial-start") + " " + MemorialNamesFormat(memorial.Comp.Names);

        args.PushMarkup(text, -5);
    }

    public string MemorialNamesFormat(List<string> memorialnames)
    {
        string list = "";
        int count = 1;
        foreach (var name in memorialnames)
        {
            if (count == memorialnames.Count)
                list += name + ".";
            else
                list += name + ", ";
            count++;
        }
        return list;
    }

    private void GetRelayedTags(Entity<TakeableTagsComponent> tags, ref InventoryRelayedEvent<GetVerbsEvent<EquipmentVerb>> args)
    {
        OnGetVerbTags(tags, ref args.Args);
    }

    private bool CanTakeTags(Entity<TakeableTagsComponent> tags, EntityUid wearer, EntityUid taker, out bool equipped, out string reason)
    {
        equipped = true;
        reason = "";

        if (wearer == taker)
        {
            if (_hands.IsHolding(taker, tags))
            {
                if (TryComp<IdCardOwnerComponent>(tags, out var cardOwner) && Exists(cardOwner.Id))
                {
                    if(cardOwner.Id == taker)
                        reason = Loc.GetString("rmc-dogtags-still-exists-self");
                    else
                        reason = Loc.GetString("rmc-dogtags-still-exists");
                    return false;
                }
                equipped = false;
                return true;
            }

            return false;
        }

        if (!_mob.IsDead(wearer))
        {
            reason = Loc.GetString("rmc-dogtags-still-alive");
            return false;
        }

        if (_rotting.IsRotten(wearer) ||
            _unrevivableSystem.IsUnrevivable(wearer) ||
            HasComp<RMCDefibrillatorBlockedComponent>(wearer) ||
            _skills.HasSkill(taker, Skill, SkillRequired))
        {
            return true;
        }

        reason = Loc.GetString("rmc-dogtags-can-be-saved");
        return false;
    }

    private void OnGetVerbTags(Entity<TakeableTagsComponent> tags, ref GetVerbsEvent<EquipmentVerb> args)
    {
        if (!args.CanInteract ||
            !args.CanAccess ||
            args.Hands == null ||
            tags.Comp.TagsTaken ||
            HasComp<XenoComponent>(args.User))
            return;

        var wearer = Transform(tags).ParentUid;
        var user = args.User;

        if (!CanTakeTags(tags, wearer, user, out var equipped, out _))
            return;

        var verb = new EquipmentVerb()
        {
            Icon = new SpriteSpecifier.Texture(new("/Textures/_RMC14/Interface/VerbIcons/dogtag.png")),
            Text = Loc.GetString("rmc-dogtags-take")
        };

        verb.Act = () => TakeTags(tags, user, wearer);

        args.Verbs.Add(verb);
    }

    private void TakeTags(Entity<TakeableTagsComponent> tags, EntityUid user, EntityUid wearer)
    {
        if (tags.Comp.TagsTaken)
        {
            _popup.PopupClient(Loc.GetString("rmc-dogtags-already-taken", ("target", wearer)), user);
            return;
        }

        if (!CanTakeTags(tags, wearer, user, out var equipped, out var reason))
        {
            _popup.PopupClient(reason, user);
            return;
        }

        if (!_interaction.InRangeAndAccessible(user, wearer))
            return;

        tags.Comp.TagsTaken = true;
        _appearance.SetData(tags, DogtagVisuals.Taken, true);

        if (_net.IsClient)
            return;

        if (!equipped)
        {
            var prop = SpawnAtPosition(tags.Comp.FallenTag, user.ToCoordinates());
            if (TryComp<IdCardComponent>(tags, out var id))
            {
                CopyComp(tags.Owner, prop, tags.Comp);
                CopyComp(tags.Owner, prop, id);
            }
            QueueDel(tags);
        }

        var tag = SpawnNextToOrDrop(tags.Comp.InfoTag, wearer);
        Dirty(tags);

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
            _appearance.SetData(args.Target.Value, InfoTagVisuals.Number, Math.Min(targTags.Tags.Count, targTags.MaxDisplayTags));
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
