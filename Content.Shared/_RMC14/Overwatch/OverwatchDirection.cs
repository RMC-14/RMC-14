using System;

namespace Content.Shared._RMC14.Overwatch
{
    /// <summary>
    /// Direction enum for adjusting the overwatch camera eye offset.
    /// </summary>
    [Serializable]
    public enum OverwatchDirection
    {
        Reset = 0, // Used for resetting eye offset and zoom back to default
        North = 1,
        South = 2,
        East  = 3,
        West  = 4
    }
}
