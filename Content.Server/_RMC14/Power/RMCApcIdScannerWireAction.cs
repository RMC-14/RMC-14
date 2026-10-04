using Content.Server.Wires;
using Content.Shared._RMC14.Power;
using Content.Shared.Wires;

namespace Content.Server._RMC14.Power;

public sealed partial class RMCApcIdScannerWireAction : BaseWireAction
{
    public override Color Color { get; set; } = Color.Green;
    public override string Name { get; set; } = "wire-name-access";

    [DataField("pulseTimeout")]
    private int _pulseTimeout = 30;

    public override object StatusKey => RMCApcIdScannerWireActionKey.Status;

    public override StatusLightState? GetLightState(Wire wire)
    {
        if (!EntityManager.TryGetComponent(wire.Owner, out RMCApcComponent? apc))
            return StatusLightState.Off;

        if (apc.IdScannerWireCut)
            return StatusLightState.Off;

        return apc.Locked ? StatusLightState.On : StatusLightState.BlinkingSlow;
    }

    public override bool Cut(EntityUid user, Wire wire)
    {
        base.Cut(user, wire);
        WiresSystem.TryCancelWireAction(wire.Owner, RMCApcIdScannerWireActionKey.PulseCancel);
        EntityManager.System<RMCPowerSystem>().SetApcIdScannerWireCut((wire.Owner, null), true);
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire)
    {
        base.Mend(user, wire);
        EntityManager.System<RMCPowerSystem>().SetApcIdScannerWireCut((wire.Owner, null), false);
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire)
    {
        base.Pulse(user, wire);
        EntityManager.System<RMCPowerSystem>().PulseApcIdScanner((wire.Owner, null));
        WiresSystem.StartWireAction(wire.Owner, _pulseTimeout, RMCApcIdScannerWireActionKey.PulseCancel, new TimedWireEvent(AwaitPulseCancel, wire));
    }

    private void AwaitPulseCancel(Wire wire)
    {
        EntityManager.System<RMCPowerSystem>().ResetApcIdScannerPulse((wire.Owner, null));
    }

}
