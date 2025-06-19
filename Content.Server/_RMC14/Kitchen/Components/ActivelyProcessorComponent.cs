namespace Content.Server._RMC14.Kitchen.Components
{
    /// <summary>
    /// Attached to an object that's actively being processord
    /// </summary>
    [RegisterComponent]
    public sealed partial class ActivelyProcessingComponent : Component
    {
        /// <summary>
        /// The processor this entity is actively being processord by.
        /// </summary>
        [DataField]
        public EntityUid? Processor;
    }
}
