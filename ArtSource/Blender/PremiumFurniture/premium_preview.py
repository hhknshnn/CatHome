"""Headless approval renders for the premium furniture wave.

Produces a three-quarter hero view plus a flat side view, so silhouette
problems are visible before anything reaches Unity.
"""

import math
import os

import bpy
from mathutils import Vector

from premium_kit import PREVIEW_FOLDER, srgb_to_linear


def look_at(obj, point):
    direction = Vector(point) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def preview_material(name, color, metallic=0.0, roughness=0.4):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    shader = material.node_tree.nodes.get("Principled BSDF")
    linear = tuple(srgb_to_linear(c) for c in color)
    shader.inputs["Base Color"].default_value = (*linear, 1.0)
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = roughness
    return material


def pick_eevee():
    engines = bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items.keys()
    for candidate in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE"):
        if candidate in engines:
            return candidate
    return "CYCLES"


def build_stage(height):
    bpy.ops.mesh.primitive_plane_add(size=24, location=(0, 0, -0.02))
    bpy.context.object.data.materials.append(
        preview_material("PreviewFloor", (0.36, 0.78, 0.80), 0.0, 0.42))

    bpy.ops.mesh.primitive_plane_add(size=24, location=(0, -2.6, 0), rotation=(math.pi / 2, 0, 0))
    bpy.context.object.data.materials.append(
        preview_material("PreviewBackdrop", (0.92, 0.80, 0.98), 0.0, 0.5))

    bpy.ops.object.light_add(type="AREA", location=(-3.2, 3.6, 4.6))
    key = bpy.context.object
    key.data.energy = 900
    key.data.shape = "DISK"
    key.data.size = 5.0
    key.data.color = (1.0, 0.86, 0.74)
    look_at(key, (0, 0, height * 0.6))

    bpy.ops.object.light_add(type="AREA", location=(4.0, 2.4, 3.4))
    fill = bpy.context.object
    fill.data.energy = 520
    fill.data.size = 4.0
    fill.data.color = (0.62, 0.92, 1.0)
    look_at(fill, (0, 0, height * 0.5))

    bpy.ops.object.light_add(type="AREA", location=(0.0, -3.2, 4.2))
    rim = bpy.context.object
    rim.data.energy = 620
    rim.data.size = 3.0
    rim.data.color = (1.0, 0.72, 0.86)
    look_at(rim, (0, 0, height * 0.7))

    world = bpy.context.scene.world
    if world is None:
        world = bpy.data.worlds.new("PreviewWorld")
        bpy.context.scene.world = world
    world.use_nodes = True
    background = world.node_tree.nodes.get("Background")
    if background is not None:
        background.inputs["Color"].default_value = (0.05, 0.10, 0.16, 1.0)
        background.inputs["Strength"].default_value = 0.9


def bounds(obj):
    corners = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    lo = Vector((min(c.x for c in corners), min(c.y for c in corners), min(c.z for c in corners)))
    hi = Vector((max(c.x for c in corners), max(c.y for c in corners), max(c.z for c in corners)))
    return lo, hi, (lo + hi) * 0.5, (hi - lo)


def render_views(obj, name, front_plus_z=False):
    """Renders <name>_Hero.png and <name>_Side.png framed on the model.

    `front_plus_z` is for products whose open face is authored at +Z (wall units
    placed with a 90/270 yaw); without it the cameras look at their backs.
    """
    os.makedirs(PREVIEW_FOLDER, exist_ok=True)
    # Models are authored in Unity-style Y-up, but the preview stage (floor,
    # backdrop, key light) is Blender Z-up. Stand the model up for the render
    # only; the rotation is never applied, so the exported mesh is untouched.
    kept_rotation = tuple(obj.rotation_euler)
    obj.rotation_euler = (math.pi / 2, 0.0, math.pi if front_plus_z else 0.0)
    bpy.context.view_layer.update()
    lo, hi, center, size = bounds(obj)
    height = hi.z
    build_stage(height)

    scene = bpy.context.scene
    scene.render.engine = pick_eevee()
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 960
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False

    # Frame on the model's own bounds so small props do not read as specks.
    target = (center.x, center.y, center.z)
    radius = max(size.x, size.y, size.z) * 0.5
    distance = radius * 4.0 + 0.7

    bpy.ops.object.camera_add(location=(
        center.x + distance * 0.62, center.y + distance * 0.78, center.z + radius * 0.75))
    hero = bpy.context.object
    hero.data.lens = 45
    look_at(hero, target)

    # Straight-on front view: the angle the player sees in the room.
    bpy.ops.object.camera_add(location=(center.x, center.y + distance * 1.1, center.z))
    front = bpy.context.object
    front.data.type = "ORTHO"
    aspect = scene.render.resolution_x / float(scene.render.resolution_y)
    front.data.ortho_scale = max(size.x, size.z * aspect) * 1.12
    look_at(front, target)

    # Profile silhouette: flat fill on white, so the shape reads on its own.
    bpy.ops.object.camera_add(location=(center.x + distance * 1.1, center.y, center.z))
    side = bpy.context.object
    side.data.type = "ORTHO"
    side.data.ortho_scale = max(size.y, size.z * aspect) * 1.2
    look_at(side, target)

    outputs = []
    for camera, suffix in ((hero, "Hero"), (front, "Front")):
        scene.camera = camera
        path = os.path.join(PREVIEW_FOLDER, "%s_%s.png" % (name, suffix))
        scene.render.filepath = path
        bpy.ops.render.render(write_still=True)
        outputs.append(path)
        print("[premium] wrote " + path)

    outputs.append(render_silhouette(obj, side, name))
    obj.rotation_euler = kept_rotation
    bpy.context.view_layer.update()
    return outputs


def render_silhouette(obj, camera, name):
    """Flat profile: everything else hidden, object filled, white ground."""
    scene = bpy.context.scene
    for other in bpy.data.objects:
        if other.type in {"MESH", "LIGHT"} and other is not obj:
            other.hide_render = True

    flat = bpy.data.materials.new("SilhouetteFill")
    flat.use_nodes = True
    nodes = flat.node_tree.nodes
    links = flat.node_tree.links
    for node in list(nodes):
        if node.type != "OUTPUT_MATERIAL":
            nodes.remove(node)
    emission = nodes.new("ShaderNodeEmission")
    emission.inputs["Color"].default_value = (0.10, 0.13, 0.20, 1.0)
    links.new(emission.outputs["Emission"], nodes["Material Output"].inputs["Surface"])

    kept = [slot.material for slot in obj.material_slots]
    for slot in obj.material_slots:
        slot.material = flat

    world = scene.world
    background = world.node_tree.nodes.get("Background")
    kept_world = None
    if background is not None:
        kept_world = (background.inputs["Color"].default_value[:], background.inputs["Strength"].default_value)
        background.inputs["Color"].default_value = (1.0, 1.0, 1.0, 1.0)
        background.inputs["Strength"].default_value = 1.0

    scene.camera = camera
    path = os.path.join(PREVIEW_FOLDER, "%s_Silhouette.png" % name)
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print("[premium] wrote " + path)

    for slot, material in zip(obj.material_slots, kept):
        slot.material = material
    if background is not None and kept_world is not None:
        background.inputs["Color"].default_value = kept_world[0]
        background.inputs["Strength"].default_value = kept_world[1]
    for other in bpy.data.objects:
        other.hide_render = False
    return path
