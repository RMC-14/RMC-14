using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Xenonids.Announce;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.HiveLeader;
using Content.Shared.Administration.Logs;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Banish;

public sealed class XenoBanishSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedCMChatSystem _rmcChat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedXenoAnnounceSystem _xenoAnnounce = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoBanishComponent, ComponentStartup>(OnBanishStartup);
    }

    private void OnBanishStartup(Entity<XenoBanishComponent> ent, ref ComponentStartup args)
    {
        // Evolution prevention handled by evolution system
    }

    public bool CanBanish(EntityUid queen, EntityUid target, out string reason)
    {
        reason = string.Empty;

        if (queen == target)
        {
            reason = "rmc-banish-cant-banish-self";
            return false;
        }

        if (HasComp<XenoBanishComponent>(target) && Comp<XenoBanishComponent>(target).Banished)
        {
            reason = "rmc-banish-already-banished";
            return false;
        }

        if (!_hive.FromSameHive(queen, target))
        {
            reason = "rmc-banish-different-hive";
            return false;
        }

        if (_mobState.IsCritical(target) || _mobState.IsDead(target))
        {
            reason = "rmc-banish-cant-banish-crit";
            return false;
        }

        return true;
    }

    public bool CanReadmit(EntityUid queen, EntityUid target, out string reason)
    {
        reason = string.Empty;

        if (!TryComp(target, out XenoBanishComponent? banish) || !banish.Banished)
        {
            reason = "rmc-banish-not-banished";
            return false;
        }

        if (!_hive.FromSameHive(queen, target))
        {
            reason = "rmc-banish-different-hive";
            return false;
        }

        if (_timing.CurTime < banish.ReadmitAvailableAt)
        {
            reason = "rmc-readmit-cooldown";
            return false;
        }

        return true;
    }

    public void BanishXeno(EntityUid queen, EntityUid target, string reason)
    {
        if (_net.IsClient)
            return;

        var banish = EnsureComp<XenoBanishComponent>(target);
        banish.Banished = true;
        banish.BanishedAt = _timing.CurTime;
        banish.BanishReason = reason;
        banish.BanishedBy = Name(queen);
        banish.ReadmitAvailableAt = _timing.CurTime + TimeSpan.FromMinutes(10);
        Dirty(target, banish);

        // Evolution prevention handled by evolution system

        // Announce to hive
        if (_hive.GetHive(queen) is { } hive)
        {
            var announcement = $"By {Name(queen)}'s will, {Name(target)} has been banished from the hive!\n\n{reason}";
            _xenoAnnounce.AnnounceToHive(default, hive, announcement);
        }

        // Message to banished xeno
        if (TryComp(target, out ActorComponent? actor))
        {
            var msg = "The Queen has banished you from the hive! Other xenomorphs may now attack you freely, but your link to the hivemind remains, preventing you from harming other sisters.";
            _rmcChat.ChatMessageToOne(ChatChannel.Local, msg, msg, default, false, actor.PlayerSession.Channel);
            _popup.PopupEntity(msg, target, PopupType.LargeCaution);
        }

        // Remove leader status if banished
        RemCompDeferred<HiveLeaderComponent>(target);

        var ev = new XenoBanishedEvent(target);
        RaiseLocalEvent(target, ref ev);

        // Admin log
        _adminLog.Add(LogType.RMCBanish, $"{ToPrettyString(queen)} banished {ToPrettyString(target)}. Reason: {reason}");

        // Automatic unbanish after 30 minutes is handled by the server system
    }

    public void ReadmitXeno(EntityUid queen, EntityUid target)
    {
        if (_net.IsClient)
            return;

        if (!TryComp(target, out XenoBanishComponent? banish))
            return;

        banish.Banished = false;
        Dirty(target, banish);

        // Evolution restoration is handled by the evolution system checking banish status

        // Announce to hive
        if (_hive.GetHive(queen) is { } hive)
        {
            var announcement = $"{Name(queen)} has readmitted {Name(target)} to the hive.";
            _xenoAnnounce.AnnounceToHive(default, hive, announcement);
        }

        // Message to readmitted xeno
        if (TryComp(target, out ActorComponent? actor))
        {
            var msg = "You have been readmitted to the hive by the Queen.";
            _rmcChat.ChatMessageToOne(ChatChannel.Local, msg, msg, default, false, actor.PlayerSession.Channel);
            _popup.PopupEntity(msg, target, PopupType.Large);
        }

        var ev = new XenoReadmittedEvent(target);
        RaiseLocalEvent(target, ref ev);

        // Admin log
        _adminLog.Add(LogType.RMCBanish, $"{ToPrettyString(queen)} readmitted {ToPrettyString(target)}");
    }

    public void UnbanishXeno(EntityUid target)
    {
        if (!TryComp(target, out XenoBanishComponent? banish))
            return;

        banish.Banished = false;
        Dirty(target, banish);

        // Evolution restoration is handled by the evolution system checking banish status

        // Message to xeno
        if (TryComp(target, out ActorComponent? actor))
        {
            var msg = "Your banishment has expired. You are now readmitted to the hive.";
            _rmcChat.ChatMessageToOne(ChatChannel.Local, msg, msg, default, false, actor.PlayerSession.Channel);
            _popup.PopupEntity(msg, target, PopupType.Large);
        }
    }

    public bool IsBanished(EntityUid xeno)
    {
        return TryComp(xeno, out XenoBanishComponent? banish) && banish.Banished;
    }
}

[ByRefEvent]
public record struct XenoBanishedEvent(EntityUid Xeno);

[ByRefEvent]
public record struct XenoReadmittedEvent(EntityUid Xeno);