using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Weapons.Ranged;

[Serializable, NetSerializable]
public sealed partial class RMCBattleExecuteEvent : SimpleDoAfterEvent
{
    public NetEntity User;
    public NetEntity Target;
    public DamageSpecifier BattleExecuteDamage;

    public RMCBattleExecuteEvent(NetEntity user, NetEntity target, DamageSpecifier battleExecuteDamage)
    {
        User = user;
        Target = target;
        BattleExecuteDamage = battleExecuteDamage;
    }
}
