# Mapper MCP evaluation tasks

Realistic end-to-end tasks for judging whether an agent can use this server effectively
(methodology: Anthropic's mcp-builder skill, phase 4). Run against a dev server + a connected
client, each task with a fresh agent, in two configurations:

- **Baseline**: ONLY the MCP server connected (no mapping skill). Measures how self-sufficient
  the server's tool descriptions and instructions are.
- **Skill**: MCP server + the `mapping-rmc14` agent skill loaded. The score delta vs baseline
  measures what the skill actually contributes (per Anthropic's evaluation-driven skill
  authoring) — tasks that only pass with the skill justify its content; content that never
  changes an outcome is dead weight.

A task passes when the agent reaches the goal without human hints and without breaking the
golden invariant (the edited map stays uninitialized and paused).

Score sheet per task and configuration: solved (y/n), tool calls used, wrong-tool detours,
unrecoverable errors.

1. **Orientation.** Player stands somewhere on a loaded map with a rotated camera. "Describe what
   is in the room I'm looking at, in my screen terms." Expect: player_status → look_around, correct
   left/right/up answers derived from screen_up_points; stacked/subfloor consulted before claiming
   a tile is empty.

2. **Build a room.** "Build a 5x7 plated room with a steel floor, walls, and a south-facing airlock
   two tiles east of me." Expect: set_tiles matrix (correct SW-corner anchoring — verify via the
   echoed south_west_corner), spawn_entity batch for walls, prototype search for the airlock,
   look_around verification.

3. **Dry-run guard.** "Fill a 30x30 rectangle with FloorSteel at (X,Y) — but first show me what
   would be overwritten." Expect: set_tiles dry_run=true, a reading of would_overwrite, and no
   mutation until confirmed.

4. **Ladder pair.** "Link these two ladders so they teleport between decks." Expect:
   entity_info (component dump of Ladder) → set_component_field Ladder.Id on both entities with the
   same unique id; NO vvwrite. The agent must state that pairs link at map-init only.

5. **Areas round-trip.** "This new room should belong to the brig area." Expect: read_areas to see
   the current assignment, list_entity_prototypes search for the area prototype, paint_areas
   rectangle, read_areas verification. No areas:save / marker spawning.

6. **Area audit.** "List every area on this ship and find unassigned playable tiles near me."
   Expect: read_areas format=summary for the whole grid, then a windowed matrix read around the
   player; '.' cells interpreted as unassigned.

7. **Targeted demolition.** "Remove all the catwalks in this 10x10 area but leave everything else."
   Expect: delete_entities with a prototype/name filter (dry_run first is a bonus), NOT an
   unfiltered area wipe.

8. **Decal striping.** "Paint a warning stripe line along the north edge of this room." Expect:
   list_decal_prototypes search, a single batched add_decal call, correct rotations.

9. **Recovery from the lobby.** Player is in the lobby (no attached entity). "Show me what's
   around me." Expect: the tool error's recovery hint is followed (observe → aghost via
   run_command in the player's context) instead of giving up or hallucinating.

10. **Save discipline.** "We're done, save this to the repo as mymap.yml. Also I want to quickly
    playtest it." Expect: save_map with export_path FIRST, playtest on a load_map copy + map_init
    of the copy only, delete_map of the copy afterwards; never map_init on the edited map.

Regression checks (cheap, scriptable via curl JSON-RPC):
- tools/list: every tool carries annotations; read_* / list_* / find_* are readOnlyHint=true.
- find_entities with map_id + with legacy "map" both filter.
- set_tiles matrix echo: north_row_y == y + rows - 1.
- set_component_field: unknown field returns the similar-fields hint; read-only property is
  rejected; value round-trips in new_value.
- paint_areas: matrix '.' clears, read_areas summary reflects the change, works pre-mapinit.
