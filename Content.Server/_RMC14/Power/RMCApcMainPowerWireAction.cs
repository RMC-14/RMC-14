using Content.Server.Wires;
using Content.Shared._RMC14.Power;
using Content.Shared.Wires;

namespace Content.Server._RMC14.Power;

public sealed partial class RMCApcMainPowerWireAction : BaseWireAction
{
    public override Color Color { get; set; } = Color.Red;
    public override string Name { get; set; } = "wire-name-power";

    [DataField("pulseTimeout")]
    private int _pulseTimeout = 30;

    public override object StatusKey => RMCApcMainPowerWireActionKey.Status;

    public override StatusLightState? GetLightState(Wire wire)
    {
        if (!EntityManager.TryGetComponent(wire.Owner, out RMCApcComponent? apc))
            return StatusLightState.Off;

        if (apc.MainPowerWireCut)
            return StatusLightState.Off;

        return apc.MainPowerWirePulsed ? StatusLightState.BlinkingSlow : StatusLightState.On;
    }

    public override bool Cut(EntityUid user, Wire wire)
    {
        base.Cut(user, wire);
        WiresSystem.TryCancelWireAction(wire.Owner, RMCApcMainPowerWireActionKey.PulseCancel);
        EntityManager.System<RMCPowerSystem>().SetApcMainPowerWireCut((wire.Owner, null), true);
        EntityManager.System<RMCPowerSystem>().SetApcMainPowerWirePulsed((wire.Owner, null), false);
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire)
    {
        base.Mend(user, wire);
        EntityManager.System<RMCPowerSystem>().SetApcMainPowerWireCut((wire.Owner, null), false);
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire)
    {
        base.Pulse(user, wire);
        if (!EntityManager.TryGetComponent(wire.Owner, out RMCApcComponent? apc) ||
            apc.MainPowerWirePulsed)
        {
            return;
        }

        EntityManager.System<RMCPowerSystem>().SetApcMainPowerWirePulsed((wire.Owner, apc), true);
        WiresSystem.StartWireAction(wire.Owner, _pulseTimeout, RMCApcMainPowerWireActionKey.PulseCancel, new TimedWireEvent(AwaitPulseCancel, wire));
    }

    private void AwaitPulseCancel(Wire wire)
    {
        EntityManager.System<RMCPowerSystem>().SetApcMainPowerWirePulsed((wire.Owner, null), false);
    }

}
