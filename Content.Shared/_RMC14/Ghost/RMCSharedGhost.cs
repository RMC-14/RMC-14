using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Ghost
{
    /// <summary>
    /// An individual place a ghost can warp to.
    /// This is used as part of <see cref="RMCGhostWarpsResponseEvent"/>
    /// </summary>
    [Serializable, NetSerializable]
    public struct RMCGhostWarp
    {
        public RMCGhostWarp(NetEntity entity, string displayName, string area, bool isWarpPoint, string? categoryName, string? warpColor)
        {
            Entity = entity;
            DisplayName = displayName;
            Area = area;
            IsWarpPoint = isWarpPoint;
            CategoryName = categoryName ?? "Other";
            WarpColor = warpColor ?? "#D3D3D3"; // light grey
        }

        /// <summary>
        /// The entity representing the warp point.
        /// </summary>
        public NetEntity Entity { get; }

        /// <summary>
        /// The display name to be surfaced in the ghost warps menu.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Whether this warp represents a warp point or a player.
        /// </summary>
        public bool IsWarpPoint { get; }

        /// <summary>
        /// The name of the category to which the warp belongs to, for mobs it's just the department.
        /// </summary>
        public string CategoryName { get; }

        /// <summary>
        /// The color of each button in the category, should match the color of the department or a squad.
        /// </summary>
        public string WarpColor { get; }

        /// <summary>
        /// The area the player or warp is in.
        /// </summary>
        public string Area { get; }
    }

    /// <summary>
    /// A server to client response for a <see cref="GhostWarpsRequestEvent"/>.
    /// Contains players, and locations a ghost can warp to.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class RMCGhostWarpsResponseEvent : EntityEventArgs
    {
        public RMCGhostWarpsResponseEvent(List<RMCGhostWarp> rmcWarps)
        {
            RMCWarps = rmcWarps;
        }

        /// <summary>
        /// A list of warp points.
        /// </summary>
        public List<RMCGhostWarp> RMCWarps { get; }
    }

}
