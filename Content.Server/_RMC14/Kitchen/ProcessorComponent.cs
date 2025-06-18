using System.ComponentModel;
using Content.Server._RMC14.EntitySystems;
using Content.Shared._RMC14.Kitchen;
using Robust.Shared.Audio;

namespace Content.Server._RMC14.Kitchen
{
    /// <summary>
    /// If this works, don't ask me how I did it
    /// as frankly, I don't know how I did it either
    /// Probably will get help with this
    /// but for now, let's see what I can do
    /// </summary>
    [Access(typeof(ProcessorSystem)), RegisterComponent]
    public sealed partial class ProcessorComponent : Robust.Shared.GameObjects.Component
    {
        [DataField]
        public int StorageMaxEntities = 6;

        [DataField]
        public TimeSpan WorkTime = TimeSpan.FromSeconds(3.5); // Roughly matches the processing sounds.

        [DataField]
        public float WorkTimeMultiplier = 1;

        [DataField]
        public SoundSpecifier ClickSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

        [DataField]
        public SoundSpecifier ProcessSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/blender.ogg");

        public EntityUid? AudioStream;


        public Container Storage = default!;
    }

    [Access(typeof(ProcessorSystem)), RegisterComponent]
    public sealed partial class ActiveProcessorComponent : Robust.Shared.GameObjects.Component
    {
        /// <summary>
        /// Remaining time until the processor finishes processing.
        /// </summary>
        [ViewVariables]
        public TimeSpan EndTime;

        [ViewVariables]
        public ProcessorProgram Program;
    }
}
