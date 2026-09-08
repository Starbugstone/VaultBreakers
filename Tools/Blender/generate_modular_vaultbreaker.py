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
    # Schema-1 helpers remain here; the current visual design lives in stylized_assets.
    import dungeon_assets
    return dungeon_assets.player()


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
    import sys
    sys.path.insert(0, str(Path(__file__).resolve().parent))
    import dungeon_assets
    dungeon_assets.main()


if __name__ == "__main__":
    main()
