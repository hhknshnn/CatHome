import bpy
import math
import os
from mathutils import Vector


ROOT = r"C:\Users\HAKAN\Desktop\CatHome\CatHome"
BLEND_PATH = ROOT + r"\ArtSource\Blender\Cat\LowPolyRigify\CatHome_CatSculpt_Blockout_v3.blend"
QA_PATH = ROOT + r"\Temp\CatRigQA\LowPolyRigify_v3"


def clear_scene():
    if bpy.context.object and bpy.context.object.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for blocks in (bpy.data.meshes, bpy.data.curves, bpy.data.armatures,
                   bpy.data.materials, bpy.data.cameras, bpy.data.lights,
                   bpy.data.actions):
        for block in list(blocks):
            if block.users == 0:
                blocks.remove(block)
    bpy.context.scene.name = 'CatHome_CatSculpt_Blockout_v3'


def collection(name):
    value = bpy.data.collections.get(name)
    if value is None:
        value = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(value)
    return value


def move_to(obj, target):
    for owner in list(obj.users_collection):
        owner.objects.unlink(obj)
    target.objects.link(obj)


def make_material(name, color, roughness=0.78):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = roughness
    return mat


def assign(obj, mat):
    obj.data.materials.append(mat)


def smooth(obj, enabled=True):
    if hasattr(obj.data, 'polygons'):
        for polygon in obj.data.polygons:
            polygon.use_smooth = enabled


def ico(name, location, scale, mat, target, subdivisions=2):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1.0, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    assign(obj, mat)
    smooth(obj)
    move_to(obj, target)
    return obj


def cone_between(name, start, end, radius_start, radius_end, mat, target, vertices=12):
    start = Vector(start)
    end = Vector(end)
    delta = end - start
    bpy.ops.mesh.primitive_cone_add(
        vertices=vertices,
        radius1=radius_start,
        radius2=radius_end,
        depth=delta.length,
        location=(start + end) * 0.5,
    )
    obj = bpy.context.object
    obj.name = name
    obj.rotation_mode = 'QUATERNION'
    obj.rotation_quaternion = delta.to_track_quat('Z', 'Y')
    assign(obj, mat)
    smooth(obj)
    move_to(obj, target)
    return obj


def closed_mesh(name, vertices, faces, mat, target):
    mesh = bpy.data.meshes.new(name + '_Mesh')
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    target.objects.link(obj)
    assign(obj, mat)
    smooth(obj)
    return obj


def curve_line(name, points, bevel, mat, target):
    curve = bpy.data.curves.new(name + '_Curve', type='CURVE')
    curve.dimensions = '3D'
    curve.resolution_u = 2
    curve.bevel_depth = bevel
    curve.bevel_resolution = 1
    curve.use_fill_caps = True
    spline = curve.splines.new('BEZIER')
    spline.bezier_points.add(len(points) - 1)
    for point, coordinate in zip(spline.bezier_points, points):
        point.co = coordinate
        point.handle_left_type = 'AUTO'
        point.handle_right_type = 'AUTO'
    obj = bpy.data.objects.new(name, curve)
    target.objects.link(obj)
    assign(obj, mat)
    return obj


def ear_wedge(name, sign, mat, target):
    center_x = 0.205 * sign
    vertices = [
        (center_x - 0.155 * sign, 0.70, 1.47),
        (center_x + 0.150 * sign, 0.71, 1.46),
        (center_x + 0.035 * sign, 0.73, 1.80),
        (center_x - 0.115 * sign, 0.57, 1.44),
        (center_x + 0.115 * sign, 0.58, 1.43),
        (center_x + 0.035 * sign, 0.61, 1.75),
    ]
    faces = [(0, 1, 2), (5, 4, 3), (0, 3, 4, 1),
             (1, 4, 5, 2), (2, 5, 3, 0)]
    return closed_mesh(name, vertices, faces, mat, target)


def join_and_remesh(parts, name, mat, target):
    bpy.ops.object.select_all(action='DESELECT')
    for part in parts:
        part.hide_set(False)
        part.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name

    modifier = obj.modifiers.new('SculptUnion', 'REMESH')
    modifier.mode = 'VOXEL'
    modifier.voxel_size = 0.026
    modifier.use_smooth_shade = True
    bpy.ops.object.modifier_apply(modifier=modifier.name)

    smooth_modifier = obj.modifiers.new('SculptRelax', 'SMOOTH')
    smooth_modifier.factor = 0.30
    smooth_modifier.iterations = 4
    bpy.ops.object.modifier_apply(modifier=smooth_modifier.name)

    decimate = obj.modifiers.new('LowPolyBudget', 'DECIMATE')
    decimate.decimate_type = 'COLLAPSE'
    decimate.ratio = 0.30
    decimate.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=decimate.name)
    obj.data.materials.clear()
    assign(obj, mat)
    smooth(obj)
    move_to(obj, target)
    return obj


def make_tail(target, mat):
    curve = bpy.data.curves.new('GEO_Tail_Curve', type='CURVE')
    curve.dimensions = '3D'
    curve.resolution_u = 3
    curve.bevel_depth = 0.082
    curve.bevel_resolution = 2
    curve.use_fill_caps = True
    spline = curve.splines.new('BEZIER')
    spline.bezier_points.add(7)
    points = [
        (0.30, -0.68, 0.78),
        (0.44, -0.84, 0.67),
        (0.51, -0.98, 0.48),
        (0.52, -1.04, 0.26),
        (0.43, -1.00, 0.115),
        (0.22, -0.89, 0.090),
        (-0.04, -0.78, 0.088),
        (-0.27, -0.67, 0.095),
    ]
    radii = (1.10, 1.08, 1.04, 0.98, 0.90, 0.82, 0.72, 0.58)
    for point, coordinate, radius in zip(spline.bezier_points, points, radii):
        point.co = coordinate
        point.radius = radius
        point.handle_left_type = 'AUTO'
        point.handle_right_type = 'AUTO'
    obj = bpy.data.objects.new('GEO_Tail', curve)
    target.objects.link(obj)
    assign(obj, mat)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.convert(target='MESH')
    smooth(obj)
    return obj


def triangle_nose(target, mat):
    vertices = [
        (-0.052, 1.070, 1.285),
        (0.052, 1.070, 1.285),
        (0.0, 1.078, 1.235),
        (0.0, 1.018, 1.262),
    ]
    faces = [(0, 1, 2), (0, 3, 1), (0, 2, 3), (1, 3, 2)]
    return closed_mesh('GEO_Nose', vertices, faces, mat, target)


def create_metarig(target):
    import addon_utils
    enabled, loaded = addon_utils.check('rigify')
    operator_ok = hasattr(bpy.ops.object, 'armature_cat_metarig_add')
    if not operator_ok:
        try:
            bpy.ops.preferences.addon_enable(module='rigify')
        except Exception:
            pass
        operator_ok = hasattr(bpy.ops.object, 'armature_cat_metarig_add')
    meta = None
    if operator_ok:
        bpy.ops.object.armature_cat_metarig_add()
        meta = bpy.context.object
        meta.name = 'RIGIFY_Cat_Meta_Unfitted'
        meta.hide_render = True
        meta.hide_set(True)
        meta['fit_status'] = 'unfitted_silhouette_checkpoint'
        move_to(meta, target)
    return operator_ok, meta


def look_at(camera, target=(0.0, 0.02, 0.83)):
    camera.rotation_euler = (Vector(target) - camera.location).to_track_quat('-Z', 'Y').to_euler()


def setup_presentation(target, mats):
    bpy.ops.mesh.primitive_plane_add(size=200, location=(0, 0, -0.012))
    ground = bpy.context.object
    ground.name = 'STAGE_Ground'
    assign(ground, mats['ground'])
    move_to(ground, target)

    bpy.ops.object.camera_add(location=(3.45, 4.70, 2.18))
    camera = bpy.context.object
    camera.name = 'CAM_ModelReview'
    camera.data.lens = 62
    look_at(camera)
    move_to(camera, target)
    bpy.context.scene.camera = camera

    def area(name, location, energy, size, color):
        bpy.ops.object.light_add(type='AREA', location=location)
        light = bpy.context.object
        light.name = name
        light.data.energy = energy
        light.data.shape = 'DISK'
        light.data.size = size
        light.data.color = color
        light.rotation_euler = (Vector((0, 0, 0.75)) - light.location).to_track_quat('-Z', 'Y').to_euler()
        move_to(light, target)

    area('LIGHT_Key', (3.4, 4.0, 4.8), 720, 3.2, (1.0, 0.83, 0.72))
    area('LIGHT_Fill', (-3.5, 2.0, 2.8), 510, 3.4, (0.66, 0.88, 1.0))
    area('LIGHT_Rim', (-2.0, -3.5, 3.7), 620, 2.8, (0.98, 0.64, 0.76))

    world = bpy.context.scene.world or bpy.data.worlds.new('CatHome_ClayWorld')
    bpy.context.scene.world = world
    world.use_nodes = True
    world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.055, 0.045, 0.060, 1)
    world.node_tree.nodes['Background'].inputs['Strength'].default_value = 0.28
    return camera


def render_view(scene, camera, name, location, target=(0.0, 0.02, 0.83)):
    camera.location = location
    look_at(camera, target)
    scene.render.filepath = os.path.join(QA_PATH, name)
    bpy.ops.render.render(write_still=True)


def build():
    os.makedirs(QA_PATH, exist_ok=True)
    clear_scene()
    geometry = collection('CAT_SCULPT')
    face = collection('CAT_FACE_GUIDES')
    rig = collection('CAT_RIG_REFERENCE')
    presentation = collection('CAT_PRESENTATION')

    mats = {
        'clay': make_material('CH_CAT_Clay_MidGray', (0.36, 0.39, 0.43), 0.82),
        'eye': make_material('CH_CAT_Clay_Iris', (0.18, 0.21, 0.24), 0.30),
        'pupil': make_material('CH_CAT_Clay_Pupil', (0.025, 0.030, 0.040), 0.28),
        'glint': make_material('CH_CAT_Clay_EyeGlint', (0.86, 0.88, 0.90), 0.20),
        'nose': make_material('CH_CAT_Clay_Nose', (0.22, 0.24, 0.28), 0.64),
        'ground': make_material('CH_STAGE_Clay', (0.075, 0.062, 0.078), 0.76),
    }

    parts = []
    add = parts.append

    # One continuous anatomical mass: pelvis, rib cage, shoulder, neck and head.
    add(ico('SRC_Torso', (0, -0.10, 0.76), (0.40, 0.70, 0.34), mats['clay'], geometry, 2))
    add(ico('SRC_Ribcage', (0, 0.30, 0.82), (0.38, 0.43, 0.39), mats['clay'], geometry, 2))
    add(ico('SRC_Pelvis', (0, -0.52, 0.79), (0.43, 0.44, 0.42), mats['clay'], geometry, 2))
    add(ico('SRC_ShoulderBridge', (0, 0.43, 0.91), (0.34, 0.31, 0.32), mats['clay'], geometry, 2))
    add(ico('SRC_NeckLow', (0, 0.52, 1.00), (0.275, 0.285, 0.31), mats['clay'], geometry, 2))
    add(ico('SRC_Head', (0, 0.75, 1.30), (0.355, 0.295, 0.315), mats['clay'], geometry, 2))
    add(ico('SRC_Cheek_L', (0.145, 0.91, 1.225), (0.18, 0.16, 0.165), mats['clay'], geometry, 2))
    add(ico('SRC_Cheek_R', (-0.145, 0.91, 1.225), (0.18, 0.16, 0.165), mats['clay'], geometry, 2))
    add(ico('SRC_Muzzle_L', (0.095, 1.005, 1.235), (0.125, 0.095, 0.105), mats['clay'], geometry, 2))
    add(ico('SRC_Muzzle_R', (-0.095, 1.005, 1.235), (0.125, 0.095, 0.105), mats['clay'], geometry, 2))
    add(ico('SRC_Chin', (0, 0.985, 1.175), (0.13, 0.08, 0.07), mats['clay'], geometry, 2))
    add(ear_wedge('SRC_Ear_L', 1, mats['clay'], geometry))
    add(ear_wedge('SRC_Ear_R', -1, mats['clay'], geometry))

    # Front legs are nearly vertical below the shoulders, with a slight forward wrist.
    for side, x in [('L', 0.215), ('R', -0.215)]:
        shoulder = (x, 0.38, 0.80)
        elbow = (x, 0.43, 0.46)
        wrist = (x, 0.49, 0.16)
        add(ico(f'SRC_Shoulder_{side}', shoulder, (0.145, 0.165, 0.19), mats['clay'], geometry, 2))
        add(cone_between(f'SRC_FrontUpper_{side}', shoulder, elbow, 0.110, 0.084, mats['clay'], geometry))
        add(cone_between(f'SRC_FrontLower_{side}', elbow, wrist, 0.086, 0.062, mats['clay'], geometry))
        add(ico(f'SRC_FrontPaw_{side}', (x, 0.575, 0.072), (0.132, 0.205, 0.072), mats['clay'], geometry, 2))

    # Hind legs carry the cat silhouette: full thigh, forward knee, rear hock, long foot.
    for side, x in [('L', 0.285), ('R', -0.285)]:
        hip = (x, -0.50, 0.72)
        knee = (x, -0.18, 0.43)
        hock = (x, -0.57, 0.20)
        ankle = (x, -0.42, 0.13)
        add(ico(f'SRC_HindThigh_{side}', (x, -0.43, 0.57), (0.22, 0.29, 0.31), mats['clay'], geometry, 2))
        add(cone_between(f'SRC_HindFemur_{side}', hip, knee, 0.145, 0.105, mats['clay'], geometry))
        add(cone_between(f'SRC_HindShin_{side}', knee, hock, 0.108, 0.075, mats['clay'], geometry))
        add(cone_between(f'SRC_HindHock_{side}', hock, ankle, 0.078, 0.064, mats['clay'], geometry))
        add(ico(f'SRC_HindPaw_{side}', (x, -0.29, 0.072), (0.142, 0.225, 0.072), mats['clay'], geometry, 2))

    cat_body = join_and_remesh(parts, 'GEO_CatBody_Blockout', mats['clay'], geometry)
    cat_body['stage'] = 'silhouette_only'
    cat_body['retopology_status'] = 'not_started'
    cat_body['reference_style'] = 'rounded_low_poly_tabby'
    cat_body['faces_positive_y'] = True

    make_tail(geometry, mats['clay'])

    for side, x in [('L', 0.140), ('R', -0.140)]:
        eye = ico(f'GEO_Eye_{side}', (x, 1.010, 1.345), (0.108, 0.060, 0.116), mats['eye'], face, 2)
        pupil = ico(f'GEO_Pupil_{side}', (x, 1.064, 1.345), (0.052, 0.017, 0.075), mats['pupil'], face, 2)
        glint_x = x + (0.022 if side == 'L' else -0.022)
        ico(f'GEO_EyeGlint_{side}', (glint_x, 1.080, 1.386), (0.017, 0.009, 0.020), mats['glint'], face, 1)
        eye['future_blink_target'] = f'Blink_{side}'
        pupil['future_eye_control'] = f'eye.{side}'

    triangle_nose(face, mats['nose'])
    curve_line('GEO_Mouth_L', [(0.0, 1.072, 1.236), (0.025, 1.075, 1.205), (0.095, 1.050, 1.193)], 0.005, mats['nose'], face)
    curve_line('GEO_Mouth_R', [(0.0, 1.072, 1.236), (-0.025, 1.075, 1.205), (-0.095, 1.050, 1.193)], 0.005, mats['nose'], face)
    rigify_ok, meta = create_metarig(rig)

    camera = setup_presentation(presentation, mats)
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_EEVEE'
    scene.render.resolution_x = 768
    scene.render.resolution_y = 768
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_mode = 'RGBA'
    scene.render.film_transparent = False
    scene.view_settings.look = 'AgX - Medium High Contrast'

    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    render_view(scene, camera, 'CatHome_CatSculpt_v3_ThreeQuarter.png', (3.45, 4.70, 2.18))
    render_view(scene, camera, 'CatHome_CatSculpt_v3_Front.png', (0.0, 5.20, 1.18))
    render_view(scene, camera, 'CatHome_CatSculpt_v3_Side.png', (5.20, 0.0, 1.20))
    camera.location = (3.45, 4.70, 2.18)
    look_at(camera)
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)

    triangles = sum(max(0, len(poly.vertices) - 2) for poly in cat_body.data.polygons)
    print({
        'blend': BLEND_PATH,
        'qa': QA_PATH,
        'rigify_cat_operator': rigify_ok,
        'metarig_created': meta is not None,
        'body_triangles': triangles,
        'body_objects': len(geometry.objects),
        'stage': 'new_anatomical_silhouette_v3',
    })


if __name__ == '__main__':
    build()
