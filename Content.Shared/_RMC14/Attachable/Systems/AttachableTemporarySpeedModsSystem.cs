using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared._RMC14.Movement;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed class AttachableTemporarySpeedModsSystem : EntitySystem
{
    [Dependency] private readonly TemporarySpeedModifiersSystem _temporarySpeedModifiersSystem = default!;
    [Dependency] private readonly AttachableHolderSystem _attachableHolderSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableTemporarySpeedModsComponent, AttachableAlteredEvent>(OnAttachableAltered);
    }

    private void OnAttachableAltered(Entity<AttachableTemporarySpeedModsComponent> attachable, ref AttachableAlteredEvent args)
    {
        if ((attachable.Comp.Alteration & args.Alteration) != attachable.Comp.Alteration || !_attachableHolderSystem.TryGetUser(attachable.Owner, out var userUid))
            return;

        _temporarySpeedModifiersSystem.ModifySpeed(userUid.Value, attachable.Comp.Modifiers);
    }
}
