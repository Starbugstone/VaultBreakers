from pathlib import Path
import sys
sys.path.insert(0,str(Path(__file__).resolve().parent))
import bpy
import stylized_assets as art
bpy.ops.wm.open_mainfile(filepath=str(art.base.SOURCE_PATH))
rig=bpy.data.objects['VB_Rig'];art.animate(rig);rig.animation_data_create();rig.animation_data.action=bpy.data.actions.get('Idle')
print('Actions:',[(a.name,len(a.slots)) for a in bpy.data.actions],flush=True)
art.export(art.ROOT/'Assets/Vaultbreakers/Art/Animation/Vaultbreaker_Animations.fbx',True,True)

bpy.ops.wm.save_as_mainfile(filepath=str(art.base.SOURCE_PATH))
bpy.context.scene.frame_set(1)
art.render(art.base.PREVIEW_PATH)
art.render(art.ROOT/'Docs/Images/Vaultbreaker_Back_Preview.png',True)
