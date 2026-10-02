"""Original Dock 9 loot: editable shaped cases, fractured variants and five gem silhouettes."""
from pathlib import Path
import bpy, math, json
from mathutils import Vector
from mathutils import Euler

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'Assets/Vaultbreakers/Art/Loot'
SOURCE = ROOT/'ArtSource/Blender/Props/Dock9_Loot.blend'
OUT.mkdir(parents=True, exist_ok=True)
SOURCE.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
bpy.context.scene.unit_settings.system = 'METRIC'
palette = {
    'Ink': (.035,.055,.07,1), 'Steel': (.27,.36,.40,1), 'Crate': (.72,.22,.045,1),
    'Chest': (.045,.30,.32,1), 'Brass': (.83,.55,.12,1), 'Indicator': (1,.77,.20,1),
    'Melee': (1,.22,.06,1), 'Ranged': (.04,.62,1,1), 'Shield': (.54,1,.85,1),
    'Relic': (.65,.17,1,1), 'Gold': (1,.68,.055,1)
}
materials = {}
for name, color in palette.items():
    mat = bpy.data.materials.new('Loot_'+name); mat.diffuse_color=color; mat.use_nodes=True
    bs=mat.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=color
    bs.inputs['Metallic'].default_value=.5; bs.inputs['Roughness'].default_value=.31
    if name in ['Indicator','Melee','Ranged','Shield','Relic','Gold']:
        bs.inputs['Emission Color'].default_value=color; bs.inputs['Emission Strength'].default_value=.45
    materials[name]=mat

def box(name, c, size, mat, bevel=.035, rotation=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=c)
    obj=bpy.context.object; obj.name=name; obj.dimensions=size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.rotation_euler.z=rotation; obj.data.materials.append(materials[mat])
    if bevel:
        mod=obj.modifiers.new('Machined edges','BEVEL'); mod.width=bevel; mod.segments=2
        bpy.ops.object.modifier_apply(modifier=mod.name)
        mod=obj.modifiers.new('Weighted normals','WEIGHTED_NORMAL');mod.keep_sharp=True
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return obj

assets=[]
def export(name, objects, pieces=False):
    copies=[]
    for original in objects:
        copy=original.copy(); copy.data=original.data.copy(); bpy.context.collection.objects.link(copy); copies.append(copy)
    merged=[];groups={}
    for obj in copies:groups.setdefault(obj['debris_piece'] if pieces else obj.data.materials[0].name,[]).append(obj)
    for key in sorted(groups):
        group=groups[key]
        bpy.ops.object.select_all(action='DESELECT')
        for obj in group:obj.select_set(True)
        bpy.context.view_layer.objects.active=group[0]
        bpy.ops.object.join(); joined=bpy.context.object
        if pieces:
            joined.name=key
            bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY',center='BOUNDS')
        merged.append(joined)
    bpy.ops.object.select_all(action='DESELECT')
    for o in merged: o.select_set(True)
    bpy.context.view_layer.objects.active=merged[0]
    # Intact meshes combine by material; each multi-material debris chunk retains its own pivot.
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,
        apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',
        use_space_transform=True,bake_space_transform=False,add_leaf_bones=False,bake_anim=False,
        object_types={'MESH'},mesh_smooth_type='FACE',use_mesh_modifiers=True)
    for obj in merged:
        mesh=obj.data;bpy.data.objects.remove(obj,do_unlink=True);bpy.data.meshes.remove(mesh)
    assets.append((name,objects))

def fractured_panel(name, width, depth, thickness, mat):
    outline=[(-width*.5,-depth*.5),(width*.33,-depth*.5),(width*.5,-depth*.28),
             (width*.22,-depth*.10),(width*.45,depth*.12),(width*.26,depth*.5),(-width*.5,depth*.5)]
    count=len(outline)
    vertices=[(x,y,z) for z in [-thickness*.5,thickness*.5] for x,y in outline]
    faces=[tuple(reversed(range(count))),tuple(range(count,count*2))]
    faces += [(i,(i+1)%count,(i+1)%count+count,i+count) for i in range(count)]
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.materials.append(materials[mat]);mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
    bevel=obj.modifiers.new('Fracture edges','BEVEL');bevel.width=.015;bevel.segments=1
    bpy.context.view_layer.objects.active=obj;bpy.ops.object.modifier_apply(modifier=bevel.name)
    return obj

def debris_chunk(objects, name, location, angles):
    rotation=Euler(angles).to_matrix()
    for obj in objects:
        obj['debris_piece']=name
        obj.location=Vector(location)+rotation@obj.location
        obj.rotation_euler=(rotation@obj.rotation_euler.to_matrix()).to_euler()
    return objects

for chest in [False,True]:
    name='Loot_Chest' if chest else 'Loot_Crate'; color='Chest' if chest else 'Crate'
    # Both silhouettes cross the caster's 1.36 m firing plane, including its projectile radius.
    width=1.8 if chest else 1.2; depth=.95; height=1.4 if chest else 1.3
    parts=[box(name+'_base',(0,0,.09),(width,depth,.18),'Ink'),
           box(name+'_shell',(0,0,height*.5),(width-.10,depth-.09,height-.15),color,.08),
           box(name+'_lid',(0,0,height),(width+.05,depth+.03,.20),'Steel',.075)]
    for x in [-1,1]:
        for y in [-1,1]: parts.append(box(name+'_corner',(x*(width/2-.04),y*(depth/2-.04),height*.5),(.13,.13,height),'Brass'))
        parts.append(box(name+'_strap',(x*width*.29,0,height+.11),(.11,depth,.065),'Brass',.015))
    for y in [-1,1]:
        parts.append(box(name+'_recess',(0,y*depth*.505,height*.55),(width*.66,.05,.32),'Ink',.015))
        parts.append(box(name+'_latch',(0,y*depth*.55,height*.68),(.2,.08,.2),'Brass',.02))
        for x in [-.26,.26]: parts.append(box(name+'_light',(x,y*depth*.54,height*.53),(.18,.025,.04),'Indicator',.009))
    # Raised diamond on the lid is the common breakable/loot marker, visible from the game camera.
    parts.append(box(name+'_gemmark',(0,0,height+.13),(.25,.25,.04),'Indicator',.012,math.pi/4))
    if chest:
        for x in [-.32,.32]:parts.append(box(name+'_lidbevel',(x,0,height+.06),(.22,.68,.16),'Chest',.055))
    export(name,parts)
    debris=[]
    # Recognizable split lids, jagged wall plates and bent corner rails, rather than small cubes.
    for i,side in enumerate([-1,1]):
        lid=[fractured_panel(name+'_lidtear'+str(i),width*.52,depth*.82,.11,'Steel'),
             box(name+'_lidpaint'+str(i),(-width*.07,0,.075),(width*.23,depth*.62,.055),color,.02),
             box(name+'_lidstrap'+str(i),(-width*.17,0,.11),(.075,depth*.82,.065),'Brass',.012)]
        if i==0:lid.append(box(name+'_brokenmarker',(.02,-.08,.085),(.17,.21,.04),'Indicator',.01,.55))
        debris+=debris_chunk(lid,'Debris_0'+str(i)+'_Lid',(side*width*.38,.27,.24),(.12*side,.20*side,.38*side))
        wall=[fractured_panel(name+'_walltear'+str(i),width*.58,height*.48,.075,color),
              box(name+'_wallrim'+str(i),(-width*.24,0,.065),(.075,height*.48,.065),'Brass',.009),
              box(name+'_wallrecess'+str(i),(-.035,-.045,.055),(width*.24,.11,.035),'Ink',.009),
              box(name+'_walllatch'+str(i),(-.025,.055,.09),(.14,.12,.075),'Brass',.012)]
        debris+=debris_chunk(wall,'Debris_0'+str(i+2)+'_Panel',(side*width*.24,-.49,.21),(.25*side,-.18,.7*side))
        frame=[box(name+'_rail'+str(i),(0,0,0),(.13,height*.51,.12),'Steel',.022),
               box(name+'_railfoot'+str(i),(.13,-height*.22,.03),(.37,.14,.13),'Brass',.018),
               box(name+'_railtrim'+str(i),(0,.04,.085),(.075,height*.38,.05),'Brass',.008)]
        debris+=debris_chunk(frame,'Debris_0'+str(i+4)+'_Frame',(side*(width*.5+.15),-.10,.19),(.18,.32*side,-.7*side))
    export(name+'_Broken',debris,pieces=True)

for name,sides,scale in [('Melee',4,(.17,.13,.28)),('Ranged',6,(.18,.18,.24)),
                         ('Shield',6,(.24,.12,.16)),('Relic',8,(.20,.20,.20)),('Gold',5,(.22,.18,.18))]:
    vertices=[(0,0,scale[2])]
    for i in range(sides):
        a=math.tau*i/sides;vertices.append((math.cos(a)*scale[0],math.sin(a)*scale[1],0))
    vertices.append((0,0,-scale[2]*.6));faces=[]
    for i in range(sides):
        a=1+i;b=1+(i+1)%sides;faces.extend([(0,a,b),(sides+1,b,a)])
    mesh=bpy.data.meshes.new('Gem_'+name);mesh.from_pydata(vertices,[],faces);mesh.materials.append(materials[name]);mesh.update()
    obj=bpy.data.objects.new('Gem_'+name,mesh);bpy.context.collection.objects.link(obj)
    export('Gem_'+name,[obj])

# Arrange the real source meshes for a neutral inspection render, after exporting at the origin.
for i,(name,objects) in enumerate(assets):
    pos=Vector(((-2.5+i*1.7) if i<4 else (-2.3+(i-4)*1.15),0 if i<4 else -2.2,0 if i<4 else .3))
    for obj in objects:obj.location+=pos
scene=bpy.context.scene
box('Preview floor',(0,0,-.14),(14,10,.2),'Ink',0)
bpy.ops.object.camera_add(location=(6,-10,8));camera=bpy.context.object
camera.rotation_euler=(Vector((.2,-.4,.3))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO';camera.data.ortho_scale=10;scene.camera=camera
for name,loc,energy,size in [('Key',(1,-4,7),1800,7),('Fill',(-5,-1,4),1000,5)]:
    bpy.ops.object.light_add(type='AREA',location=loc);light=bpy.context.object;light.name=name
    light.data.energy=energy;light.data.shape='DISK';light.data.size=size
    light.rotation_euler=(Vector((0,0,0))-light.location).to_track_quat('-Z','Y').to_euler()
scene.world.color=(.18,.18,.18)
scene.render.engine='CYCLES';scene.cycles.samples=24
scene.render.resolution_x=1400;scene.render.resolution_y=900;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.filepath=str(ROOT/'Docs/Images/Alpha001_Loot_Source.png')
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE))
bpy.ops.render.render(write_still=True)
report=[]
for name,objects in assets:
    report.append({'asset':name,'meshes':len(objects),'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in objects)})
(ROOT/'Docs/Validation/Alpha001_Loot_Source.json').write_text(json.dumps(report,indent=2)+'\n')
print('Loot source, nine FBX exports and source review render complete.',flush=True)
