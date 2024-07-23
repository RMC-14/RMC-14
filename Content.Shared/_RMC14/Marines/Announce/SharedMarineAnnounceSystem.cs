using Content.Shared.Popups;
using Content.Shared.Radio;
using Content.Shared.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Marines.Announce;

public abstract class SharedMarineAnnounceSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MarineCommunicationsComputerComponent, ActivatableUIOpenAttemptEvent>(OnMarineCommunicationsComputerOpenAttempt);
    }

    private void OnMarineCommunicationsComputerOpenAttempt(Entity<MarineCommunicationsComputerComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (_timing.CurTime < ent.Comp.LastAnnouncement + ent.Comp.Cooldown)
        {
            // TODO RMC14 localize
            _popup.PopupClient($"Please allow at least {(int) ent.Comp.Cooldown.TotalSeconds} seconds to pass between announcements", args.User);
            args.Cancel();
        }
    }

    public virtual void AnnounceRadio(EntityUid sender, string message, ProtoId<RadioChannelPrototype> channel)
    {
    }

    public virtual void AnnounceARES(EntityUid? source, string message, SoundSpecifier sound)
    {
    }
}
