using Content.Shared._RMC14.Dropship.AttachmentPoint;
using Content.Shared._RMC14.Dropship.Utility;
using Content.Shared._RMC14.Dropship.Weapon;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Dropship.Utility;

public sealed partial class DropshipUtilitySystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedDropshipSystem _dropship = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDropshipWeaponSystem _dropshipWeapon = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DropshipUtilityPointComponent, DropshipTargetChangedEvent>(OnTargetChange);
        SubscribeLocalEvent<DropshipUtilityPointComponent, InteractHandEvent>(OnInteract);
    }

    private void OnTargetChange(Entity<DropshipUtilityPointComponent> ent, ref DropshipTargetChangedEvent args)
    {
        var slot = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.UtilitySlotId);
        var utilityEntity = slot.ContainedEntity;
        if (utilityEntity is null ||
            !TryComp(utilityEntity, out DropshipUtilityComponent? utilityComp))
        {
            return;
        }
        utilityComp.Target = _entityManager.GetEntity(args.DropshipTarget);
    }

    /// <summary>
    /// Pass interaction events to the utility entity stored within the Utility Point
    /// </summary>
    private void OnInteract(Entity<DropshipUtilityPointComponent> ent, ref InteractHandEvent args)
    {
        var slot = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.UtilitySlotId);
        var utilityEntity = slot.ContainedEntity;
        if (utilityEntity is null ||
    !TryComp(utilityEntity, out DropshipUtilityComponent? utilityComp))
        {
            return;
        }

        var ev = new InteractHandEvent(args.User, utilityEntity.Value);
        RaiseLocalEvent(utilityEntity.Value, ev);
        args.Handled = ev.Handled;
    }

    public bool IsActivatable(Entity<DropshipUtilityComponent> ent, EntityUid user, [NotNullWhen(false)] out string? popup)
    {
        if (ent.Comp.Skills is not null &&
            !_skills.HasSkills(user, ent.Comp.Skills))
        {
            popup = Loc.GetString("rmc-dropship-utility-not-skilled");
            return false;
        }

        if (!_dropship.TryGetGridDropship(ent, out var dropship))
        {
            popup = "";
            return false;
        }

        if (_dropshipWeapon.CasDebug)
        {
            popup = null;
            return true;
        }

        if (!TryComp(dropship, out FTLComponent? ftl) ||
                (ftl.State != FTLState.Travelling && ftl.State != FTLState.Arriving))
        {
            popup = Loc.GetString("rmc-dropship-utility-activate-not-flying");
            return false;
        }

        if (!ent.Comp.ActivateInTransport &&
            !HasComp<DropshipInFlyByComponent>(dropship))
        {
            popup = Loc.GetString("rmc-dropship-utility-not-flyby", ("utility", ent.Owner));
            return false;
        }

        if (_timing.CurTime < ent.Comp.NextActivateAt)
        {
            popup = Loc.GetString("rmc-dropship-utility-cooldown", ("utility", ent.Owner));
            return false;
        }

        popup = null;
        return true;
    }

    public void ResetActivationCooldown(Entity<DropshipUtilityComponent> ent)
    {
        ent.Comp.NextActivateAt = _timing.CurTime + ent.Comp.ActivateDelay;
    }
}
