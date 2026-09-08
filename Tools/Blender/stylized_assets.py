"""Vaultbreakers clean arcade art. Uses the unchanged schema-1 skeleton/socket contract."""
import math
import sys
from pathlib import Path
import bpy
from mathutils import Vector, Quaternion
sys.path.insert(0, str(Path(__file__).resolve().parent))
import generate_modular_vaultbreaker as base

ROOT = Path(__file__).resolve().parents[2]

def shell(name, rings, material, bone=None, rig=None, slot=None, variant=None, default=True):
    # Eight-sided tailored sections: broad planes, bevelled corners, deliberate taper.
    vertices=[]
    for z, w, d, cx, cy in rings:
        vertices += [(cx+x*w,cy+y*d,z) for x,y in [(-.7,-1),(.7,-1),(1,-.7),(1,.7),(.7,1),(-.7,1),(-1,.7),(-1,-.7)]]
    faces=[tuple(reversed(range(8)))]
    for j in range(len(rings)-1):
        for i in range(8):faces.append((j*8+i,j*8+(i+1)%8,(j+1)*8+(i+1)%8,(j+1)*8+i))
    faces.append(tuple((len(rings)-1)*8+i for i in range(8)))
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
    base.finish_mesh(obj,material,.014)
    if bone:base.bone_parent(obj,rig,bone)
    if slot:base.mark_variant(obj,slot,variant,default)
    return obj

def plate(name, center, size, material, bone=None, rig=None, slot=None, variant=None, default=True):
    x,y,z=center;w,d,h=size
    return shell(name,[(z-h/2,w*.38,d*.42,x,y),(z-h*.24,w/2,d/2,x,y),(z+h*.3,w*.48,d/2,x,y),(z+h/2,w*.32,d*.4,x,y)],material,bone,rig,slot,variant,default)

def mats():
    return {
      'undersuit':base.make_material('VB_Undersuit',(.025,.042,.08),.15,.5),
      'metal':base.make_material('VB_SalvageMetal',(.08,.27,.38),.45,.3),
      'dark':base.make_material('VB_DarkMetal',(.02,.032,.065),.45,.28),
      'orange':base.make_material('VB_HazardOrange',(1,.23,.1),.15,.35),
      'bone':base.make_material('VB_Ceramic',(.72,.85,.9),.2,.32),
      'cyan':base.make_material('VB_EnergyCyan',(.025,.6,.85),.15,.22,(.01,.8,1),3),
      'violet':base.make_material('VB_EnergyViolet',(.55,.12,.8),.15,.3,(.65,.05,1),2),
      'visor':base.make_material('VB_Visor',(.01,.16,.23),.5,.2,(.02,.8,1),2),
    }

def player():
    base.reset_scene(); m=mats(); rig=base.create_armature()
    def p(name,c,s,mat,bone,slot=None,var=None,default=True):
        return plate(name,c,s,m[mat],bone,rig,slot,var,default)
    p('BASE_Torso',(0,0,1.33),(.43,.27,.48),'undersuit','Chest')
    p('BASE_Pelvis',(0,0,.91),(.4,.27,.21),'dark','Hips')
    p('BASE_Neck',(0,0,1.59),(.15,.17,.16),'dark','Neck')
    p('BASE_Head',(0,0,1.74),(.29,.26,.28),'dark','Head')
    for side,sign in [('L',1),('R',-1)]:
        # Connectors use fitted tapered sections; no loose floating armor blocks.
        for part,c,s,bone in [
            ('UpperArm',(.40*sign,0,1.435),(.33,.16,.17),'UpperArm'),
            ('LowerArm',(.69*sign,0,1.36),(.29,.15,.16),'LowerArm'),
            ('Hand',(.89*sign,-.005,1.29),(.18,.15,.15),'Hand'),
            ('UpperLeg',(.15*sign,0,.69),(.19,.22,.35),'UpperLeg'),
            ('LowerLeg',(.15*sign,0,.32),(.18,.20,.34),'LowerLeg'),
            ('Boot',(.15*sign,-.07,.10),(.24,.38,.19),'Foot')]:
            p('BASE_'+part+'_'+side,c,s,'undersuit' if part not in ['Boot','Hand'] else 'dark',bone+'_'+side)
        thumb=base.add_sphere('BASE_Thumb_'+side,(.84*sign,-.085,1.25),(.065,.045,.07),m['dark'],12,8)
        base.bone_parent(thumb,rig,'Hand_'+side)
        p('BASE_Sole_'+side,(.15*sign,-.07,.035),(.25,.39,.045),'orange','Foot_'+side)
    for variant,heavy in [('Scrapper',False),('Sentinel',True)]:
        prefix='VAR_Helmet_'+variant+'_'
        mat='bone' if not heavy else 'metal'
        p(prefix+'Crown',(0,.015,1.79),(.39 if heavy else .36,.34,.33),mat,'Head','Helmet',variant,not heavy)
        visor=base.add_box(prefix+'Visor',(0,-.163,1.79),(.285,.035,.085 if not heavy else .055),m['visor' if not heavy else 'violet'],.022)
        base.tag_and_parent([visor],rig,'Head','Helmet',variant,not heavy)
        p(prefix+'Jaw',(0,-.115,1.66),(.27,.15,.12),'metal' if not heavy else 'bone','Head','Helmet',variant,not heavy)
        for sign in [-1,1]:
            p(prefix+'Temple'+str(sign),(.18*sign,.015,1.79),(.055,.21,.17),'orange' if not heavy else 'bone','Head','Helmet',variant,not heavy)
        p(prefix+'Stripe',(0,.02,1.955),(.055,.24,.025),'orange','Head','Helmet',variant,not heavy)
    for variant,heavy in [('Scrapper',False),('Bulwark',True)]:
        prefix='VAR_Armor_'+variant+'_'; default=not heavy
        shell(prefix+'ChestPlate',[(1.13,.17,.08,0,-.13),(1.34,.25 if heavy else .22,.09,0,-.13),(1.51,.27 if heavy else .23,.075,0,-.1),(1.56,.15,.055,0,-.07)],m['bone' if not heavy else 'metal'],'Chest',rig,'Armor',variant,default)
        p(prefix+'Core',(0,-.235,1.38),(.14,.035,.11),'cyan' if not heavy else 'violet','Chest','Armor',variant,default)
        p(prefix+'Abdomen',(0,-.14,1.08),(.28,.08,.11),'metal','Spine','Armor',variant,default)
        for side,sign in [('L',1),('R',-1)]:
            for part,c,s,bone in [
                ('Shoulder',(.32*sign,0,1.49),(.3 if heavy else .25,.32,.22),'UpperArm'),
                ('Forearm',(.68*sign,-.015,1.365),(.26,.23,.22),'LowerArm'),
                ('Thigh',(.15*sign,-.06,.72),(.24,.19,.30),'UpperLeg'),
                ('Shin',(.15*sign,-.09,.33),(.23,.14,.34),'LowerLeg')]:
                p(prefix+part+'_'+side,c,s,'bone' if not heavy else 'metal',bone+'_'+side,'Armor',variant,default)
            p(prefix+'Knee_'+side,(.16*sign,-.12,.5),(.18,.075,.12),'orange','LowerLeg_'+side,'Armor',variant,default)
    for variant,blade in [('ScrapHammer',False),('PlasmaCutter',True)]:
        prefix='VAR_Melee_'+variant+'_'
        p(prefix+'Grip',(-.91,-.02,1.12),(.10,.12,.34),'dark','Hand_R','Melee',variant,not blade)
        if blade:
            shell(prefix+'Blade',[(.44,.01,.025,-.91,-.02),(.55,.10,.032,-.91,-.02),(1.01,.085,.032,-.91,-.02)],m['violet'],'Hand_R',rig,'Melee',variant,False)
            p(prefix+'Hilt',(-.91,-.02,1.03),(.30,.17,.09),'bone','Hand_R','Melee',variant,False)
        else:
            p(prefix+'Head',(-.91,-.02,.72),(.59,.27,.29),'metal','Hand_R','Melee',variant)
            for sign in [-1,1]:p(prefix+'Cap'+str(sign),(-.91+sign*.235,-.02,.72),(.11,.30,.31),'bone','Hand_R','Melee',variant)
            p(prefix+'Core',(-.91,-.166,.72),(.28,.02,.11),'cyan','Hand_R','Melee',variant)
    for variant,alt in [('PulseCaster',False),('ArcBlaster',True)]:
        prefix='VAR_Ranged_'+variant+'_'
        p(prefix+'Body',(.79,-.08,1.32),(.40,.27,.24),'bone' if not alt else 'metal','LowerArm_L','Ranged',variant,not alt)
        p(prefix+'Barrel',(1.035,-.09,1.32),(.26,.16,.14),'dark','LowerArm_L','Ranged',variant,not alt)
        p(prefix+'Muzzle',(1.145,-.09,1.32),(.035,.18,.16),'cyan' if not alt else 'violet','LowerArm_L','Ranged',variant,not alt)
        p(prefix+'Rail',(.79,-.08,1.46),(.27,.12,.035),'orange','LowerArm_L','Ranged',variant,not alt)
    for variant,alt in [('AegisEmitter',False),('PrismEmitter',True)]:
        prefix='VAR_Shield_'+variant+'_'
        p(prefix+'Frame',(.67,-.19,1.35),(.25,.07,.25),'metal' if not alt else 'bone','LowerArm_L','Shield',variant,not alt)
        p(prefix+'Core',(.67,-.235,1.35),(.15,.025,.15),'cyan' if not alt else 'violet','LowerArm_L','Shield',variant,not alt)
    for variant,alt in [('Reclaimer',False),('Capacitor',True)]:
        prefix='VAR_Rig_'+variant+'_'
        p(prefix+'Pack',(0,.21,1.35),(.36,.22,.40),'bone' if not alt else 'metal','Chest','Rig',variant,not alt)
        for sign in [-1,1]:p(prefix+'Vent'+str(sign),(.11*sign,.335,1.35),(.055,.025,.26),'cyan' if not alt else 'violet','Chest','Rig',variant,not alt)
    # Exact bind transforms retained from schema 1.
    for name,bone,loc in [
      ('SOCKET_RightHand_Melee','Hand_R',(-.91,-.02,1.25)),('SOCKET_LeftArm_RangedShield','LowerArm_L',(.78,-.09,1.32)),
      ('SOCKET_Back','Chest',(0,.24,1.36)),('SOCKET_PetAnchor','Chest',(.45,.25,1.55)),('ANCHOR_Muzzle','LowerArm_L',(1.13,-.09,1.31)),
      ('ANCHOR_Shield','LowerArm_L',(.67,-.24,1.35)),('ANCHOR_MeleeTrail','Hand_R',(-.91,-.02,.68)),('ANCHOR_Hit','Chest',(0,-.22,1.35)),('ANCHOR_Feet','Root',(0,0,.02))]:base.make_socket(name,rig,bone,loc)
    rig['vb_schema_version']=1
    return rig,m

def export(path, animation=False, armature_only=False):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in bpy.context.scene.objects:
        if obj.get('vb_export',False) and (not armature_only or obj.type=='ARMATURE'):obj.select_set(True)
    path.parent.mkdir(parents=True,exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,use_armature_deform_only=False,bake_anim=animation,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='RELATIVE')

def animate(rig):
    rig.animation_data_clear()
    for action in list(bpy.data.actions):bpy.data.actions.remove(action)
    # Bone rotations expressed in skeleton/world axes, then mapped to each bone's local basis.
    def pose(name,axis,angle):
        pb=rig.pose.bones[name];pb.rotation_mode='QUATERNION'
        basis=rig.data.bones[name].matrix_local.to_quaternion()
        pb.rotation_quaternion=basis.inverted() @ Quaternion(axis,math.radians(angle)) @ basis
    for name,length in [('Idle',60),('Move',24),('Melee',18),('Ranged',18),('Shield',40),('ShieldHit',12),('ShieldBreak',20),('Dodge',12),('Hit',12),('Death',30)]:
        rig.animation_data_clear()
        for frame in range(1,length+1,2):
            t=(frame-1)/max(1,length-2); swing=math.sin(t*math.tau)
            for pb in rig.pose.bones:pb.rotation_mode='QUATERNION';pb.rotation_quaternion=Quaternion();pb.location=(0,0,0)
            pose('UpperArm_L',(0,1,0),62);pose('UpperArm_R',(0,1,0),-52);pose('Hand_R',(0,1,0),52)
            if name=='Idle':pose('Chest',(1,0,0),swing*1.5)
            if name=='Move':
                pose('UpperLeg_L',(1,0,0),swing*27);pose('UpperLeg_R',(1,0,0),-swing*27)
                pose('LowerLeg_L',(1,0,0),max(0,-swing)*32);pose('LowerLeg_R',(1,0,0),max(0,swing)*32)
                pose('Chest',(0,0,1),swing*4)
            if name=='Melee':pose('UpperArm_R',(0,1,0),-20-95*math.sin(t*math.pi));pose('Chest',(0,0,1),-25+70*t)
            if name in ['Ranged','Shield','ShieldHit']:pose('UpperArm_L',(0,0,1),-78);pose('LowerArm_L',(0,0,1),-12)
            if name=='Ranged':pose('LowerArm_L',(1,0,0),-8*abs(swing))
            if name in ['Hit','ShieldHit','ShieldBreak']:pose('Chest',(1,0,0),-18*math.sin(t*math.pi))
            if name=='Dodge':pose('Hips',(1,0,0),35*math.sin(t*math.pi));pose('UpperLeg_L',(1,0,0),-45*math.sin(t*math.pi))
            if name=='Death':pose('Hips',(1,0,0),-80*t)
            for pb in rig.pose.bones:pb.keyframe_insert(data_path='rotation_quaternion',frame=frame,group=pb.name)
        action=rig.animation_data.action;action.name=name;action.use_fake_user=True
    rig.animation_data_clear()
    for pb in rig.pose.bones:pb.rotation_quaternion=Quaternion()
    bpy.context.scene.frame_set(1)

def render(path, back=False):
    scene=bpy.context.scene;scene.render.resolution_x=1100;scene.render.resolution_y=1100
    scene.render.filepath=str(path)
    if back:
        camera=scene.camera;camera.location=(3.6,5.5,2.8);camera.rotation_euler=(Vector((0,0,1.05))-camera.location).to_track_quat('-Z','Y').to_euler()
    bpy.ops.render.render(write_still=True)

def main():
    rig,m=player();base.setup_preview(m)
    bpy.context.scene.world.color=(.018,.027,.05)
    animate(rig)
    export(base.FBX_PATH)
    rig.animation_data_create();rig.animation_data.action=bpy.data.actions.get('Idle')
    export(ROOT/'Assets/Vaultbreakers/Art/Animation/Vaultbreaker_Animations.fbx',True,True)
    rig.animation_data_clear()
    for pb in rig.pose.bones:pb.rotation_quaternion=Quaternion()
    bpy.context.scene.frame_set(1)
    bpy.ops.wm.save_as_mainfile(filepath=str(base.SOURCE_PATH))
    rig.animation_data_create();rig.animation_data.action=bpy.data.actions.get('Idle');bpy.context.scene.frame_set(1)
    render(base.PREVIEW_PATH)
    render(ROOT/'Docs/Images/Vaultbreaker_Back_Preview.png',True)
    print('Stylized player and animation exports complete')
if __name__=='__main__':main()
