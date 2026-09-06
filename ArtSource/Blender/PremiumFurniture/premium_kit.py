"""Shared Blender helpers for the CatHome premium furniture wave.

Construction is written in Unity-style Y-up coordinates so dimensions read the
same as the catalog entries in StoreCatalogAssets.cs. `join_fixture` converts
the finished object to Blender Z-up before `export_fixture` runs the exporter's
own Blender-Z -> Unity-Y conversion, exactly like the bathroom kit does.

Material names must stay in the canonical CH_* set: StoreProductContentBuilder
swaps FBX slots for the existing URP materials by name.
"""

import math
import os

import bpy
from mathutils import Quaternion, Vector

ROOT = os.path.dirname(os.path.abspath(__file__))
UNITY_MODELS = os.path.abspath(
    os.path.join(ROOT, "..", "..", "..", "Assets", "Art", "PremiumFurniture", "Models")
)
PREVIEW_FOLDER = os.path.join(ROOT, "Previews")

# Base colors mirrored from Assets/Art/StoreProducts/Materials/CH_*.mat so the
# preview render matches what the room will show. Values there are sRGB.
CH_COLORS = {
    "CH_CoralBright": (1.0, 0.470588, 0.509804),
    "CH_Cream": (1.0, 0.831373, 0.560784),
    "CH_White": (1.0, 0.972549, 0.901961),
    "CH_Gold": (0.882353, 0.521569, 0.090196),
    "CH_AquaBright": (0.262745, 0.862745, 0.827451),
    "CH_MintBright": (0.494118, 0.921569, 0.745098),
    "CH_LilacBright": (0.780392, 0.690196, 1.0),
    "CH_LemonBright": (1.0, 0.898039, 0.415686),
    "CH_Pink": (1.0, 0.658824, 0.803922),
    "CH_Ink": (0.094118, 0.078431, 0.117647),
    "CH_TealLight": (0.188235, 0.678431, 0.650980),
    "CH_Teal": (0.058824, 0.478431, 0.458824),
    "CH_Screen": (0.058824, 0.188235, 0.250980),
}

METALLIC = {"CH_Gold"}


def srgb_to_linear(channel):
    if channel <= 0.04045:
        return channel / 12.92
    return ((channel + 0.055) / 1.055) ** 2.4


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.materials):
        for block in list(datablocks):
            if block.users == 0:
                datablocks.remove(block)


def palette(names):
    """Create Principled materials named exactly like the Unity CH_* set."""
    materials = {}
    for name in names:
        color = tuple(srgb_to_linear(c) for c in CH_COLORS[name])
        material = bpy.data.materials.get(name) or bpy.data.materials.new(name)
        material.use_nodes = True
        material.diffuse_color = (*color, 1.0)
        shader = material.node_tree.nodes.get("Principled BSDF")
        shader.inputs["Base Color"].default_value = (*color, 1.0)
        shader.inputs["Metallic"].default_value = 0.45 if name in METALLIC else 0.0
        shader.inputs["Roughness"].default_value = 0.22 if name in METALLIC else 0.36
        if "Coat Weight" in shader.inputs:
            shader.inputs["Coat Weight"].default_value = 0.3
            shader.inputs["Coat Roughness"].default_value = 0.17
        materials[name] = material
    return materials


def apply_all(obj):
    bpy.ops.object.select_all(action="DESELECT")
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    obj.select_set(False)


def apply_modifier(obj, modifier):
    bpy.ops.object.select_all(action="DESELECT")
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    obj.select_set(False)


def bevel(obj, width=0.06, segments=4):
    if width <= 0:
        return
    modifier = obj.modifiers.new("CandyBevel", "BEVEL")
    modifier.width = width
    modifier.segments = segments
    modifier.limit_method = "ANGLE"
    apply_modifier(obj, modifier)


def solidify(obj, thickness, offset=0.0):
    modifier = obj.modifiers.new("Thickness", "SOLIDIFY")
    modifier.thickness = thickness
    modifier.offset = offset
    apply_modifier(obj, modifier)


def smooth(obj):
    for polygon in obj.data.polygons:
        polygon.use_smooth = True


def finish(obj, material, width=0.05, segments=4):
    apply_all(obj)
    bevel(obj, width, segments)
    obj.data.materials.append(material)
    smooth(obj)
    return obj


def cube(name, location, dimensions, material, width=0.06, rotation=(0, 0, 0), segments=5):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    return finish(obj, material, min(width, min(dimensions) * 0.42), segments)


def cylinder(name, location, radius, depth, material, axis="Y", scale=(1, 1, 1),
             vertices=32, width=0.02, rotation=None):
    """Cylinders are authored around the requested Unity-style axis."""
    base = {
        "X": (0, math.pi / 2, 0),
        "Y": (math.pi / 2, 0, 0),
        "Z": (0, 0, 0),
    }[axis]
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices, radius=radius, depth=depth, location=location,
        rotation=rotation if rotation is not None else base)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    return finish(obj, material, width, 3)


def sphere(name, location, scale, material, segments=32, rings=16, rotation=None):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    if rotation is not None:
        obj.rotation_euler = rotation
    return finish(obj, material, 0.0, 0)


def aim_euler(yaw, droop):
    """Euler that spins a part to `yaw` and then droops it about its own side.

    Blender's XYZ euler applies X, then Y, then Z, so a droop written straight
    into the tuple would tilt every part about the same world axis regardless of
    its yaw. Composing the quaternions keeps the droop local to each part.
    """
    spin = Quaternion((0.0, 1.0, 0.0), yaw)
    tilt = Quaternion((0.0, 0.0, 1.0), droop)
    return (spin @ tilt).to_euler()


def torus(name, location, major, minor, material, scale=(1, 1, 1), rotation=(math.pi / 2, 0, 0),
          major_segments=44, minor_segments=12):
    bpy.ops.mesh.primitive_torus_add(
        major_radius=major, minor_radius=minor,
        major_segments=major_segments, minor_segments=minor_segments,
        location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    return finish(obj, material, 0.0, 0)


def strut(name, foot, tip, radius, material, vertices=18, width=0.008):
    """Cylinder running between two arbitrary points."""
    direction = Vector((tip[0] - foot[0], tip[1] - foot[1], tip[2] - foot[2]))
    center = ((foot[0] + tip[0]) * 0.5, (foot[1] + tip[1]) * 0.5, (foot[2] + tip[2]) * 0.5)
    return cylinder(name, center, radius, direction.length, material,
                    vertices=vertices, width=width,
                    rotation=direction.to_track_quat("Z", "Y").to_euler())


def paw_badge(parts, center, normal, material, scale=1.0):
    """Gold paw signature shared by the premium family."""
    x, y, z = center
    nx, ny, nz = normal
    parts.append(sphere("PawPad", (x, y, z), (0.10 * scale, 0.075 * scale, 0.075 * scale), material, 20, 12))
    for dx, dy in ((-0.11, 0.10), (-0.04, 0.16), (0.05, 0.16), (0.12, 0.09)):
        parts.append(sphere(
            "PawToe",
            (x + dx * scale, y + dy * scale, z + nz * 0.004),
            (0.042 * scale, 0.045 * scale, 0.042 * scale), material, 16, 10))


def sheet(name, cols, rows, point_at, materials, band_for, thickness=0.03):
    """Build a draped fabric panel as real geometry.

    `point_at(u, v)` returns the Y-up position for normalized coordinates,
    `band_for(u_index)` picks which material a quad column uses.
    """
    verts = []
    for r in range(rows + 1):
        v = r / rows
        for c in range(cols + 1):
            verts.append(point_at(c / cols, v))
    faces = []
    indices = []
    for r in range(rows):
        for c in range(cols):
            a = r * (cols + 1) + c
            faces.append((a, a + 1, a + cols + 2, a + cols + 1))
            indices.append(band_for(c))
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for material in materials:
        obj.data.materials.append(material)
    for polygon, index in zip(obj.data.polygons, indices):
        polygon.material_index = index
    solidify(obj, thickness)
    smooth(obj)
    return obj




def tube(parts, name, points, radius, material, vertices=14):
    """Continuous tube through a list of points.

    Spheres spaced along an arc read as a string of beads at product scale, so
    every joint sphere is bridged by a cylinder of the same radius.
    """
    points = list(points)
    for index, point in enumerate(points):
        parts.append(sphere(name + "Joint", point, (radius, radius, radius),
                            material, vertices, max(8, vertices - 4)))
        if index + 1 < len(points):
            parts.append(strut(name + "Span", point, points[index + 1], radius, material,
                               vertices=vertices, width=0.0))
    return parts


def shell(name, profile, material, segments=44, thickness=0.0, power=0.62):
    """Lofted rounded box: one superellipse ring per profile entry.

    `profile` is a bottom-to-top list of (half_x, half_z, y). `power` below 1
    squares the ring off, so tubs and shower trays read as rounded rectangles
    instead of ovals.
    """
    verts = []
    for half_x, half_z, y in profile:
        for i in range(segments):
            angle = 2.0 * math.pi * i / segments
            c, s = math.cos(angle), math.sin(angle)
            px = half_x * math.copysign(abs(c) ** power, c)
            pz = half_z * math.copysign(abs(s) ** power, s)
            verts.append((px, y, pz))
    faces = []
    for ring in range(len(profile) - 1):
        for i in range(segments):
            a = ring * segments + i
            b = ring * segments + (i + 1) % segments
            faces.append((a, b, b + segments, a + segments))
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material)
    if thickness > 0.0:
        solidify(obj, thickness)
    smooth(obj)
    return obj


def revolve(name, profile, material, segments=32, thickness=0.0, close_bottom=False):
    """Surface of revolution around the Y axis.

    `profile` is a bottom-to-top list of (radius, y) pairs, so pots, vases and
    lamp shades are real tapered shells instead of stacked cylinders.
    """
    verts = []
    for radius, y in profile:
        for i in range(segments):
            angle = 2.0 * math.pi * i / segments
            verts.append((radius * math.cos(angle), y, radius * math.sin(angle)))
    faces = []
    rings = len(profile)
    for ring in range(rings - 1):
        for i in range(segments):
            a = ring * segments + i
            b = ring * segments + (i + 1) % segments
            faces.append((a, b, b + segments, a + segments))
    if close_bottom:
        center = len(verts)
        verts.append((0.0, profile[0][1], 0.0))
        for i in range(segments):
            faces.append((center, (i + 1) % segments, i))
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material)
    if thickness > 0.0:
        solidify(obj, thickness)
    smooth(obj)
    return obj


def join_fixture(name, parts):
    bpy.ops.object.select_all(action="DESELECT")
    for part in parts:
        part.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    root = bpy.context.object
    root.name = name
    root.data.name = name + "Mesh"
    # Blender 5.x's FBX exporter already writes the requested Y-up axes, so the
    # Y-up construction space is exported as-is. The extra +90 X pre-rotation the
    # older bathroom kit needed would lay the model on its back in Unity.
    apply_all(root)
    smooth(root)
    return root


def drop_to_floor(obj):
    """Move the mesh so its lowest point sits on the object origin.

    Wall-hung catalog entries lift the whole product to their HungHeight, so the
    model must be authored around a local origin at its own underside.
    """
    lowest = min((obj.matrix_world @ v.co).y for v in obj.data.vertices)
    for vertex in obj.data.vertices:
        vertex.co.y -= lowest
    obj.data.update()
    return lowest


def export_fixture(obj, filename):
    """Export one object, or several when a product needs moving parts."""
    objects = obj if isinstance(obj, (list, tuple)) else [obj]
    os.makedirs(UNITY_MODELS, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for item in objects:
        item.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.fbx(
        filepath=os.path.join(UNITY_MODELS, filename),
        use_selection=True,
        object_types={"MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=False,
        add_leaf_bones=False,
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        path_mode="AUTO",
        embed_textures=False,
        bake_anim=False,
    )
    for item in objects:
        item.select_set(False)
    return os.path.join(UNITY_MODELS, filename)


def report_parts(parts, limit=8):
    """Print the parts that reach furthest, so a size overrun names its cause."""
    rows = []
    for part in parts:
        xs = [(part.matrix_world @ v.co) for v in part.data.vertices]
        rows.append((part.name,
                     min(v.x for v in xs), max(v.x for v in xs),
                     min(v.y for v in xs), max(v.y for v in xs),
                     min(v.z for v in xs), max(v.z for v in xs)))
    for label, index, reverse in (("maxY", 4, True), ("minY", 3, False),
                                  ("maxZ", 6, True), ("minZ", 5, False),
                                  ("maxX", 2, True), ("minX", 1, False)):
        ranked = sorted(rows, key=lambda row: row[index], reverse=reverse)[:limit]
        print("[extent] %s: %s" % (
            label, ", ".join("%s=%.3f" % (row[0], row[index]) for row in ranked)))


def report(obj):
    dims = obj.dimensions
    print("[premium] %s tris=%d dims(x,y,z blender)=%.2f, %.2f, %.2f" % (
        obj.name, len(obj.data.polygons), dims.x, dims.y, dims.z))
