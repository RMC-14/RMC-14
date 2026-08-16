# Embedded mapper MCP server

An embedded [Model Context Protocol](https://modelcontextprotocol.io/) server that lets AI agents
(Claude Code, or any MCP client) inspect and edit maps on a running game server: tiles, entities,
decals, RMC areas, map lifecycle and save/load — the same operations a human mapper performs in
the mapping editor, exposed as 38 typed tools.

The design follows Anthropic's
[tool-design guidance for agents](https://www.anthropic.com/engineering/writing-tools-for-agents):
consolidated workflow tools instead of thin API wrappers, token-efficient matrix output,
search-over-list catalogues, actionable error messages and per-tool behavior annotations.

## Quick start

1. Run the game server (debug builds have the server compiled in by default; see *Gating*).
2. Register the endpoint with your MCP client, e.g. for Claude Code:

   ```
   claude mcp add mapper --transport http http://localhost:1212/mcp
   ```

   The port is the status-host port (`net.port`, default 1212).
3. Connect a game client, then drive the tools: `player_status` → `look_around` →
   `set_tiles` / `spawn_entity` / `paint_areas` → `save_map`.

No client-side code is involved: even the player's screen rotation is reconstructed server-side
from the replicated `InputMoverComponent` state.

**Tip: launch the agent from the repository root.** The server only gives the agent hands; the
repo gives it everything else — reference maps and prototype YAML to imitate and validate
against, `save_map export_path` straight into the working tree for git-diffable, publishable
maps, and auto-discovered project configuration (a committed `.mcp.json`, agent skills,
`CLAUDE.md`) that only activates when the session runs inside the project folder.

A companion skill — a mapper's handbook distilled for AI agents (world model, golden rules,
recipes, pre-save checklist) — is maintained separately at
[rmc14-mapping-skill](https://github.com/IlyaBokovenko/rmc14-mapping-skill). The server is fully
usable without it; the skill is optional plain-Markdown documentation for any MCP-capable agent.

## Gating and security

- Compiled only `#if !FULL_RELEASE || RMC_MCP`; a release build needs
  `dotnet build -p:EnableMcp=true`.
- CVars: `rmc.mcp.enabled` (default true where compiled in), `rmc.mcp.token`.
- Access: loopback connections are always allowed; remote connections require
  `Authorization: Bearer <token>` matching `rmc.mcp.token` (empty token = loopback only).
- Commands executed through `run_command` bypass permission checks (the endpoint is trusted);
  the transport gate above is the security boundary.

## Architecture

```
McpManager        JSON-RPC 2.0 over stateless streamable HTTP (POST /mcp on IStatusHost),
                  initialize / ping / tools/list / tools/call, batching supported.
McpContext        Shared DI + helpers: main-thread marshalling (RunOnMainThread),
                  argument parsing, grid/tile/player resolution, component resolution.
McpEntityMatrix   Entity-matrix rendering: per-tile priority tiers, stacked/subfloor layers.
Tools/*.cs        One class per tool, grouped by domain (map read/write, entities, decals,
                  lifecycle, prototypes, console).
```

Tool handlers run on a status-host worker thread and marshal all game-state access to the main
thread via `TaskCompletionSource` (same pattern as `ServerApi`). Agent-facing failures are thrown
as `McpToolException` and returned as `isError` tool results with a suggested next step.

## Tool conventions

- **Coordinates**: tile indices per grid; +X = east, +Y = north. Rectangle tools take the
  SOUTH-WEST corner in absolute form (`x`, `y`) or the CENTER in player-relative form
  (`relative: {dx, dy, frame: "world"|"screen"}`); `frame: "screen"` follows the player's
  current camera rotation. `grid` defaults to the grid under the player; maps are addressed
  by numeric `map_id`.
- **Matrices**: bulk reads return character matrices with a legend (world-aligned, top row =
  north; `look_around` is screen-aligned instead). Legend overflow is reported explicitly
  rather than truncated silently. `set_tiles` / `paint_areas` accept the same matrix format
  for writes (first row = northmost).
- **Previews**: destructive bulk tools (`set_tiles`, `replace_tiles`, `delete_entities`) accept
  `dry_run: true` and report what would change — `set_tiles` includes a histogram of tiles that
  would be overwritten.
- **Annotations**: every tool declares MCP behavior hints (`readOnlyHint`, `destructiveHint`,
  `idempotentHint`, `openWorldHint`) so clients can build permission policies on them.
- **Searches** (`find_entities`, `list_*_prototypes`, `find_tiles`) take substrings and limits and
  report `total_matches` / `truncated`. Entity name matching also covers prototype ids, so
  English terms work against localized entity names.

## Tool catalogue

| Group | Tools |
| --- | --- |
| Player | `player_status`, `look_around`, `teleport_player` |
| Map read | `list_maps`, `list_grids`, `read_tiles`, `find_tiles`, `read_areas` |
| Entities read | `read_entities`, `find_entities`, `entity_info` |
| Prototypes | `list_entity_prototypes`, `list_tile_prototypes`, `list_decal_prototypes` |
| Map write | `set_tiles`, `replace_tiles`, `paint_areas` |
| Entities write | `spawn_entity`, `delete_entities`, `transform_entity`, `set_component_field` |
| Decals | `read_decals`, `add_decal`, `edit_decal`, `remove_decal` |
| Lifecycle | `create_map`, `create_grid`, `delete_map`, `pause_map`, `map_init`, `save_map`, `save_grid`, `load_map`, `load_grid`, `set_ambient_light`, `mapping_session` |
| Console | `run_command` (server, output captured, Toolshed included), `client_command` (remote client execution, fire-and-forget) |

Highlights beyond raw console commands:

- `mapping_session` — the full human `mapping` setup (paused map, aghost, autosave, editor UI)
  as one call.
- `paint_areas` — per-grid RMC area assignment that works on uninitialized (mapping) maps,
  replacing the area-marker + global `areas:save` workflow.
- `set_component_field` — validated component field writes with field-name suggestions and a
  read-back echo, replacing silent `vvwrite` for the common cases (ladder pair ids, trigger
  teleporter vectors, camera ids).
- `save_map` / `save_grid` — refuse initialized maps (unless forced), preserve yaml uids for
  stable git diffs, and can export the yml to an absolute path (e.g. into the repo).

## Safety rails

- Maps intended for saving must stay uninitialized and paused; `create_map` / `load_map` /
  `mapping_session` produce that state, `map_init` warns it is irreversible and `save_map`
  refuses initialized maps without `force`.
- `delete_entities` never deletes grids, maps or player-controlled entities.
- An empty created grid is garbage-collected by the engine — `create_grid` warns to place tiles
  immediately.

## Testing

Smoke-test any tool with plain JSON-RPC:

```sh
curl -s -X POST http://localhost:1212/mcp -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"list_maps","arguments":{}}}'
```

`EVALS.md` contains ten end-to-end evaluation tasks (per Anthropic's mcp-builder methodology)
plus scriptable regression checks; run them against a dev server with a connected client.
