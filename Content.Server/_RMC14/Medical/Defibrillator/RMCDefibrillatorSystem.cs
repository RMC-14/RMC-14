using Content.Server.Chat.Managers;
using Content.Server.EUI;
using Content.Server.Ghost;
using Content.Server.PowerCell;
using Content.Shared._RMC14.Chemistry.Effects.Positive;
using Content.Shared._RMC14.Medical.Defibrillator;
using Content.Shared.Chat;
using Content.Shared.Medical;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Rounding;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Medical.Defibrillator;

public sealed class RMCDefibrillatorSystem : SharedRMCDefibrillatorSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    private static readonly SoundSpecifier HeartbeatSound =
        new SoundPathSpecifier("/Audio/_RMC14/Effects/heart_beat_short.ogg", AudioParams.Default.WithVolume(-9f));

    private const string GhostNotifySound = "/Audio/_RMC14/Effects/Admin/adminhelp.ogg";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DefibrillatorComponent, PowerCellChangedEvent>(OnChangeCharge);
        SubscribeLocalEvent<MindContainerComponent, RMCDefibrillatingReviveStartedEvent>(OnDefibrillatingReviveStarted);
    }

    private void OnDefibrillatingReviveStarted(Entity<MindContainerComponent> ent, ref RMCDefibrillatingReviveStartedEvent args)
    {
        if (!_mind.TryGetMind(ent, out _, out var mind, ent.Comp) ||
            !_player.TryGetSessionById(mind.UserId, out var session))
        {
            return;
        }

        if (mind.CurrentEntity == ent.Owner)
        {
            _popup.PopupEntity(Loc.GetString("chem-defibrillating-heart-struggling"), ent, ent);
            _audio.PlayGlobal(HeartbeatSound, session);
            return;
        }

        var message = Loc.GetString("chem-defibrillating-ghost-heart-struggling");
        _chat.ChatMessageToOne(ChatChannel.Server, message, $"[bold]{message}[/bold]", default, false, session.Channel, audioPath: GhostNotifySound);
        _eui.OpenEui(new ReturnToBodyEui(mind, _mind, _player), session);
    }

    private void OnChangeCharge(Entity<DefibrillatorComponent> entity, ref PowerCellChangedEvent args)
    {
        if (!_powerCell.TryGetBatteryFromSlot(entity, out var battery) || !TryComp<PowerCellDrawComponent>(entity, out var draw))
            return;

        var frac = battery.CurrentCharge / battery.MaxCharge;
        var level = (byte)ContentHelpers.RoundToLevels(frac, 1, (int)DefibrillatorChargeVisuals.Full);
        level = (byte)Math.Ceiling(frac * (int)DefibrillatorChargeVisuals.Full);
        if (battery.CurrentCharge < draw.UseRate)
            level = 0;

        if (!TryComp<AppearanceComponent>(entity, out var appearance))
            return;
        _appearance.SetData(entity, DefibrillatorVisuals.DefibrillatorCharge, (DefibrillatorChargeVisuals)level, appearance);
    }
}
