using Content.Server._RMC14.Chat.Chat;
using Content.Server._RMC14.Deafness;
using Content.Server.Radio;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Chemistry.Disabilities;
using Content.Shared.Chat;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Chemistry.Disabilities;

// TODO RMC14: cm13 deafness also blocks whispers and audible emotes and messages
public sealed class RMCDeafDisabilitySystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedCMChatSystem _rmcChat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadioReceiveAttemptEvent>(OnRadioReceiveAttempt);
        SubscribeLocalEvent<RMCDeafDisabilityComponent, ChatMessageOverrideInVoiceRangeEvent>(OnOverrideInVoiceRange, after: [typeof(DeafnessSystem)]);
    }

    private void OnRadioReceiveAttempt(ref RadioReceiveAttemptEvent args)
    {
        var user = Transform(args.RadioReceiver).ParentUid;
        if (!HasComp<RMCDeafDisabilityComponent>(user))
            return;

        args.Cancelled = true;

        if (_random.Prob(0.2f))
            _rmcChat.ChatMessageToOne(Loc.GetString("rmc-deaf-radio-vibrate"), user, ChatChannel.Radio);
    }

    private void OnOverrideInVoiceRange(Entity<RMCDeafDisabilityComponent> ent, ref ChatMessageOverrideInVoiceRangeEvent args)
    {
        if (args.Channel is ChatChannel.Emotes
            or ChatChannel.Damage
            or ChatChannel.Visual
            or ChatChannel.Notifications
            or ChatChannel.OOC
            or ChatChannel.LOOC)
        {
            return;
        }

        var message = Loc.GetString(ent.Owner != args.Source ? "rmc-deaf-talk-others" : "rmc-deaf-talk-self");
        args.WrappedMessage = message;
        args.Message = message;
    }
}
