"""Dock 9 art revision: industrial salvage, transfer machinery and a black-glass vault.

All detail is original geometry. Collision, skeleton and gameplay remain owned by the
existing contract. Called by dungeon_assets.py; Blender sources retain separate parts.
"""
import math
import random
import bpy
from mathutils import Vector
import generate_modular_vaultbreaker as b
import stylized_assets as a


def finish_normals():
    # Evaluate the whole scene once, rather than running an operator/depsgraph rebuild per part.
    objects=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.get('vb_export',False)]
    for obj in objects:
        mod=obj.modifiers.new('Weighted surface normals','WEIGHTED_NORMAL')
        mod.keep_sharp=True
        mod.weight=50
    depsgraph=bpy.context.evaluated_depsgraph_get()
    meshes=[(obj,bpy.data.meshes.new_from_object(obj.evaluated_get(depsgraph),depsgraph=depsgraph)) for obj in objects]
    for obj,mesh in meshes:
        old=obj.data
        obj.modifiers.clear()
        obj.data=mesh
        if old.users==0:bpy.data.meshes.remove(old)


def refine_actor(rig, m, role=None):
    def plate(n, c, s, mat, bone, slot=None, variant=None, default=True):
        prefix = 'VAR_' + slot + '_' + variant + '_' if slot else ('BASE_' if role is None else 'EN_' + role + '_')
        return a.plate(prefix + n, c, s, m[mat], bone, rig, slot, variant, default)

    if role is None:
        # A fitted exosuit with layered greaves, inset seams, mechanical cuffs and harness.
        for variant, alt in [('Scrapper', False), ('Bulwark', True)]:
            metal = 'Steel' if not alt else 'Stone'
            for sign, side in [(-1, 'R'), (1, 'L')]:
                for n, c, s, mat, bone in [
                    ('Greave', (.15*sign, -.16, .30), (.23, .12, .30), metal, 'LowerLeg'),
                    ('Knee', (.15*sign, -.18, .50), (.23, .12, .18), 'Ink', 'LowerLeg'),
                    ('KneeInset', (.15*sign, -.244, .50), (.10, .024, .075), 'Gold', 'LowerLeg'),
                    ('ThighPlate', (.15*sign, -.13, .76), (.23, .10, .25), metal, 'UpperLeg'),
                    ('ShoulderInset', (.32*sign, -.15, 1.50), (.20, .08, .12), 'Cloth', 'UpperArm'),
                    ('ShoulderEdge', (.32*sign, -.18, 1.54), (.18, .022, .035), 'Rune', 'UpperArm'),
                ]:
                    plate(n+side, c, s, mat, bone+'_'+side, 'Armor', variant, not alt)
            plate('ChestKeel', (0, -.23, 1.32), (.095, .055, .34), 'Ink', 'Chest', 'Armor', variant, not alt)
            plate('ReactorLens', (0, -.267, 1.40), (.07, .022, .095), 'Rune', 'Chest', 'Armor', variant, not alt)
            for x in [-.14, .14]:
                plate('Clavicle'+str(x), (x, -.21, 1.47), (.14, .075, .09), 'CoreWhite', 'Chest', 'Armor', variant, not alt)
                plate('BeltPack'+str(x), (x, -.19, 1.055), (.12, .11, .12), 'Ink', 'Spine', 'Armor', variant, not alt)
        for side, sign in [('L', 1), ('R', -1)]:
            plate('Sole'+side, (.15*sign, -.1, .045), (.28, .43, .055), 'Ink', 'Foot_'+side)
            plate('ToeGuard'+side, (.15*sign, -.245, .145), (.25, .11, .11), 'Steel', 'Foot_'+side)
            plate('WristSeal'+side, (.80*sign, 0, 1.325), (.08, .23, .23), 'Ink', 'Hand_'+side)
            thumb = b.add_sphere('BASE_Thumb'+side, (.855*sign, -.105, 1.255), (.06, .058, .075), m['Leather'], 12, 8)
            b.bone_parent(thumb, rig, 'Hand_'+side)
        for variant, alt in [('ScrapHammer', False), ('PlasmaCutter', True)]:
            for i in range(3):
                plate('GripBand'+str(i), (-.91, -.024, 1.05+i*.07), (.115, .135, .025), 'Gold', 'Hand_R', 'Melee', variant, not alt)
            if not alt:
                for x in [-1.14, -.68]:
                    plate('HammerFace'+str(x), (x, -.19, .61), (.09, .04, .25), 'CoreWhite', 'Hand_R', 'Melee', variant)
                for z in [.52, .70]:
                    plate('HammerRail'+str(z), (-.91, -.18, z), (.35, .06, .04), 'Ink', 'Hand_R', 'Melee', variant)
        for variant, alt in [('PulseCaster', False), ('ArcBlaster', True)]:
            for i in range(3):
                plate('CoolingFin'+str(i), (.68+i*.075, -.14, 1.33), (.025, .045, .135), 'Ink', 'LowerArm_L', 'Ranged', variant, not alt)
            plate('SightRail', (.87, 0, 1.445), (.27, .09, .04), 'Ink', 'LowerArm_L', 'Ranged', variant, not alt)
        for variant, alt in [('Reclaimer', False), ('Capacitor', True)]:
            for x in [-.12, .12]:
                plate('ReactorRail'+str(x), (x, .46, 1.30), (.045, .06, .33), 'Ink', 'Chest', 'Rig', variant, not alt)
        # Reduce the oversized white eyes to inset pilot eyes; keep the open face.
        for obj in bpy.context.scene.objects:
            if obj.name.startswith(('BASE_EyeWhite','BASE_Pupil')):
                center=sum((v.co for v in obj.data.vertices),Vector())/len(obj.data.vertices)
                for vertex in obj.data.vertices:
                    vertex.co.z=center.z+(vertex.co.z-center.z)*.75

    else:
        heavy = role == 'Bruiser'
        shooter = role == 'Shooter'
        for sign, side in [(-1, 'R'), (1, 'L')]:
            plate('Joint'+side, (.57*sign, 0, 1.385), (.12, .31 if heavy else .24, .25), 'Ink', 'LowerArm_'+side)
            plate('BootToe'+side, (.15*sign, -.28, .12), (.30 if heavy else .23, .14, .15), 'Steel', 'Foot_'+side)
            plate('Greave'+side, (.15*sign, -.185, .31), (.27 if heavy else .19, .085, .28), 'Steel' if not heavy else 'StoneLight', 'LowerLeg_'+side)
            if heavy:
                plate('Pauldron'+side, (.43*sign, 0, 1.73), (.45, .53, .20), 'BlackGlass', 'UpperArm_'+side)
                for i in range(3):
                    plate('Knuckle'+side+str(i), (.91*sign, -.235, 1.19+i*.09), (.29, .06, .04), 'Gold', 'Hand_'+side)
            else:
                plate('Pauldron'+side, (.32*sign, 0, 1.54), (.27, .31, .15), 'Steel' if shooter else 'Cape', 'UpperArm_'+side)
        if heavy:
            for x in [-.25, .25]:
                plate('ChestButtress'+str(x), (x, -.24, 1.36), (.17, .12, .41), 'BlackGlass', 'Chest')
            for z in [1.18, 1.52]:
                plate('ReactorBrace'+str(z), (0, -.30, z), (.40, .08, .06), 'Gold', 'Chest')
        elif shooter:
            plate('Rangefinder', (-.26, -.16, 1.96), (.16, .25, .14), 'Steel', 'Head')
            plate('Optic', (-.26, -.29, 1.96), (.09, .02, .075), 'Crystal', 'Head')
            plate('BatteryPack', (0, .22, 1.36), (.32, .24, .42), 'Ink', 'Chest')
            for x in [-.10, .10]:
                plate('ChargeCell'+str(x), (x, .36, 1.36), (.07, .04, .28), 'Crystal', 'Chest')
        else:
            plate('Jaw', (0, -.23, 1.64), (.32, .09, .09), 'Ink', 'Head')
            for x in [-.09, 0, .09]:
                plate('Vent'+str(x), (x, -.292, 1.23), (.035, .025, .09), 'Steel', 'Chest')
    finish_normals()


def environment(m):
    rng = random.Random(91026)
    def box(n, c, s, k, bevel=.035, rot=0):
        mesh=bpy.data.meshes.new(n)
        mesh.from_pydata([(x*s[0]/2,y*s[1]/2,z*s[2]/2) for x,y,z in [(-1,-1,-1),(-1,-1,1),(-1,1,-1),(-1,1,1),(1,-1,-1),(1,-1,1),(1,1,-1),(1,1,1)]],[],[(0,2,6,4),(1,5,7,3),(0,4,5,1),(2,3,7,6),(0,1,3,2),(4,6,7,5)])
        mesh.materials.append(m[k]);mesh.update()
        obj=bpy.data.objects.new(n,mesh);bpy.context.collection.objects.link(obj)
        obj.location=c;obj.rotation_euler.z=rot;obj['vb_export']=True
        for poly in mesh.polygons:poly.use_smooth=True
        if bevel:
            mod=obj.modifiers.new('Machined edge','BEVEL');mod.width=bevel;mod.segments=2
        return obj
    def pipe(n, start, end, radius=.12, mat='Steel'):
        return b.add_cylinder_between(n, start, end, radius, m[mat], 12)
    def text(n, caption, c, size, mat, floor=False):
        curve = bpy.data.curves.new(n, 'FONT')
        curve.body = caption
        curve.size = size
        curve.align_x = 'CENTER'
        curve.extrude = .001
        obj = bpy.data.objects.new(n, curve)
        bpy.context.collection.objects.link(obj)
        obj.location = c
        obj.rotation_euler = (0, 0, math.pi) if floor else (math.pi/2, 0, math.pi)
        bpy.ops.object.select_all(action='DESELECT')
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.convert(target='MESH')
        b.finish_mesh(obj, m[mat], 0)
    def crate(x, y):
        box('Cargo case', (x,y,.46), (1.2,1,.92), 'Stone', .08)
        box('Cargo lid', (x,y,.94), (1.2,1,.08), 'Steel')
        for sign in [-1,1]:
            box('Case band', (x+sign*.43,y,.50), (.09,1.025,.94), 'Ink', .015)
            box('Cargo inset', (x,y+sign*.505,.49), (.66,.03,.50), 'Ink', .014)
            box('Latch', (x,y+sign*.53,.65), (.28,.035,.07), 'Gold', .008)
        box('Cargo serial', (x,y-.533,.38), (.28,.013,.10), 'CoreWhite', .004)
    def support(x,y,h,room):
        box('Structural foot', (x,y,.16), (1.04,1.04,.32), 'Ink', .06)
        a.plate('Tapered dock support', (x,y,h*.5), (.68,.70,h), m['BlackGlass' if room==2 else 'Stone'])
        box('Support cap', (x,y,h), (.91,.90,.15), 'Steel')
        box('Recessed light bed', (x,y-.358,h*.60), (.28,.045,h*.42), 'Ink', .012)
        box('Support strip', (x,y-.384,h*.60), (.052,.02,h*.34), 'Rune' if room==2 else 'Flame', .006)
    def lamp(x,y,room):
        box('Worklight base', (x,y,.14), (.64,.64,.28), 'Ink')
        box('Worklight pedestal', (x,y,.49), (.36,.36,.52), 'Stone')
        box('Lamp housing', (x,y,.83), (.60,.60,.28), 'Ink')
        for sign in [-1,1]:
            box('Lamp diffuser', (x,y+sign*.305,.84), (.42,.024,.14), 'Rune' if room==2 else 'Flame', .012)
        box('Lamp hood', (x,y,1.01), (.68,.68,.07), 'Steel')

    for room in range(3):
        print('Authoring industrial room',room+1,flush=True)
        cy = -28*room
        floor = ['Tile','VaultTile','BlackGlass'][room]
        box('Layered foundation', (0,cy,-.63), (21.6,21.6,1.2), 'Ink', .16)
        box('Foundation rim', (0,cy,-.21), (21.2,21.2,.24), 'Stone', .06)
        # Broad offset slabs replace the noisy small checkerboard.
        for ix in range(5):
            for iy in range(5):
                x=-8+ix*4; y=cy-8+iy*4
                box('Deck slab', (x,y,-.075), (3.97,3.97,.15), floor, .018)
                for side in [-1,1]:
                    box('Panel inset seam', (x+side*1.80,y,.006), (.018,3.58,.012), 'Ink', .003)
                if (ix+iy)%3==0:
                    for off in [-.13,.13]:
                        box('Deck fastener', (x+1.67,y+1.6+off,.018), (.07,.075,.018), 'Steel', .004)
        # Recessed service trenches flank a quiet fighting area.
        for side in [-1,1]:
            x=side*8.9
            box('Drain channel', (x,cy,.012), (.56,19.6,.035), 'Ink', .006)
            for i in range(49):
                box('Drain crossbar', (x,cy-9.6+i*.4,.034), (.54,.075,.025), 'Stone', .006)
            for i in range(5):
                box('Boundary guide', (side*9.55,cy-8+i*4,.024), (.05,1.1,.02), 'Gold' if room<2 else 'Rune', .005)
            # Low articulated perimeter; no tall objects across the player's foreground.
            for i in range(10):
                y=cy-9+i*2
                box('Dock retaining wall', (side*10.35,y,.33), (.68,1.98,.66), 'Stone', .035)
                box('Coping rail', (side*10.35,y,.70), (.80,1.98,.11), 'Ink', .025)
                box('Perimeter inset', (side*10.35,y,.775), (.25,.75,.026), 'Steel', .008)
            for end in [-1,1]:
                for x2 in [-6.6,6.6]:
                    box('Bulkhead footing', (x2,cy+end*10.35,.30), (7.2,.70,.6), 'Stone', .03)
                    box('Bulkhead cap', (x2,cy+end*10.35,.64), (7.2,.80,.09), 'Ink', .02)
        for x in [-8,8]:
            for y in [-8,8]: support(x,cy+y,2.4 if y<0 else 1.5,room)
        for x in [-5.44,5.44]:
            for y in [-8,8]: lamp(x,cy+y,room)
        for x,y in [(-8,3),(8,-3),(-6,-8)]: crate(x,cy+y)
        for x in [-2.8,2.8]: support(x,cy-10,3,room)
        box('Gate bridge beam', (0,cy-10,3.14), (6.3,.74,.40), 'Ink', .07)
        box('Gate fascia', (0,cy-9.60,3.12), (4.8,.065,.29), 'Stone', .018)
        text('Sector sign', ['09 / INTAKE','TRANSFER / 02','VAULT / 03'][room], (0,cy-9.56,3.05), .19, 'CoreWhite')
        for x in [-2.2,2.2]: box('Gate beacon', (x,cy-9.55,3.12), (.23,.04,.11), 'Rune', .008)
        text('Deck sector', ['09','02','03'][room], (-5,cy+5,.025), 1.8, 'StoneLight', True)
        text('Deck instruction', ['SALVAGE','TRANSFER','RESTRICTED'][room], (-5,cy+5.55,.027), .23, 'StoneLight', True)
        for y in [-9.3,9.3]:
            for i in range(12):
                box('Threshold warning', (-2.2+i*.4,cy+y,.022), (.18,.52,.023), 'Gold', .005, -.45)
        # Large silhouettes and layered depth outside the collision boundary.
        for side in [-1,1]:
            for y in [-7,0,7]:
                x=side*11.9; yy=cy+y
                box('Service outrigger', (x,yy,-.9), (4.6,5.9,.5), 'Stone', .09)
                if room==0:
                    for k in range(2):
                        box('Salvage container', (x,yy,k*1.05+.1), (2.8,4.7,.98), 'Hood' if k else 'Cape', .08)
                        for j in range(8):
                            box('Container rib', (x,yy-2+j*.57,k*1.05+.64), (2.85,.07,.09), 'Ink', .014)
                    box('Container serial plate', (x,yy+2.37,.85), (1.4,.04,.40), 'Ink')
                elif room==1:
                    for off in [-.78,.78]:
                        b.add_cylinder('Pressure vessel', (x+off,yy,1.15), .67,3.4,m['Steel'],20,bevel=.06)
                        for z in [-.3,.7,2.5]:
                            b.add_torus('Tank band',(x+off,yy,z),.68,.055,m['Ink'])
                        pipe('Exhaust riser',(x+off,yy,2.85),(x+off,yy-1.7,2.85),.18,'Gold')
                    box('Manifold cabinet',(x,yy+1.7,.42),(2.6,.75,1.3),'Stone')
                else:
                    a.plate('Vault monolith',(x,yy,1.75),(2.5,3.6,5.4),m['BlackGlass'])
                    for off in [-.7,.7]:
                        box('Vault spine',(x+off,yy+1.72,1.7),(.13,.14,3.7),'StoneLight')
                    box('Core slit',(x,yy+1.82,1.75),(.10,.035,2.7),'Rune',.008)
                for off in [-.9,.9]:
                    pipe('Exterior trunk',(side*11.3,yy-2.5,off-.8),(side*11.3,yy+2.5,off-.8),.20,'Steel')
            # Distant dock superstructure reads through the open silhouette of the stage.
            for y in [-12,12]:
                x=side*16
                box('Distant support',(x,cy+y,-2),(1.2,1.2,10),'BlackGlass',.09)
                box('Cross structure',(x,cy+y,2.6),(5.2,.65,.8),'Stone',.06)
        if room<2:
            box('Bridge keel',(0,cy-14,-.43),(5.6,8,.8),'Ink')
            for j in range(8):
                box('Bridge decking',(0,cy-10.5-j,-.045),(5.5,.98,.09),'VaultTile',.018)
            for side in [-1,1]:
                box('Bridge guard',(side*2.9,cy-14,.30),(.4,8,.60),'Stone')
                box('Bridge handrail',(side*2.9,cy-14,.67),(.48,8,.12),'Ink')
                for j in range(4):
                    box('Bridge marker',(side*2.65,cy-11-j*2,.035),(.08,.4,.035),'Rune',.008)
        else:
            box('Vault dais',(0,cy-7,.055),(4,3,.11),'VaultTile',.04)
            box('Vault cache',(0,cy-7,.60),(1.8,.95,1),'Ink',.10)
            box('Vault cache lid',(0,cy-7,1.17),(1.95,1.05,.30),'Steel',.09)
            for x in [-.68,.68]: box('Cache restraint',(x,cy-7,.76),(.13,1.08,.92),'Gold',.02)
            box('Cache lock',(0,cy-6.49,.77),(.24,.04,.26),'Rune',.015)
            for x in [-2,2]:
                box('Dais inlay',(x,cy-7,.12),(.045,3,.02),'Rune',.005)
    bpy.context.view_layer.update()
    print('Generating environment UVs and normals',flush=True)
    # World-projected UVs give the reproducible Unity surface maps a consistent metre scale.
    for obj in list(bpy.context.scene.objects):
        if obj.type!='MESH': continue
        uv=obj.data.uv_layers.active or obj.data.uv_layers.new(name='UVMap')
        uv.name='UVMap'
        for poly in obj.data.polygons:
            normal=obj.matrix_world.to_3x3() @ poly.normal
            axis=max(range(3),key=lambda i:abs(normal[i]))
            axes=[i for i in range(3) if i!=axis]
            for loop in poly.loop_indices:
                co=obj.matrix_world @ obj.data.vertices[obj.data.loops[loop].vertex_index].co
                uv.data[loop].uv=(co[axes[0]]*.5,co[axes[1]]*.5)
    finish_normals()
