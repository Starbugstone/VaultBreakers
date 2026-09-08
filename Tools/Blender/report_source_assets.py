"""Inspect saved editable sources without rewriting them."""
from pathlib import Path
import json
import bpy
root=Path(__file__).resolve().parents[2]
rows=[]
for path in sorted((root/'ArtSource/Blender').rglob('*.blend')):
    bpy.ops.wm.open_mainfile(filepath=str(path))
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.get('vb_export',False)]
    triangles=0
    for obj in meshes:obj.data.calc_loop_triangles();triangles+=len(obj.data.loop_triangles)
    row={'path':str(path.relative_to(root)).replace('\\','/'),'meshes':len(meshes),'triangles':triangles,'uniqueMaterials':len({m.name for o in meshes for m in o.data.materials if m}),'bones':sum(len(o.data.bones) for o in bpy.context.scene.objects if o.type=='ARMATURE'),'metersPerUnit':bpy.context.scene.unit_settings.scale_length}
    rows.append(row)
(root/'Docs/BLENDER_SOURCE_REPORT.json').write_text(json.dumps({'assets':rows},indent=2)+'\n')
print('Saved source report for',len(rows),'editable assets',flush=True)
