using Content.Shared._RMC14.Kitchen;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._RMC14.Kitchen.Components
{
    /// <summary>
    /// Attached to a processor that is currently in the process of cooking
    /// </summary>
    [RegisterComponent, AutoGenerateComponentPause]
    public sealed partial class ActiveProcessorComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public float CookTimeRemaining;

        [ViewVariables(VVAccess.ReadWrite)]
        public float TotalTime;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
        [AutoPausedField]
        public TimeSpan MalfunctionTime = TimeSpan.Zero;

        [ViewVariables]
        public (ProcessorRecipePrototype?, int) PortionedRecipe;
    }
}
