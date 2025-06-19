using Content.Shared.DeviceLinking;
using Content.Shared.Item;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._RMC14.Kitchen.Components
{
    [RegisterComponent]
    public sealed partial class ProcessorComponent : Component
    {
        [DataField("cookTimeMultiplier"), ViewVariables(VVAccess.ReadWrite)]
        public float CookTimeMultiplier = 1;

        [DataField("baseHeatMultiplier"), ViewVariables(VVAccess.ReadWrite)]
        public float BaseHeatMultiplier = 100;

        [DataField("objectHeatMultiplier"), ViewVariables(VVAccess.ReadWrite)]
        public float ObjectHeatMultiplier = 100;

        [DataField("failureResult", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string BadRecipeEntityId = "FoodBadRecipe";

        #region  audio
        [DataField("clickSound")]
        public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

        [DataField("ItemBreakSound")]
        public SoundSpecifier ItemBreakSound = new SoundPathSpecifier("/Audio/Effects/clang.ogg");

        public EntityUid? PlayingStream;

        [DataField("loopingSound")]
        public SoundSpecifier LoopingSound = new SoundPathSpecifier("/Audio/Machines/blender.ogg");
        #endregion

        [ViewVariables]
        public bool Broken;

        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public ProtoId<SinkPortPrototype> OnPort = "On";

        /// <summary>
        /// This is a fixed offset of 5.
        /// The cook times for all recipes should be divisible by 5,with a minimum of 1 second.
        /// For right now, I don't think any recipe cook time should be greater than 60 seconds.
        /// </summary>
        [DataField("currentCookTimerTime"), ViewVariables(VVAccess.ReadWrite)]
        public uint CurrentCookTimerTime = 0;

        /// <summary>
        /// Tracks the elapsed time of the current cook timer.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan CurrentCookTimeEnd = TimeSpan.Zero;

        /// <summary>
        /// The maximum number of seconds a processor can be set to.
        /// This is currently only used for validation and the client does not check this.
        /// </summary>
        [DataField("maxCookTime"), ViewVariables(VVAccess.ReadWrite)]
        public uint MaxCookTime = 30;

        /// <summary>
        ///     The max temperature that this processor can heat objects to.
        /// </summary>
        [DataField("temperatureUpperThreshold")]
        public float TemperatureUpperThreshold = 373.15f;

        public int CurrentCookTimeButtonIndex;

        public Container Storage = default!;

        [DataField]
        public string ContainerId = "processor_entity_container";

        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public int Capacity = 10;

        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public ProtoId<ItemSizePrototype> MaxItemSize = "Normal";

        /// <summary>
        /// How frequently the processor can malfunction.
        /// </summary>
        [DataField]
        public float MalfunctionInterval = 1.0f;

        /// <summary>
        /// Chance of an explosion occurring when we processor a metallic object
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public float ExplosionChance = .1f;

        /// <summary>
        /// Chance of lightning occurring when we processor a metallic object
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public float LightningChance = .75f;

        /// <summary>
        /// If this processor can give ids accesses without exploding
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public bool CanProcessorIdsSafely = true;
    }

    public sealed class BeingProcessorEvent : HandledEntityEventArgs
    {
        public EntityUid Processor;
        public EntityUid? User;

        public BeingProcessorEvent(EntityUid processor, EntityUid? user)
        {
            Processor = processor;
            User = user;
        }
    }
}
