using Content.Shared.Interaction.Events;
using Content.Shared._RMC14.Dialog;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Megaphone;

public sealed class RMCMegaphoneSystem : EntitySystem
{
    [Dependency] private readonly DialogSystem _dialog = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCMegaphoneComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(Entity<RMCMegaphoneComponent> ent, ref UseInHandEvent args)
    {
        args.Handled = true;

        var ev = new MegaphoneInputEvent(GetNetEntity(args.User));
        _dialog.OpenInput(args.User, "Enter a message for the megaphone:", ev, largeInput: false, characterLimit: 100);
    }
}

[Serializable, NetSerializable]
public sealed record MegaphoneInputEvent(NetEntity Actor, string Message = "") : DialogInputEvent(Message);
