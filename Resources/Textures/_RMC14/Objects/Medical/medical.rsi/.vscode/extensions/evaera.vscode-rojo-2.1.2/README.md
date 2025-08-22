# Rojo for VS Code

Integrates [Rojo](https://github.com/rojo-rbx/rojo) natively with VS Code.

![Rojo menu](https://i.eryn.io/2222/chrome-DdVyGHdh.png)

All actions are performed via the Rojo menu, as seen above. To open the Rojo menu, either:

- Open the Command Pallette (`Ctrl` + `Shift` + `P`) and type "Rojo: Open menu"
- Use the Rojo button in the bottom right corner:

![Rojo button](https://i.eryn.io/2222/dHvsUY6w.png)

> Note: The Rojo button only appears if a folder in your workspace contains a `*.project.json` file.

## Automatic installation

If you do not have Rojo installed, the extension will ask you if you want it to be automatically installed for you. If you do, it will be installed via [Aftman](https://github.com/LPGhatguy/aftman), a toolchain manager. This will create an `aftman.toml` file in your project directory, which will pin the current version of Rojo in your project.

You must click "Install Roblox Studio plugin" at least once if you want to live-sync from Studio!

### Aftman bin on Non-Windows platforms

On macOS and Linux, after aftman is installed, you need to add the Aftman bin to your user PATH: `~/.aftman/bin`. On windows, this is done automatically, but otherwise you must do it yourself for the extension to work properly.

- [How to add a folder to my PATH?](https://gist.github.com/nex3/c395b2f8fd4b02068be37c961301caa7#mac-os-x)

## System rojo

This extension uses the `rojo.exe` from your system path. If you already installed Rojo manually to use it from the command line, or with another toolchain manager, this extension will use that version of Rojo automatically. However, we recommend upgrading to Aftman-managed Rojo for the best experience.

### Foreman

If you are already using Foreman to manage your system Rojo, we recommend switching to Aftman. Aftman is a more robust, spiritual successor to Foreman, created by the same original author. (Who is also the creator of Rojo! 🙂)

If you want to learn more, see the [Differences from Foreman](https://github.com/LPGhatguy/aftman#differences-from-foreman) section of the Aftman README.

> **_Warning_: Stopping Rojo does not work with Foreman**
>
> All currently-released versions of Foreman (as of v1.0.4) have a bug that makes killing the launched Rojo process leave a Rojo process running forever. There is [an open issue on the Foreman repo](https://github.com/Roblox/foreman/issues/45) for this problem, but for now you must either not use Rojo managed with Foreman, or kill the process yourself. Aftman does not have this problem.

### Migrating from a globally-installed or Foreman-managed Rojo

When you open the extension for the first time, if you are not using Aftman, a message will appear in the bottom-right. If you click "Switch to Aftman" on that message, your old `rojo.exe` will automatically be removed from your system PATH, and you will see another prompt that will install Aftman and Rojo for you.

## Coming from V1 of this extension

The release of V2 of this extension has changed how it works drastically, both internally and how you interact with it. However, we hope that these changes do not negatively affect your workflow.

- All sub-commands have been removed in favor of a single menu.
- The extension no longer uses its own bespoke installation of Rojo. It shares whatever version of Rojo you have installed on your system.
- When the extension installs Rojo for you, it is installed in a way so that you can use it from the command line.
- Versions of Rojo before Rojo 6 are no longer supported.
- The extension will not automatically update Rojo for you. When Rojo is installed via Aftman, you can change your installed Rojo version by editing the `aftman.toml` file in your project.

If something about this new extension breaks your workflow, please tell us about it!

- Join the [Roblox Open Source Discord Server](https://discord.gg/wH5ncNS)
- [Open an issue](https://github.com/rojo-rbx/vscode-rojo/issues) on the project repo

You can always temporarily revert to V1 by [clicking the cog on the extension page and choosing "Install another version"](https://i.eryn.io/2222/2q0w1H3I.png).

## Help me!

- Read the [Rojo docs](https://rojo.space/docs/v7/)
- Join the [Roblox Open Source Discord Server](https://discord.gg/wH5ncNS)

## Supported platforms

- Windows
- macOS
- Linux

## License

Rojo for VS Code is available under the terms of The Mozilla Public License Version 2. See [LICENSE](https://github.com/rojo-rbx/vscode-rojo/blob/HEAD/LICENSE) for details.
