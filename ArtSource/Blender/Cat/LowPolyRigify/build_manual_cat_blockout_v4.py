import bpy
import math
import os
from mathutils import Vector


ROOT = r"C:\Users\HAKAN\Desktop\CatHome\CatHome"
BLEND_PATH = ROOT + r"\ArtSource\Blender\Cat\LowPolyRigify\CatHome_ManualCat_Blockout_v4.blend"
QA_PATH = ROOT + r"\Temp\CatRigQA\ManualCat_v4"


def ensure_collection(name):
    value = bpy.data.collections.get(name)
    if value is None:
        value = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(value)
    return value


def clear_collection(target):
    for obj in list(target.objects):
        bpy.data.objects.remove(obj, do_unlink=True)


def material(name, color, roughness=0.76):
    value = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    value.diffuse_color = (*color, 1.0)
    value.use_nodes = True
    bsdf = value.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = roughness
    return value


def mesh_object(name, vertices, faces, mat, target):
    mesh = bpy.data.meshes.new(name + '_Mesh')
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    target.objects.link(obj)
    obj.data.materials.append(mat)
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    return obj


def loft_y(name, stations, sides, mat, target):
    vertices = []
    faces = []
    for y, radius_x, center_z, radius_z in stations:
        for index in range(sides):
            angle = 2.0 * math.pi * index / sides
            vertices.append((radius_x * math.cos(angle), y, center_z + radius_z * math.sin(angle)))
    for ring in range(len(stations) - 1):
        base = ring * sides
        next_base = (ring + 1) * sides
        for index in range(sides):
            nxt = (index + 1) % sides
            faces.append((base + index, base + nxt, next_base + nxt, next_base + index))
    faces.append(tuple(reversed(range(sides))))
    last = (len(stations) - 1) * sides
    faces.append(tuple(last + index for index in range(sides)))
    return mesh_object(name, vertices, faces, mat, target)


def loft_z(name, stations, sides, mat, target):
    vertices = []
    faces = []
    for z, radius_x, center_y, radius_y in stations:
        for index in range(sides):
            angle = 2.0 * math.pi * index / sides
            vertices.append((radius_x * math.cos(angle), center_y + radius_y * math.sin(angle), z))
    for ring in range(len(stations) - 1):
        base = ring * sides
        next_base = (ring + 1) * sides
        for index in range(sides):
            nxt = (index + 1) % sides
            faces.append((base + index, base + nxt, next_base + nxt, next_base + index))
    faces.append(tuple(reversed(range(sides))))
    last = (len(stations) - 1) * sides
    faces.append(tuple(last + index for index in range(sides)))
    return mesh_object(name, vertices, faces, mat, target)


def limb(name, stations, sides, mat, target):
    vertices = []
    faces = []
    for x, y, z, radius_x, radius_y in stations:
        for index in range(sides):
            angle = 2.0 * math.pi * index / sides
            vertices.append((x + radius_x * math.cos(angle), y + radius_y * math.sin(angle), z))
    for ring in range(len(stations) - 1):
        base = ring * sides
        next_base = (ring + 1) * sides
        for index in range(sides):
            nxt = (index + 1) % sides
            faces.append((base + index, base + nxt, next_base + nxt, next_base + index))
    faces.append(tuple(reversed(range(sides))))
    last = (len(stations) - 1) * sides
    faces.append(tuple(last + index for index in range(sides)))
    return mesh_object(name, vertices, faces, mat, target)


def ellipsoid(name, location, scale, mat, target, subdivisions=2):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    for owner in list(obj.users_collection):
        owner.objects.unlink(obj)
    target.objects.link(obj)
    obj.data.materials.append(mat)
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    return obj


def closed_mesh(name, vertices, faces, mat, target):
    return mesh_object(name, vertices, faces, mat, target)


def curve_line(name, points, bevel, mat, target):
    data = bpy.data.curves.new(name + '_Curve', type='CURVE')
    data.dimensions = '3D'
    data.resolution_u = 2
    data.bevel_depth = bevel
    data.bevel_resolution = 1
    data.use_fill_caps = True
    spline = data.splines.new('BEZIER')
    spline.bezier_points.add(len(points) - 1)
    for point, coordinate in zip(spline.bezier_points, points):
        point.co = coordinate
        point.handle_left_type = 'AUTO'
        point.handle_right_type = 'AUTO'
    obj = bpy.data.objects.new(name, data)
    target.objects.link(obj)
    obj.data.materials.append(mat)
    return obj


def ear(name, sign, mat, target):
    cx = 0.20 * sign
    vertices = [
        (cx - 0.155 * sign, 0.70, 1.47),
        (cx + 0.145 * sign, 0.70, 1.46),
        (cx + 0.040 * sign, 0.69, 1.78),
        (cx - 0.115 * sign, 0.57, 1.45),
        (cx + 0.110 * sign, 0.57, 1.44),
        (cx + 0.038 * sign, 0.58, 1.74),
    ]
    faces = [(0, 1, 2), (5, 4, 3), (0, 3, 4, 1), (1, 4, 5, 2), (2, 5, 3, 0)]
    return mesh_object(name, vertices, faces, mat, target)


def tail(target, mat):
    data = bpy.data.curves.new('GEO_Tail_Blockout_Curve', type='CURVE')
    data.dimensions = '3D'
    data.resolution_u = 3
    data.bevel_depth = 0.085
    data.bevel_resolution = 2
    data.use_fill_caps = True
    spline = data.splines.new('BEZIER')
    spline.bezier_points.add(7)
    points = [
        (0.29, -0.67, 0.78), (0.39, -0.83, 0.67),
        (0.44, -0.99, 0.49), (0.48, -1.06, 0.30),
        (0.55, -1.02, 0.16), (0.68, -0.91, 0.10),
        (0.83, -0.75, 0.10), (0.93, -0.59, 0.12),
    ]
    radii = (1.08, 1.06, 1.02, 0.96, 0.88, 0.80, 0.70, 0.56)
    for point, coordinate, radius in zip(spline.bezier_points, points, radii):
        point.co = coordinate
        point.radius = radius
        point.handle_left_type = 'AUTO'
        point.handle_right_type = 'AUTO'
    obj = bpy.data.objects.new('GEO_Tail_Blockout', data)
    target.objects.link(obj)
    obj.data.materials.append(mat)
    return obj


def build():
    os.makedirs(QA_PATH, exist_ok=True)
    model = ensure_collection('CAT_MODEL_V4')
    clear_collection(model)
    clay = material('CH_CAT_Blockout_Fur', (0.45, 0.20, 0.055), 0.76)
    muzzle_clay = material('CH_CAT_Blockout_Cream', (0.82, 0.61, 0.35), 0.80)
    eye_clay = material('CH_CAT_Blockout_Iris', (0.72, 0.30, 0.025), 0.28)
    pupil_clay = material('CH_CAT_Blockout_Pupil', (0.012, 0.016, 0.024), 0.24)
    glint_clay = material('CH_CAT_Blockout_Glint', (0.95, 0.90, 0.72), 0.18)
    nose_clay = material('CH_CAT_Blockout_Nose', (0.78, 0.11, 0.12), 0.58)
    inner_ear = material('CH_CAT_Blockout_InnerEar', (0.72, 0.13, 0.14), 0.70)

    body = loft_y('GEO_Body_Core', [
        (-0.78, 0.20, 0.76, 0.24),
        (-0.64, 0.35, 0.79, 0.37),
        (-0.43, 0.41, 0.78, 0.41),
        (-0.12, 0.42, 0.76, 0.37),
        (0.18, 0.38, 0.79, 0.36),
        (0.40, 0.34, 0.84, 0.38),
        (0.54, 0.28, 0.94, 0.34),
        (0.63, 0.235, 1.07, 0.28),
    ], 14, clay, model)

    head = loft_z('GEO_Head_Core', [
        (1.05, 0.22, 0.66, 0.20),
        (1.14, 0.30, 0.72, 0.27),
        (1.28, 0.365, 0.75, 0.31),
        (1.43, 0.35, 0.73, 0.29),
        (1.56, 0.295, 0.69, 0.245),
        (1.63, 0.21, 0.66, 0.18),
    ], 14, clay, model)

    ear_l = ear('GEO_Ear_L_Blockout', 1, clay, model)
    ear_r = ear('GEO_Ear_R_Blockout', -1, clay, model)

    pieces = [body, head, ear_l, ear_r]
    for side, x in [('L', 0.215), ('R', -0.215)]:
        pieces.append(limb(f'GEO_FrontLeg_{side}', [
            (x, 0.38, 0.82, 0.135, 0.145),
            (x, 0.43, 0.58, 0.105, 0.110),
            (x, 0.47, 0.33, 0.080, 0.084),
            (x, 0.50, 0.13, 0.067, 0.072),
        ], 10, clay, model))
        pieces.append(ellipsoid(f'GEO_FrontPaw_{side}', (x, 0.59, 0.075), (0.14, 0.21, 0.075), muzzle_clay, model, 2))

    for side, x in [('L', 0.285), ('R', -0.285)]:
        pieces.append(ellipsoid(f'GEO_HindThigh_{side}', (x, -0.43, 0.58), (0.22, 0.29, 0.31), clay, model, 2))
        pieces.append(limb(f'GEO_HindLeg_{side}', [
            (x, -0.42, 0.66, 0.13, 0.145),
            (x, -0.19, 0.43, 0.105, 0.115),
            (x, -0.53, 0.22, 0.076, 0.084),
            (x, -0.42, 0.13, 0.062, 0.070),
        ], 10, clay, model))
        pieces.append(ellipsoid(f'GEO_HindPaw_{side}', (x, -0.29, 0.075), (0.145, 0.23, 0.075), muzzle_clay, model, 2))

    # Neutral face guides: short muzzle and inset dark eyes, not final facial topology.
    ellipsoid('GEO_Muzzle_L_Guide', (0.092, 1.035, 1.245), (0.128, 0.078, 0.100), muzzle_clay, model, 2)
    ellipsoid('GEO_Muzzle_R_Guide', (-0.092, 1.035, 1.245), (0.128, 0.078, 0.100), muzzle_clay, model, 2)
    for side, x in [('L', 0.140), ('R', -0.140)]:
        ellipsoid(f'GEO_Eye_{side}_Guide', (x, 1.025, 1.390), (0.105, 0.050, 0.115), eye_clay, model, 2)
        ellipsoid(f'GEO_Pupil_{side}_Guide', (x, 1.071, 1.390), (0.050, 0.014, 0.074), pupil_clay, model, 2)
        glint_x = x + (0.022 if side == 'L' else -0.022)
        ellipsoid(f'GEO_Glint_{side}_Guide', (glint_x, 1.083, 1.430), (0.016, 0.008, 0.019), glint_clay, model, 1)
    nose_vertices = [
        (-0.050, 1.112, 1.295), (0.050, 1.112, 1.295),
        (0.0, 1.118, 1.245), (0.0, 1.060, 1.270),
    ]
    closed_mesh('GEO_Nose_Guide', nose_vertices,
                [(0, 1, 2), (0, 3, 1), (0, 2, 3), (1, 3, 2)],
                nose_clay, model)
    curve_line('GEO_Mouth_L_Guide', [(0.0, 1.116, 1.245), (0.026, 1.116, 1.215), (0.096, 1.085, 1.202)], 0.005, nose_clay, model)
    curve_line('GEO_Mouth_R_Guide', [(0.0, 1.116, 1.245), (-0.026, 1.116, 1.215), (-0.096, 1.085, 1.202)], 0.005, nose_clay, model)
    ellipsoid('GEO_ChestPatch_Guide', (0.0, 0.655, 0.845), (0.225, 0.032, 0.315), muzzle_clay, model, 2)

    closed_mesh('GEO_EarInner_L_Guide', [(0.075, 0.555, 1.475), (0.300, 0.555, 1.470), (0.238, 0.565, 1.705)], [(0, 1, 2)], inner_ear, model)
    closed_mesh('GEO_EarInner_R_Guide', [(-0.075, 0.555, 1.475), (-0.300, 0.555, 1.470), (-0.238, 0.565, 1.705)], [(0, 1, 2)], inner_ear, model)

    # Keep the sculpt pieces editable; this checkpoint is for silhouette only.
    for obj in pieces:
        obj['blockout_stage'] = 'manual_editable_core'
    tail(model, clay)

    reference_plane = bpy.data.objects.get('REF_Cat_ThreeQuarter_Plane')
    if reference_plane:
        reference_plane.hide_viewport = True
        reference_plane.hide_render = True
    reference_empty = bpy.data.objects.get('REF_Cat_ThreeQuarter_Input')
    if reference_empty:
        reference_empty.hide_viewport = True
        reference_empty.hide_render = True

    bpy.ops.object.select_all(action='DESELECT')
    body.select_set(True)
    bpy.context.view_layer.objects.active = body
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    print({'blend': BLEND_PATH, 'collection': model.name, 'editable_parts': len(model.objects), 'stage': 'manual_silhouette_core'})


if __name__ == '__main__':
    build()
