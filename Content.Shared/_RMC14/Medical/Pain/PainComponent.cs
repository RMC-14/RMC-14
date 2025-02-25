using Robust.Shared.GameStates;
using Content.Shared.FixedPoint;
using Content.Shared.EntityEffects;

namespace Content.Shared._RMC14.Medical.Pain
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class PainComponent : Component
    {
        /// <summary>
        /// Sum of pain increacing factors at the moment
        /// </summary>
        [ViewVariables]
        public FixedPoint2 CurrentPain = FixedPoint2.Zero;

        [ViewVariables]
        public FixedPoint2 CurrentPainPercentage = FixedPoint2.Zero;

        [ViewVariables]
        public int CurrentPainLevel = 0;
        [DataField]
        public TimeSpan PainLevelUpdateRate = TimeSpan.FromSeconds(2);

        [ViewVariables]
        public TimeSpan LastPainLevelUpdateTime = new(0);

        [DataField]
        public TimeSpan EffectUpdateRate = TimeSpan.FromSeconds(1);

        [ViewVariables]
        public TimeSpan NextEffectUpdateTime = new(0);

        [ViewVariables, Access(typeof(PainSystem))]
        public List<PainReductionModificator> PainReductionModificators = [];

        [DataField]
        public FixedPoint2 PainReductionDecreaceRate = FixedPoint2.New(0.25);

        [DataField]
        public FixedPoint2 BrutePainMultiplier = FixedPoint2.New(1);

        [DataField]
        public FixedPoint2 BurnPainMultiplier = FixedPoint2.New(1.2);

        [DataField]
        public FixedPoint2 ToxinPainMultiplier = FixedPoint2.New(1.5);

        [DataField]
        public FixedPoint2 AirlossPainMultiplier = FixedPoint2.Zero;

        [DataField(required: true, serverOnly: true)]
        public List<EntityEffect> PainLevels = new(0);
    }

    [DataDefinition]
    public sealed partial class PainReductionModificator
    {
        public TimeSpan Duration;
        public FixedPoint2 EffectStrength;
    }
}
