# GMod LuaDev

[LuaDev](https://github.com/noiwex/luadev) integration for VS Code.

Send code to LuaDev with simple shortcuts!

## Installation

To install *GMod LuaDev*, do the following steps:

1. Open Visual Studio Code
2. Open the Quick Open Palette (By default: `Ctrl-P`)
3. Type `ext install gmod-lua`
4. Select the GMod LuaDev extension
5. Select *Install*

## Usage

Use the Command Palette (By default: `Ctrl-Shift-P`) to select a GMod LuaDev
command to run.

You can
[add custom keybindings](https://code.visualstudio.com/docs/customization/keybindings#_customizing-shortcuts)
to trigger these commands by shortcut instead.

For example, to trigger `Send to Server` when `Ctrl-1` is pressed, use the
following keybinding shortcut:

``` json
{
	"key": "ctrl+1",
	"command": "gmod-luadev.server",
	"when": "editorTextFocus"
}
```

## Commands

- `gmod-luadev.server`
	- Sends the current file to run on the server.
- `gmod-luadev.shared`
	- Sends the current file to run on the server and all clients.
- `gmod-luadev.clients`
	- Sends the current file to run on all clients.
- `gmod-luadev.self`
	- Sends the current file to only run on your client.
- `gmod-luadev.client`
	- Sends the current file to run on a specific client.

## Configuration

- `gmod-luadev.port` (Default: `27099`)
	- The port number to communicate to LuaDev with.
- `gmod-luadev.hidescriptname` (Default: `false`)
	- If enabled, GMod LuaDev will send files with a blank filename, hiding
	  the real filename.

## Other

[GMod LuaDev](https://github.com/lixquid/vscode-gmod-luadev) is hosted at
GitHub.

GMod LuaDev is licensed under the GPLv3.
