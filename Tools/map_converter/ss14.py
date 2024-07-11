import base64
import collections
import dataclasses
import pathlib
import typing as t

import numpy as np
import numpy.typing as npt
import yaml
from loguru import logger

from . import ss13


# Types.
# ========================================
@dataclasses.dataclass
class Ss14Tile:
    name: str
    flags: int
    variant: int

    def encode(self, id: int) -> bytearray:
        return (
            bytearray(id.to_bytes(4, byteorder="little"))
            + self.flags.to_bytes(1, byteorder="little")
            + self.variant.to_bytes(1, byteorder="little")
        )


Ss14Chunks = t.Dict[t.Tuple[int, int], t.List[Ss14Tile]]

# Const.
# ========================================
DEFAULT_TILE: Ss14Tile = Ss14Tile(name="Space", flags=0, variant=0)
DEFAULT_TILE_ID = 0
CHUNK_SIZE = 16

CWD = pathlib.Path(__file__).parent
TEMPLATE_PATH = CWD / "template.yml"

# Class.
# ========================================


class Map:
    data: t.Any
    chunks: Ss14Chunks = collections.defaultdict(list)

    def __init__(self, path: pathlib.Path) -> None:
        with open(path, "r") as f:
            self.data = yaml.load(f.read(), Loader=yaml.FullLoader)
        self.data["tilemap"] = {0: "Space"}

    @staticmethod
    def from_ss13(
        ss13_map: ss13.Map, conversion_tile_mapping_path: pathlib.Path
    ) -> "Map":
        # Create map from template.
        map = Map(TEMPLATE_PATH)

        # Load ss13 to ss14 tile mapping.
        tile_conversion_mapping: t.Dict[str, Ss14Tile] = {}
        with open(conversion_tile_mapping_path, "r") as f:
            mapping = yaml.load(f.read(), Loader=yaml.FullLoader)
            for key, value in mapping.items():
                tile_conversion_mapping[key] = Ss14Tile(**value)

            # Prepare SS14 tilemap.
            unique_ss14_tiles = set(
                ss14_tile.name for _, ss14_tile in tile_conversion_mapping.items()
            )
            for idx, tile_name in enumerate(unique_ss14_tiles, 1):
                map.data["tilemap"][idx] = tile_name
        tilemap_reversed = {v: k for k, v in map.data["tilemap"].items()}

        # Create a huge 2d array of encoded ss14 tiles.
        width = len(ss13_map.chunks)
        width = width + (CHUNK_SIZE - width % CHUNK_SIZE)
        height = len(list(ss13_map.chunks.values())[0])
        height = height + (CHUNK_SIZE - height % CHUNK_SIZE)
        cols = int(height / 16)
        rows = int(width / 16)
        raw_map: npt.NDArray[t.Any] = np.full(
            (width, height),
            DEFAULT_TILE,
        )
        mappings_not_found: set[str] = set()
        for ss13_chunk_coords, map_keys in ss13_map.chunks.items():
            for idx, map_key in enumerate(map_keys):
                try:
                    ss14_tile = tile_conversion_mapping[ss13_map.keys[map_key]]
                except KeyError:
                    mappings_not_found.add(ss13_map.keys[map_key])
                    ss14_tile: Ss14Tile = DEFAULT_TILE

                raw_map[
                    ss13_chunk_coords[0] - 1,  # ss13 map coords start with 1, not 0
                    idx,
                ] = ss14_tile
        if mappings_not_found:
            logger.warning("Mappings not found:")
            for mapping in mappings_not_found:
                logger.info(f"{mapping}")

        # Now slice resulting 2d array into chunks.
        sliced_map = np.hsplit(raw_map, cols)
        for x, col in enumerate(sliced_map):
            sliced_map[x] = np.vsplit(col, rows)

        # And put resulting chunks into the map.
        for chunk_x, chunk_col in enumerate(sliced_map):
            for chunk_y, chunk in enumerate(chunk_col):
                chunk_data = bytearray()
                for item in chunk.flatten():
                    chunk_data += item.encode(tilemap_reversed[item.name])
                encoded_chunk_data = base64.b64encode(bytes(chunk_data)).decode("ascii")

                map.data["entities"][0]["entities"][1]["components"][2]["chunks"][
                    f"{chunk_x},{chunk_y}"
                ] = {
                    "ind": f"{chunk_x},{chunk_y}",
                    "tiles": encoded_chunk_data,
                    "version": 6,
                }

        return map

    def save(self, path: pathlib.Path) -> None:
        with open(path, "w") as f:
            yaml.dump(self.data, f)
