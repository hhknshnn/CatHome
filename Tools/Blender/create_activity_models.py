import bpy
import math
import os
from mathutils import Vector


ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
MODEL_DIR = os.path.join(ROOT, "Assets", "Art", "Activities", "Models")
SOURCE_PATH = os.path.join(MODEL_DIR, "CatHome_ActivityProps.blend")


def clean_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)


def material(name, color, metallic=0.0, roughness=0.65):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metallic
    return mat


MAT_ORANGE = material("Toy Orange", (0.95, 0.28, 0.055), 0.0, 0.5)
MAT_CREAM = material("Warm Cream", (0.96, 0.72, 0.35), 0.0, 0.72)
MAT_TEAL = material("Play Teal", (0.08, 0.55, 0.56), 0.0, 0.56)
MAT_PURPLE = material("Mouse Purple", (0.47, 0.22, 0.7), 0.0, 0.55)
MAT_PINK = material("Mouse Pink", (0.95, 0.46, 0.55), 0.0, 0.6)
MAT_DARK = material("Detail Dark", (0.09, 0.07, 0.08), 0.0, 0.45)
MAT_WOOD = material("Warm Wood", (0.48, 0.19, 0.07), 0.0, 0.74)
MAT_ROPE = material("Sisal Rope", (0.76, 0.52, 0.24), 0.0, 0.95)
MAT_GREEN = material("Mint Accent", (0.35, 0.76, 0.48), 0.0, 0.58)


def assign(obj, mat):
    obj.data.materials.append(mat)
    return obj


def bevel(obj, amount=0.025, segments=2):
    modifier = obj.modifiers.new("Soft Low Poly Edges", "BEVEL")
    modifier.width = amount
    modifier.segments = segments
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    return obj


def cube(name, location, scale, mat, bevel_amount=0.0, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel_amount:
        bevel(obj, bevel_amount)
    assign(obj, mat)
    return obj


def cylinder(name, location, radius, depth, mat, vertices=12, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices,
        radius=radius,
        depth=depth,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    assign(obj, mat)
    return obj


def ico(name, location, radius, scale, mat, subdivisions=2):
    bpy.ops.mesh.primitive_ico_sphere_add(
        subdivisions=subdivisions,
        radius=radius,
        location=location,
    )
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    assign(obj, mat)
    return obj


def torus(name, location, major, minor, mat, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_torus_add(
        major_radius=major,
        minor_radius=minor,
        major_segments=16,
        minor_segments=6,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    assign(obj, mat)
    return obj


def curve_tube(name, points, radius, mat):
    curve = bpy.data.curves.new(name, "CURVE")
    curve.dimensions = "3D"
    curve.resolution_u = 1
    curve.bevel_depth = radius
    curve.bevel_resolution = 1
    spline = curve.splines.new("POLY")
    spline.points.add(len(points) - 1)
    for point, value in zip(spline.points, points):
        point.co = (*value, 1.0)
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    assign(obj, mat)
    return obj


def triangle_prism(name, center, width, height, depth, mat):
    x = width * 0.5
    y = depth * 0.5
    z = height
    vertices = [
        (-x, -y, 0), (x, -y, 0), (0, -y, z),
        (-x, y, 0), (x, y, 0), (0, y, z),
    ]
    faces = [
        (0, 1, 2), (3, 5, 4), (0, 3, 4, 1),
        (1, 4, 5, 2), (2, 5, 3, 0),
    ]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = center
    assign(obj, mat)
    return obj


def parent_all(objects, root_name):
    root = bpy.data.objects.new(root_name, None)
    bpy.context.collection.objects.link(root)
    for obj in objects:
        obj.parent = root
    return root


def build_ball():
    objects = []
    objects.append(ico("Ball_Core", (0, 0, 0.16), 0.16, (1, 1, 1), MAT_ORANGE, 2))
    objects.append(torus("Ball_Band_A", (0, 0, 0.16), 0.145, 0.015, MAT_CREAM, (0, 0, 0)))
    objects.append(torus("Ball_Band_B", (0, 0, 0.16), 0.145, 0.015, MAT_CREAM, (math.pi / 2, 0, 0)))
    objects.append(torus("Ball_Band_C", (0, 0, 0.16), 0.145, 0.015, MAT_TEAL, (0, math.pi / 2, 0)))
    return parent_all(objects, "ActivityBall")


def build_basket():
    objects = []
    objects.append(cube("Basket_Base", (0, 0, 0.07), (0.33, 0.24, 0.07), MAT_WOOD, 0.035))
    objects.append(cube("Basket_Back", (0, 0.205, 0.25), (0.33, 0.035, 0.18), MAT_WOOD, 0.025, (math.radians(-8), 0, 0)))
    objects.append(cube("Basket_Left", (-0.30, 0, 0.22), (0.035, 0.22, 0.15), MAT_WOOD, 0.02, (0, math.radians(-8), 0)))
    objects.append(cube("Basket_Right", (0.30, 0, 0.22), (0.035, 0.22, 0.15), MAT_WOOD, 0.02, (0, math.radians(8), 0)))
    objects.append(cube("Basket_Front", (0, -0.205, 0.18), (0.29, 0.035, 0.11), MAT_WOOD, 0.02, (math.radians(8), 0, 0)))
    objects.append(ico("Basket_Ball_Orange", (-0.12, 0, 0.31), 0.12, (1, 1, 1), MAT_ORANGE, 1))
    objects.append(ico("Basket_Ball_Teal", (0.12, 0.03, 0.29), 0.105, (1, 1, 1), MAT_TEAL, 1))
    return parent_all(objects, "ToyBasket")


def build_scratch_post():
    objects = []
    objects.append(cube("Scratch_Base", (0, 0, 0.055), (0.38, 0.30, 0.055), MAT_TEAL, 0.045))
    objects.append(cylinder("Scratch_Post", (0, 0, 0.52), 0.095, 0.82, MAT_ROPE, 14))
    for index in range(13):
        z = 0.17 + index * 0.055
        objects.append(torus(f"Rope_Ring_{index:02}", (0, 0, z), 0.098, 0.009, MAT_CREAM))
    objects.append(cube("Scratch_Top", (0, 0, 0.97), (0.29, 0.22, 0.055), MAT_ORANGE, 0.04))
    objects.append(triangle_prism("Top_Ear_Left", (-0.16, 0, 1.02), 0.17, 0.17, 0.08, MAT_ORANGE))
    objects.append(triangle_prism("Top_Ear_Right", (0.16, 0, 1.02), 0.17, 0.17, 0.08, MAT_ORANGE))
    objects.append(curve_tube("Dangling_String", [(0.18, 0, 0.94), (0.22, -0.02, 0.65)], 0.008, MAT_DARK))
    objects.append(ico("Dangling_Toy", (0.22, -0.02, 0.59), 0.07, (1, 1, 1), MAT_PURPLE, 1))
    return parent_all(objects, "ScratchPost")


def build_mouse():
    objects = []
    objects.append(ico("Mouse_Body", (0, 0, 0.10), 0.16, (1.25, 0.72, 0.62), MAT_PURPLE, 2))
    objects.append(ico("Mouse_Head", (0.18, 0, 0.13), 0.105, (1.0, 0.9, 0.95), MAT_PURPLE, 2))
    objects.append(ico("Mouse_Ear_Left", (0.13, -0.075, 0.22), 0.055, (1, 0.38, 1), MAT_PINK, 1))
    objects.append(ico("Mouse_Ear_Right", (0.13, 0.075, 0.22), 0.055, (1, 0.38, 1), MAT_PINK, 1))
    objects.append(ico("Mouse_Nose", (0.28, 0, 0.13), 0.026, (1, 1, 1), MAT_PINK, 1))
    objects.append(ico("Mouse_Eye_Left", (0.22, -0.06, 0.17), 0.018, (1, 1, 1), MAT_DARK, 1))
    objects.append(ico("Mouse_Eye_Right", (0.22, 0.06, 0.17), 0.018, (1, 1, 1), MAT_DARK, 1))
    objects.append(curve_tube("Mouse_Tail", [(-0.16, 0, 0.11), (-0.29, 0.05, 0.12), (-0.37, 0.13, 0.08), (-0.32, 0.22, 0.05)], 0.012, MAT_PINK))
    objects.append(cylinder("Mouse_Wheel_Left", (-0.04, -0.115, 0.055), 0.045, 0.025, MAT_DARK, 10, (math.pi / 2, 0, 0)))
    objects.append(cylinder("Mouse_Wheel_Right", (-0.04, 0.115, 0.055), 0.045, 0.025, MAT_DARK, 10, (math.pi / 2, 0, 0)))
    objects.append(cylinder("Mouse_Key", (-0.06, 0, 0.24), 0.018, 0.10, MAT_CREAM, 8, (0, math.pi / 2, 0)))
    return parent_all(objects, "ClockworkMouse")


def export_root(root, filename):
    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    for child in root.children_recursive:
        child.select_set(True)
    bpy.context.view_layer.objects.active = root
    path = os.path.join(MODEL_DIR, filename)
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        bake_anim=False,
        bake_space_transform=False,
        object_types={'EMPTY', 'MESH', 'OTHER'},
    )


def main():
    os.makedirs(MODEL_DIR, exist_ok=True)
    clean_scene()
    roots = [build_ball(), build_basket(), build_scratch_post(), build_mouse()]

    offsets = [(-1.7, 0, 0), (-0.65, 0, 0), (0.65, 0, 0), (1.75, 0, 0)]
    for root, offset in zip(roots, offsets):
        root.location = offset

    bpy.ops.wm.save_as_mainfile(filepath=SOURCE_PATH)

    for root in roots:
        root.location = (0, 0, 0)
    export_root(roots[0], "ActivityBall.fbx")
    export_root(roots[1], "ToyBasket.fbx")
    export_root(roots[2], "ScratchPost.fbx")
    export_root(roots[3], "ClockworkMouse.fbx")


main()
