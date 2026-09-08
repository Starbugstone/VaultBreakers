from pathlib import Path
import sys,bpy
sys.path.insert(0,str(Path(__file__).resolve().parent))
import dungeon_assets as art
bpy.ops.wm.open_mainfile(filepath=str(art.ROOT/'ArtSource/Blender/Environments/Dock9_Dungeon.blend'))
art.export_environment()
