using Content.Server.Chat.Systems;
using Content.Server.Interaction;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.Radio;
using Content.Server.Radio.Components;
using Content.Server.Radio.EntitySystems;
using Content.Server.Speech.Components;
using Content.Shared._RMC14.Radio;
using Content.Shared.Power;
using Content.Shared.Radio.Components;
using Robust.Server.GameObjects;

namespace Content.Server._RMC14.Radio;

public sealed class RMCIntercomSystem : EntitySystem
{
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly RadioDeviceSystem _radioDevice = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const float DirectRange = 1.5f;
    private const float DistanceEpsilon = 0.0001f;

    private readonly HashSet<Entity<RMCIntercomComponent>> _intercoms = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntitySpokeEvent>(OnEntitySpoke);
        SubscribeLocalEvent<RadioReceiveAttemptEvent>(OnRadioReceiveAttempt);
        SubscribeLocalEvent<RMCIntercomComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RMCIntercomComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnEntitySpoke(EntitySpokeEvent args)
    {
        if (args.Channel?.ID != RMCIntercomConstants.Channel.Id)
            return;

        var intercom = FindIntercom(args.Source);
        if (intercom == null ||
            !TryComp<IntercomComponent>(intercom.Value, out var intercomComp) ||
            intercomComp.CurrentChannel is not { } channel)
        {
            _popup.PopupEntity(Loc.GetString("rmc-intercom-no-device"), args.Source, args.Source);
            return;
        }

        _radio.SendRadioMessage(args.Source, args.Message, channel, intercom.Value, args.Language);
    }

    private void OnMapInit(Entity<RMCIntercomComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<IntercomComponent>(ent, out var intercom))
            return;

        var channel = intercom.CurrentChannel;
        if (channel == null || !intercom.SupportedChannels.Contains(channel.Value))
        {
            channel = intercom.SupportedChannels.Count > 0
                ? intercom.SupportedChannels[0]
                : default;
        }

        _radioDevice.SetIntercomChannel((ent.Owner, intercom), channel);
        ApplyPowerState((ent.Owner, ent.Comp, intercom), this.IsPowered(ent, EntityManager));
    }

    private void OnPowerChanged(Entity<RMCIntercomComponent> ent, ref PowerChangedEvent args)
    {
        if (!TryComp<IntercomComponent>(ent, out var intercom))
            return;

        ApplyPowerState((ent.Owner, ent.Comp, intercom), args.Powered);
    }

    private void OnRadioReceiveAttempt(ref RadioReceiveAttemptEvent args)
    {
        if (!CanReceiveIntercomRadio(args.RadioSource, args.RadioReceiver))
            args.Cancelled = true;
    }

    /// <summary>
    /// Checks CM13 intercom routing. Ordinary radio sources are not restricted by this system.
    /// </summary>
    public bool CanReceiveIntercomRadio(EntityUid radioSource, EntityUid radioReceiver)
    {
        if (!HasComp<RMCIntercomComponent>(radioSource))
            return true;

        return HasComp<RMCIntercomComponent>(radioReceiver) &&
               Transform(radioSource).MapID == Transform(radioReceiver).MapID;
    }

    private void ApplyPowerState(Entity<RMCIntercomComponent, IntercomComponent> ent, bool powered)
    {
        if (!ent.Comp2.RequiresPower)
            powered = true;

        _radioDevice.SetMicrophoneEnabled(ent, null, powered && ent.Comp2.MicrophoneEnabled, true);
        _radioDevice.SetSpeakerEnabled(ent, null, powered && ent.Comp2.SpeakerEnabled, true);
    }

    /// <summary>
    /// Finds the single adjacent intercom that direct intercom speech should use.
    /// </summary>
    public EntityUid? FindIntercom(EntityUid source)
    {
        var sourceCoordinates = _transform.GetMapCoordinates(source);
        var closestDistance = float.MaxValue;
        EntityUid? closest = null;

        _intercoms.Clear();
        _lookup.GetEntitiesInRange(sourceCoordinates, DirectRange, _intercoms);

        foreach (var intercom in _intercoms)
        {
            if (!TryComp<IntercomComponent>(intercom, out var intercomComp) ||
                intercomComp.CurrentChannel == null ||
                !intercomComp.SupportedChannels.Contains(intercomComp.CurrentChannel.Value) ||
                intercomComp.RequiresPower && !this.IsPowered(intercom, EntityManager) ||
                HasComp<BlockListeningComponent>(intercom) ||
                !_interaction.InRangeUnobstructed(source, intercom.Owner, DirectRange))
            {
                continue;
            }

            var intercomCoordinates = _transform.GetMapCoordinates(intercom);
            if (intercomCoordinates.MapId != sourceCoordinates.MapId)
                continue;

            var distance = (intercomCoordinates.Position - sourceCoordinates.Position).LengthSquared();
            var isCloser = distance < closestDistance - DistanceEpsilon;
            var isSameDistance = MathF.Abs(distance - closestDistance) <= DistanceEpsilon;
            var hasLowerUid = closest == null || intercom.Owner.Id < closest.Value.Id;
            if (!isCloser && (!isSameDistance || !hasLowerUid))
                continue;

            closestDistance = distance;
            closest = intercom;
        }

        return closest;
    }
}
