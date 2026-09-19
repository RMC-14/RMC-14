using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Input;

namespace Content.Client._RMC14.UserInterface;

/// <summary>
/// Simple interface to flag that a <see cref="BaseWindow"/> should remain open
/// when <see cref="EngineKeyFunctions.WindowCloseRecent"/> is pressed.
/// </summary>
public interface IIgnoreCloseRecentWindow;
