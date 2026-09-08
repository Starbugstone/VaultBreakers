"""Rebuild enemy, arena, training and projectile meshes as editable Blender sources."""
import sys, math
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import bpy
from mathutils import Vector
import stylized_assets as art
import generate_modular_vaultbreaker as base
ROOT=Path(__file__).resolve().parents[2]

def enemy(role):
    base.reset_scene();m=art.mats();rig=base.create_armature()
    large=role=='Bruiser';thin=role=='Shooter';color='violet' if large else 'orange';armor='metal' if thin else 'bone'
    def p(name,c,s,mat,bone):return art.plate('EN_'+role+'_'+name,c,s,m[mat],bone,rig)
    p('Torso',(0,0,1.30),(.65 if large else .36,.38 if large else .25,.57),armor,'Chest')
    p('Waist',(0,0,.93),(.42 if large else .30,.26,.24),'dark','Hips')
    p('Head',(0,-.045 if large else 0,1.73),(.36 if large else .29,.31,.31),'metal' if large else 'orange','Head')
    if thin:
        # One optical barrel, antenna fins and a narrow silhouette identify the shooter.
        eye=base.add_cylinder('EN_Shooter_Optic',(0,-.19,1.78),.09,.09,m['orange'],16,(math.pi/2,0,0));base.bone_parent(eye,rig,'Head')
        for sign in [-1,1]:p('Antenna'+str(sign),(.20*sign,.02,1.96),(.075,.13,.40),'metal','Head')
    else:
        p('Visor',(0,-.205,1.77),(.27,.035,.055),color,'Head')
        p('Brow',(0,-.12,1.92),(.41 if large else .32,.21,.075),'metal','Head')
    for sign,side in [(1,'L'),(-1,'R')]:
        for part,c,s,bone in [('UpperArm',(.39*sign,0,1.43),(.32,.19,.21),'UpperArm'),('Forearm',(.7*sign,0,1.34),(.34,.3 if large else .16,.3 if large else .18),'LowerArm'),('Fist',(.91*sign,0,1.27),(.27 if large else .16,.29 if large else .14,.28 if large else .14),'Hand'),('Thigh',(.15*sign,0,.71),(.25 if large else .16,.24,.35),'UpperLeg'),('Shin',(.15*sign,-.025,.34),(.27 if large else .15,.25,.33),'LowerLeg'),('Foot',(.15*sign,-.09,.1),(.3 if large else .21,.39,.19),'Foot')]:
            p(part+side,c,s,armor if part not in ['Fist','UpperArm'] else 'dark',bone+'_'+side)
        p('Pauldron'+side,(.32*sign,0,1.51),(.43 if large else .24,.42 if large else .25,.31 if large else .2),color,'UpperArm_'+side)
        if large:p('Gauntlet'+side,(.87*sign,-.04,1.27),(.35,.38,.4),'metal','Hand_'+side)
    if thin:
        p('Rifle',(.7,-.22,1.35),(.18,.75,.20),'dark','LowerArm_L')
        p('Barrel',(.7,-.62,1.35),(.19,.12,.21),'orange','LowerArm_L')
    else:
        p('ChestMark',(0,-.23 if large else -.145,1.36),(.22,.035,.19),color,'Chest')
    p('BackPack',(0,.21,1.33),(.4 if large else .27,.22,.41),'dark','Chest')
    source=ROOT/'ArtSource/Blender/Characters/Enemies'/('Enemy_'+role+'.blend');source.parent.mkdir(parents=True,exist_ok=True)
    art.export(ROOT/'Assets/Vaultbreakers/Art/Characters/Enemies'/('Enemy_'+role+'.fbx'))
    base.setup_preview(m)
    # Relaxed showcase pose without changing the exported bind pose.
    art.animate(rig);rig.animation_data_create();rig.animation_data.action=bpy.data.actions['Idle'];bpy.context.scene.frame_set(1)
    bpy.ops.wm.save_as_mainfile(filepath=str(source))
    art.render(ROOT/'Docs/Images'/('Enemy_'+role+'_Preview.png'))

def arena():
    base.reset_scene();m=art.mats()
    floor=base.make_material('VB_StageFloor',(.018,.032,.068),.3,.5)
    tile=base.make_material('VB_StageTile',(.06,.09,.15),.4,.4)
    for x in range(-8,9,4):
        for y in range(-8,9,4):
            art.plate('StageTile_'+str(x)+'_'+str(y),(x,y,-.10),(3.96,3.96,.20),tile)
            for sign in [-1,1]:base.add_box('TileInset',(x+sign*1.63,y,-.001),(.025,2.6,.008),floor,.002)
    base.add_box('Foundation',(0,0,-.40),(21.6,21.6,.65),m['dark'],.2)
    for side in [-1,1]:
        for axis in ['X','Y']:
            for offset in range(-8,9,4):
                pos=(10.25*side,offset,.25) if axis=='X' else (offset,10.25*side,.25)
                size=(.55,3.9,.8) if axis=='X' else (3.9,.55,.8)
                art.plate('Barrier_'+axis+str(side)+str(offset),pos,size,m['metal'])
                strip=(10.0*side,offset,.58) if axis=='X' else (offset,10.0*side,.58)
                dims=(.07,3.5,.09) if axis=='X' else (3.5,.07,.09)
                base.add_box('BarrierLight',strip,dims,m['cyan'],.012)
    for x in [-10,10]:
        for y in [-10,10]:
            art.plate('CornerTower',(x,y,.90),(1.1,1.1,1.9),m['bone'])
            art.plate('CornerCore',(x,y,1.8),(.8,.8,.12),m['cyan'])
    for idx,(x,y) in enumerate([(-5,-4),(5,-4),(0,5)]):
        base.add_cylinder('PortalBase_'+str(idx),(x,y,.005),.8,.04,m['dark'],32)
        base.add_torus('PortalRing_'+str(idx),(x,y,.025),.69,.035,m['orange'])
        for sign in [-1,1]:base.add_box('PortalMark',(x+sign*.95,y,.025),(.3,.08,.03),m['orange'],.01)
    # A readable central emblem, flush to the playable floor.
    for angle in [45,135,225,315]:
        a=math.radians(angle);base.add_box('CoreEmblem',(math.sin(a)*1.5,math.cos(a)*1.5,.015),(.08,1.3,.025),m['cyan'],.01,(0,0,-a))
    # Beyond the collision boundary: quiet infrastructure with bright edge lights.
    for x in [-7,0,7]:
        art.plate('BackConsole',(x,-11.3,.6),(2,1,1.2),m['dark'])
        base.add_box('ConsoleLight',(x,-10.78,.9),(1.4,.04,.15),m['violet'],.02)
    source=ROOT/'ArtSource/Blender/Environments/Combat_Arena.blend';source.parent.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(source));art.export(ROOT/'Assets/Vaultbreakers/Art/Environments/Combat_Arena.fbx')
    print('Arena exported',flush=True)

def props():
    base.reset_scene();m=art.mats()
    art.plate('TrainingBase',(0,0,.15),(.8,.7,.3),m['dark'])
    art.plate('TrainingColumn',(0,0,.8),(.25,.25,1.2),m['metal'])
    art.plate('TrainingTarget',(0,0,1.3),(.85,.25,.95),m['bone'])
    base.add_cylinder('TrainingBullseye',(0,-.145,1.35),.24,.04,m['orange'],24,(math.pi/2,0,0))
    base.add_cylinder('TrainingCore',(0,-.17,1.35),.1,.045,m['cyan'],24,(math.pi/2,0,0))
    folder=ROOT/'ArtSource/Blender/Props';folder.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(folder/'Training_Target.blend'));art.export(ROOT/'Assets/Vaultbreakers/Art/Environments/Training_Target.fbx')
    for name,enemy in [('Player_Bolt',False),('Enemy_Bolt',True)]:
        base.reset_scene();m=art.mats()
        # The enemy shot has a large diamond head; the player shot is a narrow tracer.
        art.shell(name,[(-.35,.035,.035,0,0),(0,.18 if enemy else .065,.18 if enemy else .065,0,0),(.32,.005,.005,0,0)],m['orange' if enemy else 'cyan'])
        for obj in bpy.context.scene.objects:
            if obj.type=='MESH':obj.rotation_euler[0]=math.pi/2
        bpy.ops.wm.save_as_mainfile(filepath=str(folder/(name+'.blend')));art.export(ROOT/'Assets/Vaultbreakers/Art/VFX'/(name+'.fbx'))
    base.reset_scene();m=art.mats();base.add_cylinder('ShowcaseDeck',(0,0,-.07),.95,.12,m['dark'],64)
    base.add_torus('ShowcaseRing',(0,0,-.005),.90,.018,m['cyan'])
    bpy.ops.wm.save_as_mainfile(filepath=str(folder/'Showcase_Deck.blend'));art.export(ROOT/'Assets/Vaultbreakers/Art/Environments/Showcase_Deck.fbx')

for role in ['Grunt','Shooter','Bruiser']:enemy(role)
arena();props()
print('All enemy, arena and prop sources saved; explicit FBX exports complete.',flush=True)
