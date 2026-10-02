"""Original Dock9 dungeon art: tailored character meshes and a dressed three-room ruin.
Uses schema-1 rig, variants and sockets. Run with pinned Blender --background --python.
"""
import sys, math, random
from pathlib import Path
import bpy
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
import generate_modular_vaultbreaker as b
import stylized_assets as a
import polished_dock9 as polished
ROOT=Path(__file__).resolve().parents[2]
PALETTE={
 'BlackGlass':(.025,.07,.105),'VaultTile':(.12,.23,.28),'CoreWhite':(.68,.82,.85),'Skin':(.91,.56,.32),'Hair':(.18,.075,.035),'Cloth':(.035,.28,.48),'Cape':(.62,.17,.065),
 'Leather':(.17,.075,.035),'Gold':(.76,.43,.12),'Steel':(.48,.59,.65),'Ink':(.015,.025,.038),
 'Eye':(.92,.98,1),'Goblin':(.38,.43,.43),'Hood':(.13,.24,.34),'Stone':(.17,.27,.30),
 'StoneLight':(.39,.46,.46),'Tile':(.19,.25,.25),'TileLight':(.28,.34,.33),'Earth':(.095,.16,.15),
 'Moss':(.21,.38,.16),'Leaf':(.095,.31,.20),'LeafLight':(.24,.49,.22),'Bark':(.18,.12,.085),
 'Rune':(.10,.85,.91),'Flame':(1,.52,.10),'Crystal':(.45,.20,.80),'Petal':(.81,.31,.60),
}
def mats():
 return {k:b.make_material('DG_'+k,v,.65 if k in ['Gold','Steel','BlackGlass'] else .18 if k in ['Stone','StoneLight','VaultTile'] else 0,.3 if k in ['Steel','BlackGlass'] else .65,v if k in ['Rune','Flame','Crystal'] else None,2 if k in ['Rune','Flame','Crystal'] else 0) for k,v in PALETTE.items()}
def player():
 b.reset_scene();m=mats();rig=b.create_armature()
 def p(n,c,s,mat,bone,slot=None,var=None,default=True):return a.plate(('VAR_'+slot+'_'+var+'_' if slot else 'BASE_')+n,c,s,m[mat],bone,rig,slot,var,default)
 def sh(n,rings,mat,bone,slot=None,var=None,default=True):return a.shell(('VAR_'+slot+'_'+var+'_' if slot else 'BASE_')+n,rings,m[mat],bone,rig,slot,var,default)
 sh('Tunic',[(.9,.21,.15,0,0),(1.15,.20,.16,0,0),(1.48,.26,.17,0,0),(1.55,.15,.11,0,0)],'Cloth','Chest')
 p('Belt',(0,-.01,.98),(.47,.34,.09),'Leather','Hips');p('Buckle',(0,-.19,.98),(.12,.035,.09),'Gold','Hips')
 p('Neck',(0,0,1.58),(.18,.18,.14),'Skin','Neck')
 sh('Face',[(1.57,.17,.13,0,-.045),(1.65,.25,.20,0,-.015),(1.91,.26,.20,0,0),(1.99,.20,.16,0,0)],'Skin','Head')
 # A sculpted sweep of hair, eyebrows, inset eyes and nose give the hero an identity.
 sh('Hair',[(1.87,.265,.21,0,.055),(2.03,.29,.235,0,.02),(2.10,.20,.18,-.04,.025)],'Hair','Head')
 p('Fringe',(-.095,-.205,1.96),(.31,.10,.18),'Hair','Head')
 for sign in [-1,1]:
  p('EyeWhite'+str(sign),(.108*sign,-.208,1.82),(.105,.025,.075),'Eye','Head')
  p('Pupil'+str(sign),(.10*sign,-.226,1.82),(.044,.013,.064),'Ink','Head')
  p('Brow'+str(sign),(.11*sign,-.23,1.89),(.12,.028,.028),'Hair','Head')
 p('Nose',(0,-.223,1.75),(.074,.072,.085),'Skin','Head');p('Mouth',(0,-.203,1.665),(.10,.015,.017),'Hair','Head')
 for sign,side in [(1,'L'),(-1,'R')]:
  for n,c,s,mat,bone in [
   ('Sleeve',(.4*sign,0,1.43),(.36,.27,.29),'Cloth','UpperArm'),('Forearm',(.69*sign,0,1.35),(.31,.21,.23),'Skin','LowerArm'),
   ('Glove',(.89*sign,-.01,1.28),(.20,.21,.20),'Leather','Hand'),('Trouser',(.15*sign,0,.69),(.25,.29,.4),'Leather','UpperLeg'),
   ('Boot',(.15*sign,-.045,.29),(.26,.30,.43),'Leather','LowerLeg'),('Toe',(.15*sign,-.10,.10),(.28,.43,.19),'Leather','Foot')]:p(n+side,c,s,mat,bone+'_'+side)
  p('BootCuff'+side,(.15*sign,-.015,.46),(.29,.31,.10),'Gold','LowerLeg_'+side)
 for var,alt in [('Scrapper',False),('Sentinel',True)]:
  # Open-faced Breaker helmet with head-mounted scanner; face stays readable.
  p('Circlet',(0,-.015,1.995),(.59,.43,.09),'Steel' if not alt else 'Ink','Head','Helmet',var,not alt)
  p('Gem',(0,-.239,1.995),(.22,.07,.13),'Rune' if not alt else 'Crystal','Head','Helmet',var,not alt)
 for var,alt in [('Scrapper',False),('Bulwark',True)]:
  slot='Armor';default=not alt
  p('Vest',(0,-.15,1.31),(.43,.13,.37),'Steel' if not alt else 'Cloth','Chest',slot,var,default)
  p('Collar',(0,-.09,1.535),(.46,.29,.10),'Cape' if not alt else 'Hood','Chest',slot,var,default)
  for sign,side in [(1,'L'),(-1,'R')]:p('Shoulder'+side,(.31*sign,0,1.49),(.26,.34,.16),'Gold' if not alt else 'Steel','UpperArm_'+side,slot,var,default)
  # Single coherent cape mesh, curved away from the body and split hem silhouette.
  verts=[(-.22,.18,1.5),(0,.21,1.54),(.22,.18,1.5),(-.31,.30,1.15),(0,.34,1.16),(.31,.30,1.15),(-.35,.44,.65),(0,.48,.71),(.35,.44,.65)]
  mesh=bpy.data.meshes.new('Cape');mesh.from_pydata(verts,[],[(0,3,4,1),(1,4,5,2),(3,6,7,4),(4,7,8,5)]);mesh.update()
  o=bpy.data.objects.new('VAR_Armor_'+var+'_Cape',mesh);bpy.context.collection.objects.link(o);b.finish_mesh(o,m['Cloth' if not alt else 'Hood'],0)
  mod=o.modifiers.new('Cloth thickness','SOLIDIFY');mod.thickness=.025;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name);b.bone_parent(o,rig,'Chest');b.mark_variant(o,slot,var,default)
 for var,alt in [('ScrapHammer',False),('PlasmaCutter',True)]:
  p('Grip',(-.91,-.02,1.14),(.10,.12,.30),'Leather','Hand_R','Melee',var,not alt)
  p('Guard',(-.91,-.02,1.01),(.26,.15,.09),'Steel','Hand_R','Melee',var,not alt)
  if alt:
   sh('PlasmaBlade',[(.21,.005,.008,-.91,-.02),(.40,.12,.035,-.91,-.02),(.97,.10,.04,-.91,-.02)],'Crystal','Hand_R','Melee',var,False)
  else:
   p('GravityHead',(-.91,-.02,.61),(.62,.30,.34),'Steel','Hand_R','Melee',var,True)
   p('GravityCore',(-.91,-.182,.61),(.30,.035,.14),'Rune','Hand_R','Melee',var,True)
   for sign in [-1,1]:p('ImpactCap'+str(sign),(-.91+sign*.26,-.02,.61),(.10,.34,.37),'Ink','Hand_R','Melee',var,True)
 for var,alt in [('PulseCaster',False),('ArcBlaster',True)]:
  p('PulseHousing',(.79,0,1.31),(.40,.24,.21),'Steel','LowerArm_L','Ranged',var,not alt)
  p('Barrel',(1.045,0,1.31),(.22,.15,.15),'Ink','LowerArm_L','Ranged',var,not alt)
  p('Muzzle',(1.16,0,1.31),(.045,.16,.16),'Rune' if not alt else 'Crystal','LowerArm_L','Ranged',var,not alt)
  p('Battery',(.79,.14,1.31),(.2,.10,.18),'Cape','LowerArm_L','Ranged',var,not alt)
 for var,alt in [('AegisEmitter',False),('PrismEmitter',True)]:
  p('Buckler',(.67,-.17,1.35),(.30,.09,.33),'Gold' if not alt else 'Steel','LowerArm_L','Shield',var,not alt)
  p('Gem',(.67,-.225,1.35),(.13,.035,.16),'Rune' if not alt else 'Crystal','LowerArm_L','Shield',var,not alt)
 for var,alt in [('Reclaimer',False),('Capacitor',True)]:
  p('RigPack',(0,.34,1.3),(.30,.19,.35),'Steel','Chest','Rig',var,not alt)
  p('Reactor',(0,.45,1.3),(.13,.025,.22),'Rune' if not alt else 'Rune','Chest','Rig',var,not alt)
 for name,bone,loc in [('SOCKET_RightHand_Melee','Hand_R',(-.91,-.02,1.25)),('SOCKET_LeftArm_RangedShield','LowerArm_L',(.78,-.09,1.32)),('SOCKET_Back','Chest',(0,.24,1.36)),('SOCKET_PetAnchor','Chest',(.45,.25,1.55)),('ANCHOR_Muzzle','LowerArm_L',(1.13,-.09,1.31)),('ANCHOR_Shield','LowerArm_L',(.67,-.24,1.35)),('ANCHOR_MeleeTrail','Hand_R',(-.91,-.02,.68)),('ANCHOR_Hit','Chest',(0,-.22,1.35)),('ANCHOR_Feet','Root',(0,0,.02))]:b.make_socket(name,rig,bone,loc)
 rig['vb_schema_version']=1
 polished.refine_actor(rig,m)
 return rig,m

def save_actor(rig,m,path,fbx,preview):
 a.export(fbx);b.setup_preview({'dark':m['Ink']});a.animate(rig)
 bpy.ops.wm.save_as_mainfile(filepath=str(path))
 rig.animation_data_create();rig.animation_data.action=bpy.data.actions['Idle'];bpy.context.scene.frame_set(1)
 a.render(preview)
 if 'Modular' in path.name:a.render(ROOT/'Docs/Images/Vaultbreaker_Back_Preview.png',True)

def enemy(role):
 b.reset_scene();m=mats();rig=b.create_armature();big=role=='Bruiser';hood=role=='Shooter'
 def p(n,c,s,mat,bone):return a.plate('EN_'+role+'_'+n,c,s,m[mat],bone,rig)
 def sh(n,rings,mat,bone):return a.shell('EN_'+role+'_'+n,rings,m[mat],bone,rig)
 body='Stone' if big else 'Hood' if hood else 'Cape';skin='StoneLight' if big else 'Goblin'
 sh('Torso',[(.94,.29 if big else .18,.18,0,0),(1.3,.43 if big else .24,.25,0,0),(1.58,.38 if big else .24,.22,0,0)],body,'Chest')
 p('Waist',(0,0,.96),(.48 if big else .37,.34,.20),body,'Hips')
 sh('Head',[(1.57,.24,.2,0,0),(1.69,.35 if big else .29,.24,0,-.02),(1.94,.34 if big else .30,.24,0,0),(2.05,.26,.19,0,0)],skin,'Head')
 if hood:
  sh('Cowl',[(1.59,.32,.18,0,.12),(1.96,.36,.27,0,.04),(2.19,.17,.19,0,.05),(2.24,.05,.05,0,.02)],'Hood','Head')
  p('FaceShadow',(0,-.245,1.81),(.45,.03,.30),'Ink','Head')
 for sign in [-1,1]:
  p('Eye'+str(sign),(.13*sign,-.27,1.84),(.13,.045,.075),'Crystal' if hood else 'Rune' if big else 'Flame','Head')
  if not big and not hood:
   p('Sensor'+str(sign),(.30*sign,0,1.94),(.10,.18,.31),'Gold','Head')
   p('JawBolt'+str(sign),(.16*sign,-.25,1.64),(.10,.055,.07),'Steel','Head')
 if big:
  p('Brow',(0,-.20,1.985),(.71,.22,.11),'Stone','Head')
  p('Heart',(0,-.275,1.35),(.24,.07,.30),'Rune','Chest')
 else:p('Belt',(0,0,.98),(.45,.35,.09),'Gold','Hips')
 for sign,side in [(1,'L'),(-1,'R')]:
  for n,c,s,mat,bone in [('UpperArm',(.4*sign,0,1.435),(.4,.36 if big else .22,.35 if big else .23),body,'UpperArm'),('Forearm',(.7*sign,0,1.36),(.34,.42 if big else .22,.40 if big else .23),skin,'LowerArm'),('Hand',(.9*sign,0,1.28),(.30 if big else .19,.38 if big else .20,.33 if big else .20),skin,'Hand'),('Thigh',(.15*sign,0,.7),(.32 if big else .23,.32,.4),body,'UpperLeg'),('Shin',(.15*sign,-.02,.33),(.31 if big else .22,.32,.34),body,'LowerLeg'),('Foot',(.15*sign,-.10,.1),(.35 if big else .26,.45,.2),body,'Foot')]:p(n+side,c,s,mat,bone+'_'+side)
  if big:
   p('Shoulder'+side,(.36*sign,0,1.55),(.48,.50,.40),'StoneLight','UpperArm_'+side)
   p('RuneFist'+side,(.91*sign,-.21,1.29),(.18,.035,.16),'Rune','Hand_'+side)
 if not hood and not big:
  p('ChestArmor',(0,-.24,1.32),(.37,.08,.34),'Ink','Chest')
  p('Battery',(0,-.293,1.35),(.15,.025,.09),'Flame','Chest')
  for sign,side in [(1,'L'),(-1,'R')]:p('KneePlate'+side,(.15*sign,-.20,.48),(.18,.07,.15),'Steel','LowerLeg_'+side)
 if hood:
  p('RepoCannon',(.74,-.1,1.34),(.45,.25,.24),'Steel','LowerArm_L')
  p('RepoMuzzle',(1.05,-.1,1.34),(.20,.21,.20),'Flame','LowerArm_L')
  p('Scanner',(.16,-.18,2.13),(.16,.14,.15),'Crystal','Head')
 elif not big:
  p('CutterGrip',(-.91,-.02,1.12),(.09,.11,.29),'Ink','Hand_R')
  sh('Cutter',[(.55,.006,.006,-.91,-.02),(.65,.12,.035,-.91,-.02),(1.02,.10,.035,-.91,-.02)],'Steel','Hand_R')
 polished.refine_actor(rig,m,role)
 source=ROOT/'ArtSource/Blender/Characters/Enemies'/('Enemy_'+role+'.blend');source.parent.mkdir(parents=True,exist_ok=True)
 save_actor(rig,m,source,ROOT/'Assets/Vaultbreakers/Art/Characters/Enemies'/('Enemy_'+role+'.fbx'),ROOT/'Docs/Images'/('Enemy_'+role+'_Preview.png'))

def export_environment():
 # Keep independently editable source objects in .blend; combine export by material and room.
 # This avoids hundreds of tiny draw submissions without merging the entire mission's bounds.
 groups={}
 for obj in list(bpy.context.scene.objects):
  if obj.type!='MESH' or not obj.get('vb_export',False):continue
  for uv in obj.data.uv_layers:uv.name='UVMap'
  mat=obj.data.materials[0].name if obj.data.materials else 'None'
  center=sum((obj.matrix_world @ Vector(corner) for corner in obj.bound_box),Vector())/8
  room=max(0,min(2,round(-center.y/28)))
  groups.setdefault((room,mat),[]).append(obj)
 for (room,mat),objects in groups.items():
  bpy.ops.object.select_all(action='DESELECT')
  for obj in objects:obj.select_set(True)
  bpy.context.view_layer.objects.active=objects[0]
  if len(objects)>1:bpy.ops.object.join()
  bpy.context.object.name='Zone_'+str(room)+'_'+mat
 a.export(ROOT/'Assets/Vaultbreakers/Art/Environments/Dock9_Dungeon.fbx')

def environment():
 b.reset_scene();m=mats();polished.environment(m)
 folder=ROOT/'ArtSource/Blender/Environments';folder.mkdir(parents=True,exist_ok=True)
 bpy.ops.wm.save_as_mainfile(filepath=str(folder/'Dock9_Dungeon.blend'));export_environment()
 print('Polished Dock9 environment exported',flush=True)

def main():
 if '--environment-only' in sys.argv:
  environment();return
 rig,m=player();a.animate(rig);a.export(b.FBX_PATH)
 rig.animation_data_create();rig.animation_data.action=bpy.data.actions['Idle'];a.export(ROOT/'Assets/Vaultbreakers/Art/Animation/Vaultbreaker_Animations.fbx',True,True)
 rig.animation_data_clear()
 for pb in rig.pose.bones:pb.rotation_quaternion=(1,0,0,0)
 b.setup_preview({'dark':m['Ink']});bpy.ops.wm.save_as_mainfile(filepath=str(b.SOURCE_PATH))
 rig.animation_data_create();rig.animation_data.action=bpy.data.actions['Idle'];bpy.context.scene.frame_set(1);a.render(b.PREVIEW_PATH);a.render(ROOT/'Docs/Images/Vaultbreaker_Back_Preview.png',True)
 for role in ['Grunt','Shooter','Bruiser']:enemy(role)
 if '--actors-only' not in sys.argv:environment()
 print('Dock9 source models, animations and FBX exports complete.',flush=True)
if __name__=='__main__':main()
