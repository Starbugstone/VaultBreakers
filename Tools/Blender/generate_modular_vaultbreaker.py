"""Generate the canonical modular Vaultbreaker source asset and Unity FBX export.

Run with:
    blender --background --python Tools/Blender/generate_modular_vaultbreaker.py

The generated character is deliberately constructed from rigid, bone-attached pieces.
This matches the chunky salvage-rig design, keeps equipment swaps independent, and
avoids skin-weight seams while the gameplay proportions and animation set evolve.
"""

from pathlib import Path
import math

import bpy
from mathutils import Euler, Matrix, Vector


# Blender -Y is the model front and maps to Unity +Z, so an empty left at Blender identity
# arrives in Unity pointing at the sky. Sockets are authored a quarter turn around X so that
# Unity receives an identity rotation: local +Z is gameplay facing and local +Y is up.
SOCKET_ALIGNMENT = Euler((math.pi / 2.0, 0.0, 0.0), "XYZ").to_matrix().to_4x4()

PROJECT_ROOT = Path(__file__).resolve().parents[2]
SOURCE_PATH = PROJECT_ROOT / "ArtSource" / "Blender" / "Characters" / "Player" / "Vaultbreaker_Modular.blend"
FBX_PATH = PROJECT_ROOT / "Assets" / "Vaultbreakers" / "Art" / "Characters" / "Player" / "Vaultbreaker_Modular.fbx"
PREVIEW_PATH = PROJECT_ROOT / "Docs" / "Images" / "Vaultbreaker_Modular_Preview.png"


def reset_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.armatures, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for block in list(datablocks):
            if block.users == 0:
                datablocks.remove(block)


def make_material(name, color, metallic=0.0, roughness=0.5, emission=None, emission_strength=0.0):
    material = bpy.data.materials.new(name)
    material.diffuse_color = (*color, 1.0)
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if emission:
        emission_input = bsdf.inputs.get("Emission Color") or bsdf.inputs.get("Emission")
        strength_input = bsdf.inputs.get("Emission Strength")
        if emission_input:
            emission_input.default_value = (*emission, 1.0)
        if strength_input:
            strength_input.default_value = emission_strength
    return material


def finish_mesh(obj, material, bevel=0.025):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel > 0:
        modifier = obj.modifiers.new("EdgeSoftening", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    if material:
        obj.data.materials.append(material)
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    obj["vb_export"] = True
    return obj


def add_box(name, location, dimensions, material, bevel=0.025, rotation=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    return finish_mesh(obj, material, bevel)


def add_sphere(name, location, scale, material, segments=20, rings=12):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    return finish_mesh(obj, material, 0.01)


def add_cylinder(name, location, radius, depth, material, vertices=16, rotation=(0.0, 0.0, 0.0), bevel=0.015):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    return finish_mesh(obj, material, bevel)


def add_cylinder_between(name, start, end, radius, material, vertices=16):
    start = Vector(start)
    end = Vector(end)
    direction = end - start
    obj = add_cylinder(name, (start + end) * 0.5, radius, direction.length, material, vertices, bevel=0.012)
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = Vector((0.0, 0.0, 1.0)).rotation_difference(direction.normalized())
    return obj


def add_torus(name, location, major_radius, minor_radius, material, rotation=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_torus_add(
        major_radius=major_radius,
        minor_radius=minor_radius,
        major_segments=20,
        minor_segments=8,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    return finish_mesh(obj, material, 0.005)


def create_armature():
    armature_data = bpy.data.armatures.new("VB_Rig")
    armature = bpy.data.objects.new("VB_Rig", armature_data)
    bpy.context.collection.objects.link(armature)
    armature.show_in_front = True
    armature["vb_export"] = True
    bpy.context.view_layer.objects.active = armature
    armature.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")

    def bone(name, head, tail, parent=None):
        result = armature_data.edit_bones.new(name)
        result.head = head
        result.tail = tail
        if parent:
            result.parent = armature_data.edit_bones[parent]
        return result

    bone("Root", (0, 0, 0), (0, 0, 0.12))
    bone("Hips", (0, 0, 0.82), (0, 0, 1.0), "Root")
    bone("Spine", (0, 0, 1.0), (0, 0, 1.26), "Hips")
    bone("Chest", (0, 0, 1.26), (0, 0, 1.52), "Spine")
    bone("Neck", (0, 0, 1.52), (0, 0, 1.64), "Chest")
    bone("Head", (0, 0, 1.64), (0, 0, 1.88), "Neck")

    for side, sign in (("L", 1), ("R", -1)):
        bone(f"UpperArm_{side}", (0.22 * sign, 0, 1.47), (0.55 * sign, 0, 1.40), "Chest")
        bone(f"LowerArm_{side}", (0.55 * sign, 0, 1.40), (0.82 * sign, 0, 1.32), f"UpperArm_{side}")
        bone(f"Hand_{side}", (0.82 * sign, 0, 1.32), (1.00 * sign, 0, 1.27), f"LowerArm_{side}")
        bone(f"UpperLeg_{side}", (0.14 * sign, 0, 0.90), (0.16 * sign, 0, 0.50), "Hips")
        bone(f"LowerLeg_{side}", (0.16 * sign, 0, 0.50), (0.15 * sign, 0, 0.13), f"UpperLeg_{side}")
        bone(f"Foot_{side}", (0.15 * sign, 0, 0.13), (0.15 * sign, -0.20, 0.07), f"LowerLeg_{side}")

    bpy.ops.object.mode_set(mode="OBJECT")
    return armature


def bone_parent(obj, armature, bone_name, world=None):
    # matrix_world is only trustworthy after the dependency graph has evaluated the object,
    # and the bone-relative basis is only known after the new parent has been evaluated too.
    # Objects created with bpy.data.objects.new() are still at identity until this runs.
    if world is None:
        bpy.context.view_layer.update()
        world = obj.matrix_world.copy()

    obj.parent = armature
    obj.parent_type = "BONE"
    obj.parent_bone = bone_name
    bpy.context.view_layer.update()
    obj.matrix_world = world
    obj["vb_bone"] = bone_name
    return obj


def mark_variant(obj, slot, variant, is_default=False):
    obj["vb_slot"] = slot
    obj["vb_variant"] = variant
    obj["vb_default"] = is_default
    obj.hide_render = not is_default
    return obj


def make_socket(name, armature, bone_name, location, rotation=(0.0, 0.0, 0.0)):
    socket = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(socket)
    socket.empty_display_type = "ARROWS"
    socket.empty_display_size = 0.12
    socket["vb_export"] = True
    socket["vb_socket"] = True
    world = Matrix.Translation(location) @ Euler(rotation, "XYZ").to_matrix().to_4x4() @ SOCKET_ALIGNMENT
    return bone_parent(socket, armature, bone_name, world)


def tag_and_parent(objects, armature, bone_name, slot=None, variant=None, is_default=False):
    for obj in objects:
        bone_parent(obj, armature, bone_name)
        if slot and variant:
            mark_variant(obj, slot, variant, is_default)


def create_character():
    reset_scene()

    mats = {
        "undersuit": make_material("VB_Undersuit", (0.035, 0.045, 0.055), 0.15, 0.38),
        "metal": make_material("VB_SalvageMetal", (0.18, 0.22, 0.24), 0.72, 0.28),
        "dark": make_material("VB_DarkMetal", (0.045, 0.055, 0.065), 0.82, 0.22),
        "orange": make_material("VB_HazardOrange", (0.88, 0.24, 0.045), 0.32, 0.30),
        "bone": make_material("VB_Ceramic", (0.63, 0.64, 0.57), 0.25, 0.36),
        "cyan": make_material("VB_EnergyCyan", (0.02, 0.48, 0.68), 0.15, 0.20, (0.0, 0.72, 1.0), 5.0),
        "violet": make_material("VB_EnergyViolet", (0.40, 0.07, 0.62), 0.12, 0.22, (0.65, 0.08, 1.0), 4.0),
        "visor": make_material("VB_Visor", (0.015, 0.16, 0.20), 0.55, 0.08, (0.0, 0.62, 0.78), 3.0),
    }

    armature = create_armature()

    # Permanent undersuit and mechanical body. Every piece follows the canonical skeleton.
    body_specs = [
        ("BASE_Torso", "Chest", add_box("BASE_Torso", (0, 0.015, 1.34), (0.40, 0.24, 0.47), mats["undersuit"], 0.07)),
        ("BASE_Pelvis", "Hips", add_box("BASE_Pelvis", (0, 0.015, 0.91), (0.37, 0.25, 0.22), mats["dark"], 0.06)),
        ("BASE_Neck", "Neck", add_cylinder("BASE_Neck", (0, 0, 1.58), 0.09, 0.14, mats["undersuit"])),
        ("BASE_Head", "Head", add_sphere("BASE_Head", (0, 0, 1.73), (0.145, 0.13, 0.18), mats["undersuit"])),
    ]
    for _, bone_name, obj in body_specs:
        bone_parent(obj, armature, bone_name)

    for side, sign in (("L", 1), ("R", -1)):
        upper_arm = add_cylinder_between(f"BASE_UpperArm_{side}", (0.27 * sign, 0, 1.46), (0.53 * sign, 0, 1.40), 0.085, mats["undersuit"])
        lower_arm = add_cylinder_between(f"BASE_LowerArm_{side}", (0.57 * sign, 0, 1.39), (0.80 * sign, 0, 1.32), 0.078, mats["undersuit"])
        hand = add_box(f"BASE_Hand_{side}", (0.89 * sign, -0.005, 1.29), (0.18, 0.11, 0.12), mats["dark"], 0.035)
        upper_leg = add_cylinder_between(f"BASE_UpperLeg_{side}", (0.14 * sign, 0, 0.84), (0.16 * sign, 0, 0.53), 0.105, mats["undersuit"])
        lower_leg = add_cylinder_between(f"BASE_LowerLeg_{side}", (0.16 * sign, 0, 0.46), (0.15 * sign, 0, 0.17), 0.092, mats["undersuit"])
        knee = add_sphere(f"BASE_Knee_{side}", (0.16 * sign, -0.005, 0.50), (0.11, 0.105, 0.10), mats["dark"])
        boot = add_box(f"BASE_Boot_{side}", (0.15 * sign, -0.08, 0.10), (0.22, 0.36, 0.18), mats["dark"], 0.045)
        tag_and_parent([upper_arm], armature, f"UpperArm_{side}")
        tag_and_parent([lower_arm], armature, f"LowerArm_{side}")
        tag_and_parent([hand], armature, f"Hand_{side}")
        tag_and_parent([upper_leg], armature, f"UpperLeg_{side}")
        tag_and_parent([lower_leg, knee], armature, f"LowerLeg_{side}")
        tag_and_parent([boot], armature, f"Foot_{side}")

    # Base rig details remain visible through every equipment combination.
    chest_core = add_cylinder("BASE_RigCore", (0, -0.145, 1.35), 0.085, 0.045, mats["cyan"], 20, (math.pi / 2, 0, 0), 0.008)
    belt = add_torus("BASE_BeltRing", (0, 0, 0.96), 0.205, 0.025, mats["orange"])
    spine_pack = add_box("BASE_SpineRail", (0, 0.145, 1.31), (0.12, 0.08, 0.43), mats["metal"], 0.02)
    tag_and_parent([chest_core, spine_pack], armature, "Chest")
    tag_and_parent([belt], armature, "Hips")

    # Helmet variants.
    scrapper_helmet = [
        add_box("VAR_Helmet_Scrapper_Crown", (0, 0.005, 1.79), (0.34, 0.29, 0.27), mats["metal"], 0.055),
        add_box("VAR_Helmet_Scrapper_Visor", (0, -0.146, 1.76), (0.26, 0.035, 0.095), mats["visor"], 0.015),
        add_box("VAR_Helmet_Scrapper_Brow", (0, -0.158, 1.83), (0.34, 0.045, 0.045), mats["orange"], 0.012),
        add_cylinder("VAR_Helmet_Scrapper_Antenna", (0.13, 0.01, 1.99), 0.018, 0.19, mats["dark"], 10),
        add_sphere("VAR_Helmet_Scrapper_AntennaTip", (0.13, 0.01, 2.09), (0.035, 0.035, 0.035), mats["cyan"], 12, 8),
    ]
    tag_and_parent(scrapper_helmet, armature, "Head", "Helmet", "Scrapper", True)

    sentinel_helmet = [
        add_box("VAR_Helmet_Sentinel_Crown", (0, 0.01, 1.80), (0.37, 0.31, 0.30), mats["bone"], 0.07),
        add_box("VAR_Helmet_Sentinel_Faceplate", (0, -0.158, 1.75), (0.29, 0.04, 0.16), mats["dark"], 0.02),
        add_box("VAR_Helmet_Sentinel_Visor", (0, -0.183, 1.80), (0.23, 0.018, 0.045), mats["violet"], 0.007),
        add_box("VAR_Helmet_Sentinel_Crest", (0, 0.01, 2.00), (0.075, 0.22, 0.16), mats["orange"], 0.025),
    ]
    tag_and_parent(sentinel_helmet, armature, "Head", "Helmet", "Sentinel", False)

    # Armor variants. Limb pieces remain bone-attached, so one variant can span the shared skeleton safely.
    scrapper_chest = [
        add_box("VAR_Armor_Scrapper_ChestPlate", (0, -0.13, 1.38), (0.46, 0.09, 0.37), mats["metal"], 0.055),
        add_box("VAR_Armor_Scrapper_ChestStripe", (0.10, -0.182, 1.38), (0.085, 0.025, 0.30), mats["orange"], 0.012, (0, 0, -0.10)),
        add_box("VAR_Armor_Scrapper_Collar", (0, -0.02, 1.55), (0.35, 0.25, 0.10), mats["dark"], 0.035),
    ]
    tag_and_parent(scrapper_chest, armature, "Chest", "Armor", "Scrapper", True)
    for side, sign in (("L", 1), ("R", -1)):
        shoulder = add_box(f"VAR_Armor_Scrapper_Shoulder_{side}", (0.31 * sign, 0.0, 1.47), (0.24, 0.28, 0.18), mats["metal"], 0.055, (0, 0, -0.12 * sign))
        forearm = add_box(f"VAR_Armor_Scrapper_Forearm_{side}", (0.68 * sign, -0.012, 1.35), (0.25, 0.18, 0.18), mats["bone"], 0.035)
        shin = add_box(f"VAR_Armor_Scrapper_Shin_{side}", (0.15 * sign, -0.075, 0.31), (0.18, 0.11, 0.31), mats["metal"], 0.035)
        tag_and_parent([shoulder], armature, f"UpperArm_{side}", "Armor", "Scrapper", True)
        tag_and_parent([forearm], armature, f"LowerArm_{side}", "Armor", "Scrapper", True)
        tag_and_parent([shin], armature, f"LowerLeg_{side}", "Armor", "Scrapper", True)

    bulwark_chest = [
        add_box("VAR_Armor_Bulwark_ChestPlate", (0, -0.145, 1.38), (0.55, 0.13, 0.43), mats["bone"], 0.075),
        add_box("VAR_Armor_Bulwark_CoreGuard", (0, -0.225, 1.36), (0.20, 0.055, 0.21), mats["dark"], 0.035),
        add_box("VAR_Armor_Bulwark_Collar", (0, -0.01, 1.57), (0.43, 0.31, 0.13), mats["metal"], 0.045),
    ]
    tag_and_parent(bulwark_chest, armature, "Chest", "Armor", "Bulwark", False)
    for side, sign in (("L", 1), ("R", -1)):
        shoulder = add_sphere(f"VAR_Armor_Bulwark_Shoulder_{side}", (0.33 * sign, 0, 1.47), (0.18, 0.19, 0.15), mats["bone"], 18, 10)
        pauldron = add_box(f"VAR_Armor_Bulwark_Pauldron_{side}", (0.38 * sign, -0.02, 1.49), (0.25, 0.31, 0.11), mats["metal"], 0.045, (0, 0, -0.10 * sign))
        thigh = add_box(f"VAR_Armor_Bulwark_Thigh_{side}", (0.15 * sign, -0.055, 0.70), (0.22, 0.17, 0.31), mats["bone"], 0.045)
        tag_and_parent([shoulder, pauldron], armature, f"UpperArm_{side}", "Armor", "Bulwark", False)
        tag_and_parent([thigh], armature, f"UpperLeg_{side}", "Armor", "Bulwark", False)

    # Melee variants on the right-hand socket standard.
    hammer = [
        add_cylinder_between("VAR_Melee_ScrapHammer_Handle", (-0.91, -0.02, 1.23), (-0.91, -0.02, 0.72), 0.035, mats["dark"], 12),
        add_box("VAR_Melee_ScrapHammer_Head", (-0.91, -0.02, 0.68), (0.48, 0.22, 0.22), mats["metal"], 0.045),
        add_box("VAR_Melee_ScrapHammer_Strike", (-0.91, -0.145, 0.68), (0.34, 0.07, 0.14), mats["orange"], 0.018),
        add_cylinder("VAR_Melee_ScrapHammer_Core", (-0.91, 0.10, 0.68), 0.065, 0.05, mats["cyan"], 16, (math.pi / 2, 0, 0), 0.008),
    ]
    tag_and_parent(hammer, armature, "Hand_R", "Melee", "ScrapHammer", True)

    cutter = [
        add_box("VAR_Melee_PlasmaCutter_Grip", (-0.91, -0.01, 1.22), (0.10, 0.12, 0.29), mats["dark"], 0.025),
        add_box("VAR_Melee_PlasmaCutter_Hilt", (-0.91, -0.01, 1.09), (0.31, 0.16, 0.10), mats["metal"], 0.025),
        add_box("VAR_Melee_PlasmaCutter_Blade", (-0.91, -0.01, 0.73), (0.10, 0.055, 0.64), mats["violet"], 0.025),
    ]
    tag_and_parent(cutter, armature, "Hand_R", "Melee", "PlasmaCutter", False)

    # Ranged variants and a separately swappable shield emitter share the left arm.
    pulse = [
        add_box("VAR_Ranged_PulseCaster_Body", (0.79, -0.08, 1.31), (0.39, 0.23, 0.22), mats["metal"], 0.045),
        add_cylinder("VAR_Ranged_PulseCaster_Barrel", (0.98, -0.09, 1.31), 0.065, 0.31, mats["dark"], 16, (0, math.pi / 2, 0), 0.012),
        add_torus("VAR_Ranged_PulseCaster_Coil", (0.88, -0.09, 1.31), 0.095, 0.018, mats["cyan"], (0, math.pi / 2, 0)),
    ]
    tag_and_parent(pulse, armature, "LowerArm_L", "Ranged", "PulseCaster", True)

    arc = [
        add_box("VAR_Ranged_ArcBlaster_Body", (0.77, -0.08, 1.31), (0.35, 0.28, 0.27), mats["bone"], 0.055),
        add_cylinder("VAR_Ranged_ArcBlaster_BarrelTop", (0.98, -0.15, 1.36), 0.042, 0.28, mats["dark"], 12, (0, math.pi / 2, 0), 0.01),
        add_cylinder("VAR_Ranged_ArcBlaster_BarrelBottom", (0.98, -0.02, 1.26), 0.042, 0.28, mats["dark"], 12, (0, math.pi / 2, 0), 0.01),
        add_sphere("VAR_Ranged_ArcBlaster_Cell", (0.75, 0.08, 1.31), (0.07, 0.055, 0.12), mats["violet"], 16, 10),
    ]
    tag_and_parent(arc, armature, "LowerArm_L", "Ranged", "ArcBlaster", False)

    emitter = [
        add_cylinder("VAR_Shield_AegisEmitter_Ring", (0.67, -0.17, 1.35), 0.12, 0.045, mats["metal"], 20, (math.pi / 2, 0, 0), 0.012),
        add_cylinder("VAR_Shield_AegisEmitter_Core", (0.67, -0.20, 1.35), 0.067, 0.035, mats["cyan"], 18, (math.pi / 2, 0, 0), 0.006),
        add_box("VAR_Shield_AegisEmitter_Brace", (0.67, -0.10, 1.35), (0.25, 0.13, 0.12), mats["dark"], 0.025),
    ]
    tag_and_parent(emitter, armature, "LowerArm_L", "Shield", "AegisEmitter", True)

    prism_emitter = [
        add_box("VAR_Shield_PrismEmitter_Frame", (0.67, -0.18, 1.35), (0.23, 0.055, 0.23), mats["bone"], 0.035, (0, 0, math.pi / 4)),
        add_sphere("VAR_Shield_PrismEmitter_Core", (0.67, -0.22, 1.35), (0.065, 0.035, 0.065), mats["violet"], 16, 10),
    ]
    tag_and_parent(prism_emitter, armature, "LowerArm_L", "Shield", "PrismEmitter", False)

    # Back-rig variants provide a future skin/rig equipment seam.
    recycler = [
        add_box("VAR_Rig_Reclaimer_Pack", (0, 0.205, 1.35), (0.31, 0.18, 0.39), mats["dark"], 0.055),
        add_cylinder("VAR_Rig_Reclaimer_Canister_L", (0.11, 0.30, 1.34), 0.055, 0.30, mats["orange"], 14),
        add_cylinder("VAR_Rig_Reclaimer_Canister_R", (-0.11, 0.30, 1.34), 0.055, 0.30, mats["metal"], 14),
        add_box("VAR_Rig_Reclaimer_Light", (0, 0.305, 1.48), (0.13, 0.025, 0.045), mats["cyan"], 0.008),
    ]
    tag_and_parent(recycler, armature, "Chest", "Rig", "Reclaimer", True)

    capacitor = [
        add_box("VAR_Rig_Capacitor_Pack", (0, 0.22, 1.35), (0.35, 0.21, 0.42), mats["bone"], 0.065),
        add_torus("VAR_Rig_Capacitor_Coil", (0, 0.34, 1.36), 0.13, 0.025, mats["violet"], (math.pi / 2, 0, 0)),
        add_box("VAR_Rig_Capacitor_Vent", (0, 0.34, 1.16), (0.22, 0.04, 0.08), mats["dark"], 0.018),
    ]
    tag_and_parent(capacitor, armature, "Chest", "Rig", "Capacitor", False)

    # Canonical, invariant equipment and VFX sockets.
    make_socket("SOCKET_RightHand_Melee", armature, "Hand_R", (-0.91, -0.02, 1.25))
    make_socket("SOCKET_LeftArm_RangedShield", armature, "LowerArm_L", (0.78, -0.09, 1.32))
    make_socket("SOCKET_Back", armature, "Chest", (0, 0.24, 1.36))
    make_socket("SOCKET_PetAnchor", armature, "Chest", (0.45, 0.25, 1.55))
    make_socket("ANCHOR_Muzzle", armature, "LowerArm_L", (1.13, -0.09, 1.31))
    make_socket("ANCHOR_Shield", armature, "LowerArm_L", (0.67, -0.24, 1.35))
    make_socket("ANCHOR_MeleeTrail", armature, "Hand_R", (-0.91, -0.02, 0.68))
    make_socket("ANCHOR_Hit", armature, "Chest", (0, -0.22, 1.35))
    make_socket("ANCHOR_Feet", armature, "Root", (0, 0, 0.02))

    armature["vb_schema_version"] = 1
    armature["vb_units"] = "1 Blender unit = 1 meter"
    armature["vb_forward"] = "Blender -Y / Unity +Z"
    return armature, mats


def setup_preview(materials):
    # Preview-only stage objects are not exported to Unity.
    floor = add_cylinder("_PREVIEW_Plinth", (0, 0, -0.06), 0.78, 0.10, materials["dark"], 48, bevel=0.025)
    floor["vb_export"] = False

    bpy.ops.object.light_add(type="AREA", location=(3.2, -4.0, 4.2))
    key = bpy.context.object
    key.name = "_PREVIEW_Key"
    key.data.energy = 900
    key.data.shape = "DISK"
    key.data.size = 3.0

    bpy.ops.object.light_add(type="AREA", location=(-3.0, -1.2, 2.8))
    fill = bpy.context.object
    fill.name = "_PREVIEW_Fill"
    fill.data.energy = 550
    fill.data.color = (0.12, 0.45, 1.0)
    fill.data.size = 2.0

    bpy.ops.object.light_add(type="AREA", location=(0.5, 3.0, 3.2))
    rim = bpy.context.object
    rim.name = "_PREVIEW_Rim"
    rim.data.energy = 800
    rim.data.color = (1.0, 0.18, 0.04)
    rim.data.size = 2.5

    bpy.ops.object.camera_add(location=(3.55, -5.3, 2.75))
    camera = bpy.context.object
    camera.name = "_PREVIEW_Camera"
    target = Vector((0, 0, 1.05))
    camera.rotation_euler = (target - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera.data.lens = 58
    bpy.context.scene.camera = camera

    scene = bpy.context.scene
    # Blender 5 exposes the Eevee Next implementation under the BLENDER_EEVEE id.
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 768
    scene.render.resolution_y = 768
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.world.color = (0.008, 0.011, 0.018)
    scene.render.filepath = str(PREVIEW_PATH)


def export_character():
    bpy.ops.object.select_all(action="DESELECT")
    export_objects = [obj for obj in bpy.context.scene.objects if obj.get("vb_export", False)]
    for obj in export_objects:
        obj.select_set(True)
    if export_objects:
        bpy.context.view_layer.objects.active = export_objects[0]

    FBX_PATH.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=str(FBX_PATH),
        use_selection=True,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        use_space_transform=True,
        bake_space_transform=False,
        add_leaf_bones=False,
        use_armature_deform_only=False,
        mesh_smooth_type="FACE",
        use_mesh_modifiers=True,
        use_triangles=False,
        bake_anim=False,
        path_mode="AUTO",
        embed_textures=False,
    )


def main():
    armature, materials = create_character()
    setup_preview(materials)

    SOURCE_PATH.parent.mkdir(parents=True, exist_ok=True)
    PREVIEW_PATH.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE_PATH))
    export_character()
    bpy.context.scene.render.filepath = str(PREVIEW_PATH)
    bpy.ops.render.render(write_still=True)

    print(f"Generated Blender source: {SOURCE_PATH}")
    print(f"Generated Unity FBX: {FBX_PATH}")
    print(f"Generated preview: {PREVIEW_PATH}")
    print(f"Export object count: {sum(1 for obj in bpy.context.scene.objects if obj.get('vb_export', False))}")


if __name__ == "__main__":
    main()
