using Robust.Shared.GameStates;

namespace Content.Shared.Stealth.Components
{
    /// <summary>
    ///     When added to an entity with stealth component, this component will change the visibility
    ///     based on in the entity shoots a weapon.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed partial class StealthOnShootComponent : Component
    {
        /// <summary>
        /// Rate that effects how fast an entity's visibility passively changes.
        /// </summary>
        [DataField]
        public float PassiveVisibilityRate = -0.2f;

        /// <summary>
        /// Rate for gun induced visibility changes.
        /// </summary>
        [DataField]
        public float ShootVisibilityRate = 1.0f;
    }
}