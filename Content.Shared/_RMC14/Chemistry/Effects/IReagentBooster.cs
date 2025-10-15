using Content.Shared.EntityEffects;

namespace Content.Shared._RMC14.Chemistry.Effects;

public interface IReagentBooster
{
    float CalculateBoost(EntityEffectReagentArgs args);

    bool BoostSelf => false;
}
