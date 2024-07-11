import pathlib

from . import ss13, ss14

MAP_NAME = "Shivas_Snowball"
CWD = pathlib.Path(__file__).parent
SOURCE_PATH = CWD / f"{MAP_NAME}.dmm"  # TODO: load file name from arguments.
TILE_MAPPING_PATH = CWD / "tile_mapping.yml"
TARGET_PATH = CWD / f"{MAP_NAME}.yml"  # TODO: load file name from arguments.

ss13_map = ss13.Map(SOURCE_PATH)
ss14_map = ss14.Map.from_ss13(ss13_map, TILE_MAPPING_PATH)
ss14_map.save(TARGET_PATH)
