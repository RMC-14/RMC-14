using Content.Shared._RMC14.Damage;

namespace Content.Shared._RMC14.Shields;

public sealed class XenoShieldHitEvent
{
    public XenoShield Shield;
    public DamageModifyAfterResistEvent Event;

    public XenoShieldHitEvent(XenoShield shield, DamageModifyAfterResistEvent ev)
    {
        Shield = shield;
        Event = ev;
    }
}
