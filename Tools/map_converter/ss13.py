import enum
import pathlib
import typing as t

# {"puZ": "/turf/snow/tile/snow", ...}
Keys = t.Dict[str, str]

# {(0, 0, 0): ["puZ", "puZ" ...], ...}
Chunks = t.Dict[t.Tuple[int, int, int], t.List[str]]


@enum.unique
class BufferType(enum.Enum):
    UNDEFINED = 0
    TILE_DEFINITION = 1
    MAP_CHUNK = 2


class Map:
    keys: Keys = {}
    chunks: Chunks = {}

    def __init__(self, path: pathlib.Path) -> None:
        buffer = ""
        buffer_type = BufferType.UNDEFINED

        definition_id = None
        turf = None

        for line in open(path, "r"):
            line = line.strip()

            # Detect which kind of buffer are we filling if not set yet.
            if buffer_type == BufferType.UNDEFINED:
                if line.startswith('"'):
                    buffer_type = BufferType.TILE_DEFINITION
                elif line.startswith("("):
                    buffer_type = BufferType.MAP_CHUNK

            # Tile definitions.
            if buffer_type == BufferType.TILE_DEFINITION:
                # Process every line.

                # Definition id line.
                if line.startswith('"'):
                    definition_id = line.replace(" = (", "").strip('"')

                if line.startswith("/turf/"):
                    turf = line.strip(",{}")

                # This is the last line of the definition.
                if line.endswith(")") and "=" not in line:
                    if not definition_id or not turf:
                        raise ValueError("Invalid tile definition.")

                    self.keys[definition_id] = turf

                    buffer_type = BufferType.UNDEFINED
                    definition_id = None
                    turf = None

            # Map chunks.
            if buffer_type == BufferType.MAP_CHUNK:
                if not line.endswith('"}'):
                    # Fill the buffer until we reach terminator.
                    buffer += f" {line}"
                else:
                    # Process buffer.
                    buffer += f" {line}"

                    coords_str, content = buffer.split("=", 1)
                    coords = t.cast(
                        t.Tuple[int, int, int],
                        tuple(
                            int(coord) for coord in coords_str.strip(" ()").split(",")
                        ),
                    )

                    tile_definition_ids = content.strip(' "{}').split(" ")

                    self.chunks[coords] = tile_definition_ids

                    buffer_type = BufferType.UNDEFINED
                    buffer = ""
