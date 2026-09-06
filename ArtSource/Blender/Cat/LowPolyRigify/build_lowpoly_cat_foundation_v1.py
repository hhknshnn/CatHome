import bpy
import math
from mathutils import Vector


ROOT = r"C:\Users\HAKAN\Desktop\CatHome\CatHome"
BLEND_PATH = ROOT + r"\ArtSource\Blender\Cat\LowPolyRigify\CatHome_LowPolyCat_Rigify_v2.blend"
QA_PATH = ROOT + r"\Temp\CatRigQA\LowPolyRigify_v1"


def clear_to_blank():
    if bpy.context.object and bpy.context.object.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')
    # Direct datablock removal also clears hidden objects from the prior WIP;
    # operator selection alone deliberately skips hidden rigs/widgets.
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for datablocks in (
        bpy.data.meshes, bpy.data.curves, bpy.data.armatures,
        bpy.data.cameras, bpy.data.lights, bpy.data.materials,
        bpy.data.actions,
    ):
        for block in list(datablocks):
            if block.users == 0:
                datablocks.remove(block)
    for collection in list(bpy.data.collections):
        if collection.users == 0:
            bpy.data.collections.remove(collection)
    bpy.context.scene.name = "CatHome_LowPolyCat_Design"


def ensure_collection(name):
    collection = bpy.data.collections.get(name)
    if collection is None:
        collection = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(collection)
    return collection


def move_to_collection(obj, collection):
    for current in list(obj.users_collection):
        current.objects.unlink(obj)
    collection.objects.link(obj)


def material(name, color, metallic=0.0, roughness=0.62):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = roughness
    bsdf.inputs['Metallic'].default_value = metallic
    return mat


def assign(obj, mat):
    obj.data.materials.append(mat)


def smooth(obj):
    if hasattr(obj.data, 'polygons'):
        for poly in obj.data.polygons:
            poly.use_smooth = True


def ellipsoid(name, location, scale, mat, collection, segments=12, rings=8, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    assign(obj, mat)
    smooth(obj)
    move_to_collection(obj, collection)
    return obj


def cone_between(name, start, end, r1, r2, mat, collection, vertices=10):
    start = Vector(start)
    end = Vector(end)
    delta = end - start
    bpy.ops.mesh.primitive_cone_add(
        vertices=vertices,
        radius1=r1,
        radius2=r2,
        depth=delta.length,
        location=(start + end) * 0.5,
    )
    obj = bpy.context.object
    obj.name = name
    obj.rotation_mode = 'QUATERNION'
    obj.rotation_quaternion = delta.to_track_quat('Z', 'Y')
    assign(obj, mat)
    smooth(obj)
    move_to_collection(obj, collection)
    return obj


def prism(name, vertices, faces, mat, collection):
    mesh = bpy.data.meshes.new(name + '_Mesh')
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    collection.objects.link(obj)
    assign(obj, mat)
    return obj


def curve_line(name, points, bevel, mat, collection):
    curve_data = bpy.data.curves.new(name + '_Curve', type='CURVE')
    curve_data.dimensions = '3D'
    curve_data.resolution_u = 2
    curve_data.bevel_depth = bevel
    curve_data.bevel_resolution = 1
    curve_data.use_fill_caps = True
    spline = curve_data.splines.new('BEZIER')
    spline.bezier_points.add(len(points) - 1)
    for point, co in zip(spline.bezier_points, points):
        point.co = co
        point.handle_left_type = 'AUTO'
        point.handle_right_type = 'AUTO'
    obj = bpy.data.objects.new(name, curve_data)
    collection.objects.link(obj)
    assign(obj, mat)
    return obj


def ear(name, x, outer_mat, inner_mat, collection):
    sign = 1 if x > 0 else -1
    verts = [
        (x - 0.15 * sign, 0.75, 1.43),
        (x + 0.15 * sign, 0.76, 1.44),
        (x + 0.035 * sign, 0.79, 1.76),
        (x - 0.12 * sign, 0.66, 1.43),
        (x + 0.12 * sign, 0.67, 1.44),
        (x + 0.035 * sign, 0.69, 1.73),
    ]
    faces = [(0, 1, 2), (5, 4, 3), (0, 3, 4, 1), (1, 4, 5, 2), (2, 5, 3, 0)]
    obj = prism(name, verts, faces, outer_mat, collection)
    inner_verts = [
        (x - 0.085 * sign, 0.645, 1.48),
        (x + 0.085 * sign, 0.65, 1.49),
        (x + 0.025 * sign, 0.68, 1.68),
    ]
    inner = prism(name.replace('Ear', 'EarInner'), inner_verts, [(0, 1, 2)], inner_mat, collection)
    return obj, inner


def create_tail(collection, mat):
    curve_data = bpy.data.curves.new('GEO_Tail_Mesh', type='CURVE')
    curve_data.dimensions = '3D'
    curve_data.resolution_u = 4
    curve_data.bevel_depth = 0.085
    curve_data.bevel_resolution = 2
    curve_data.use_fill_caps = True
    spline = curve_data.splines.new('BEZIER')
    spline.bezier_points.add(6)
    points = [
        (0.30, -0.66, 0.70),
        (0.43, -0.88, 0.56),
        (0.51, -1.04, 0.34),
        (0.47, -1.08, 0.16),
        (0.31, -1.00, 0.105),
        (0.08, -0.89, 0.10),
        (-0.17, -0.82, 0.11),
    ]
    radii = (1.08, 1.04, 1.0, 0.94, 0.86, 0.77, 0.68)
    for point, co, radius in zip(spline.bezier_points, points, radii):
        point.co = co
        point.radius = radius
        point.handle_left_type = 'AUTO'
        point.handle_right_type = 'AUTO'
    obj = bpy.data.objects.new('GEO_Tail', curve_data)
    collection.objects.link(obj)
    assign(obj, mat)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.convert(target='MESH')
    smooth(obj)
    return obj


def setup_camera_and_stage(presentation, mats):
    bpy.ops.mesh.primitive_plane_add(size=200, location=(0, 0, -0.012))
    ground = bpy.context.object
    ground.name = 'STAGE_Ground'
    assign(ground, mats['stage'])
    move_to_collection(ground, presentation)

    bpy.ops.object.camera_add(location=(3.25, 4.65, 2.25))
    camera = bpy.context.object
    camera.name = 'CAM_Preview'
    camera.data.lens = 58
    camera.data.sensor_width = 36
    camera.data.dof.use_dof = False
    move_to_collection(camera, presentation)
    bpy.context.scene.camera = camera

    target = Vector((0.0, 0.08, 0.84))
    direction = target - camera.location
    camera.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()

    def area(name, location, energy, size, color):
        bpy.ops.object.light_add(type='AREA', location=location)
        light = bpy.context.object
        light.name = name
        light.data.energy = energy
        light.data.shape = 'DISK'
        light.data.size = size
        light.data.color = color
        light.rotation_euler = (Vector((0.0, 0.1, 0.8)) - light.location).to_track_quat('-Z', 'Y').to_euler()
        move_to_collection(light, presentation)
        return light

    area('LIGHT_Key', (3.0, 3.5, 4.7), 850, 3.0, (1.0, 0.80, 0.68))
    area('LIGHT_Fill', (-3.5, 2.0, 2.6), 620, 3.5, (0.55, 0.95, 1.0))
    area('LIGHT_Rim', (-1.0, -3.0, 3.7), 780, 2.5, (1.0, 0.48, 0.60))

    world = bpy.context.scene.world or bpy.data.worlds.new('CatHome_PearlWorld')
    bpy.context.scene.world = world
    world.use_nodes = True
    world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.055, 0.038, 0.06, 1)
    world.node_tree.nodes['Background'].inputs['Strength'].default_value = 0.30


def create_metarig(rig_collection):
    import addon_utils
    enabled, loaded = addon_utils.check('rigify')
    if not loaded:
        try:
            bpy.ops.preferences.addon_enable(module='rigify')
        except Exception:
            pass
    operator_available = hasattr(bpy.ops.object, 'armature_cat_metarig_add')
    meta = None
    if operator_available:
        bpy.ops.object.armature_cat_metarig_add()
        meta = bpy.context.object
        meta.name = 'RIGIFY_Cat_Meta'
        meta['cat_home_fit_status'] = 'placeholder_only_phase_1'
        meta.hide_render = True
        meta.hide_set(True)
        move_to_collection(meta, rig_collection)
    return enabled, loaded, operator_available, meta


def build():
    import os
    os.makedirs(QA_PATH, exist_ok=True)
    clear_to_blank()

    geo = ensure_collection('CAT_GEO')
    face = ensure_collection('CAT_FACE')
    rig = ensure_collection('CAT_RIG')
    presentation = ensure_collection('CAT_PRESENTATION')

    root = bpy.data.objects.new('CHR_CatRoot', None)
    bpy.context.scene.collection.objects.link(root)
    root['variant_id'] = 'cream_caramel_prototype'
    root['topology_version'] = 2
    root['rig_profile'] = 'rigify_cat_plus_face'
    root['faces_positive_y'] = True

    mats = {
        'fur': material('CH_CAT_FurBase_Caramel', (0.43, 0.20, 0.085), roughness=0.76),
        'cream': material('CH_CAT_MuzzleChest_Cream', (0.88, 0.66, 0.40), roughness=0.74),
        'pattern': material('CH_CAT_PatternA_Cocoa', (0.095, 0.028, 0.012), roughness=0.78),
        'inner': material('CH_CAT_Skin_Pink', (0.86, 0.16, 0.20), roughness=0.56),
        'eye': material('CH_CAT_Iris_Amber', (0.70, 0.27, 0.025), roughness=0.20),
        'pupil': material('CH_CAT_Pupil_Navy', (0.008, 0.012, 0.025), roughness=0.18),
        'glint': material('CH_CAT_EyeGlint', (1.0, 0.96, 0.84), roughness=0.12),
        'whisker': material('CH_CAT_Whisker', (0.96, 0.82, 0.62), roughness=0.58),
        'tooth': material('CH_CAT_Teeth_Pearl', (1.0, 0.96, 0.80), roughness=0.35),
        'tongue': material('CH_CAT_Tongue_Coral', (1.0, 0.20, 0.32), roughness=0.55),
        'stage': material('CH_STAGE_Pearl', (0.055, 0.038, 0.06), roughness=0.72),
    }

    body = ellipsoid('GEO_Body', (0, -0.08, 0.73), (0.39, 0.67, 0.33), mats['fur'], geo, 16, 10)
    chest = ellipsoid('GEO_Chest', (0, 0.39, 0.79), (0.34, 0.36, 0.37), mats['fur'], geo, 14, 9)
    rump = ellipsoid('GEO_Rump', (0, -0.49, 0.78), (0.43, 0.43, 0.43), mats['fur'], geo, 14, 9)
    neck = ellipsoid('GEO_Neck', (0, 0.55, 1.04), (0.27, 0.26, 0.29), mats['fur'], geo, 12, 8)
    head = ellipsoid('GEO_Head', (0, 0.75, 1.34), (0.36, 0.30, 0.32), mats['fur'], geo, 14, 10)
    for obj in (body, chest, rump, neck, head):
        obj.parent = root

    ear('GEO_Ear_L', 0.20, mats['fur'], mats['inner'], geo)
    ear('GEO_Ear_R', -0.20, mats['fur'], mats['inner'], geo)

    ellipsoid('GEO_Muzzle_L', (0.105, 1.015, 1.245), (0.135, 0.085, 0.105), mats['cream'], face, 12, 8)
    ellipsoid('GEO_Muzzle_R', (-0.105, 1.015, 1.245), (0.135, 0.085, 0.105), mats['cream'], face, 12, 8)
    ellipsoid('GEO_Chin', (0, 0.998, 1.178), (0.125, 0.075, 0.062), mats['cream'], face, 12, 8)
    nose = prism('GEO_Nose', [(-0.055, 1.105, 1.305), (0.055, 1.105, 1.305), (0, 1.112, 1.255), (0, 1.045, 1.285)], [(0, 1, 2), (0, 3, 1), (0, 2, 3), (1, 3, 2)], mats['inner'], face)

    for side, x in [('L', 0.155), ('R', -0.155)]:
        eye = ellipsoid(f'GEO_Eye_{side}', (x, 1.010, 1.382), (0.122, 0.067, 0.132), mats['eye'], face, 16, 10)
        pupil = ellipsoid(f'GEO_Pupil_{side}', (x, 1.070, 1.382), (0.044, 0.022, 0.088), mats['pupil'], face, 12, 8)
        glint_x = x + (0.023 if side == 'L' else -0.023)
        ellipsoid(f'GEO_EyeGlint_{side}', (glint_x, 1.091, 1.430), (0.020, 0.010, 0.023), mats['glint'], face, 8, 5)
        lid = ellipsoid(f'GEO_EyelidUpper_{side}', (x, 1.045, 1.492), (0.128, 0.010, 0.016), mats['fur'], face, 12, 6)
        lid['future_shape_keys'] = 'Basis,Blink'
        lid.hide_render = True
        eye.parent = root
        pupil.parent = root
        lid.parent = root

    jaw = ellipsoid('GEO_Jaw', (0, 0.998, 1.180), (0.127, 0.076, 0.060), mats['cream'], face, 12, 8)
    jaw['future_control'] = 'jaw_master'
    upper_teeth = cone_between('GEO_TeethUpper', (-0.036, 1.075, 1.245), (-0.036, 1.080, 1.202), 0.020, 0.005, mats['tooth'], face, 8)
    lower_teeth = cone_between('GEO_TeethLower', (0.036, 1.072, 1.185), (0.036, 1.078, 1.224), 0.018, 0.004, mats['tooth'], face, 8)
    tongue = ellipsoid('GEO_Tongue', (0, 1.060, 1.188), (0.062, 0.048, 0.020), mats['tongue'], face, 10, 6)
    tongue['future_bones'] = 'tongue.01,tongue.02'
    upper_teeth.hide_render = True
    lower_teeth.hide_render = True
    tongue.hide_render = True

    # Front legs: shoulder, forearm and low three-toed paw silhouette.
    for side, x in [('L', 0.205), ('R', -0.205)]:
        shoulder = (x, 0.40, 0.76)
        elbow = (x, 0.455, 0.43)
        wrist = (x, 0.51, 0.145)
        ellipsoid(f'GEO_Shoulder_Front_{side}', shoulder, (0.145, 0.155, 0.19), mats['fur'], geo, 10, 7)
        cone_between(f'GEO_ForelegUpper_{side}', shoulder, elbow, 0.122, 0.100, mats['fur'], geo, 10)
        cone_between(f'GEO_ForelegLower_{side}', elbow, wrist, 0.100, 0.068, mats['fur'], geo, 10)
        paw = ellipsoid(f'GEO_Paw_Front_{side}', (x, 0.585, 0.068), (0.135, 0.195, 0.068), mats['cream'], geo, 12, 7)
        paw['ground_contact_z'] = 0.0
        for toe_index, toe_x in enumerate((-0.058, 0.0, 0.058), start=1):
            ellipsoid(f'GEO_Toe_Front_{side}_{toe_index}', (x + toe_x, 0.735, 0.052), (0.048, 0.070, 0.052), mats['cream'], geo, 10, 6)

    # Hind legs: visible hip-thigh volume, forward knee, rear hock and forward paw.
    for side, x in [('L', 0.285), ('R', -0.285)]:
        hip = (x, -0.47, 0.66)
        knee = (x, -0.20, 0.40)
        hock = (x, -0.50, 0.19)
        ankle = (x, -0.39, 0.115)
        ellipsoid(f'GEO_Thigh_Hind_{side}', (x, -0.43, 0.55), (0.205, 0.250, 0.285), mats['fur'], geo, 12, 8)
        cone_between(f'GEO_HindUpper_{side}', hip, knee, 0.135, 0.105, mats['fur'], geo, 10)
        cone_between(f'GEO_HindShin_{side}', knee, hock, 0.105, 0.072, mats['fur'], geo, 10)
        cone_between(f'GEO_HindHock_{side}', hock, ankle, 0.074, 0.062, mats['cream'], geo, 10)
        paw = ellipsoid(f'GEO_Paw_Hind_{side}', (x, -0.29, 0.068), (0.145, 0.220, 0.068), mats['cream'], geo, 12, 7)
        paw['ground_contact_z'] = 0.0
        for toe_index, toe_x in enumerate((-0.060, 0.0, 0.060), start=1):
            ellipsoid(f'GEO_Toe_Hind_{side}_{toe_index}', (x + toe_x, -0.115, 0.052), (0.050, 0.072, 0.052), mats['cream'], geo, 10, 6)

    # Soft chest patch and restrained forehead/torso pattern markers.
    ellipsoid('GEO_ChestPatch', (0, 0.725, 0.84), (0.215, 0.050, 0.285), mats['cream'], geo, 12, 8)
    for stripe_index, (x, angle) in enumerate(((-0.075, -0.24), (0.0, 0.0), (0.075, 0.24)), start=1):
        ellipsoid(f'GEO_ForeheadStripe_{stripe_index}', (x, 1.015, 1.535), (0.030, 0.014, 0.095), mats['pattern'], face, 9, 5, rotation=(0, angle, 0))
    for side, x in [('L', 0.382), ('R', -0.382)]:
        for stripe_index, (y, z, angle) in enumerate(((-0.10, 0.85, -0.42), (-0.30, 0.88, -0.24), (-0.50, 0.89, 0.08)), start=1):
            ellipsoid(f'GEO_BodyStripe_{side}_{stripe_index}', (x, y, z), (0.018, 0.135, 0.074), mats['pattern'], geo, 9, 5, rotation=(angle, 0, 0))
    for side, x in [('L', 0.325), ('R', -0.325)]:
        for stripe_index, (y, z, angle) in enumerate(((0.905, 1.355, -0.20), (0.900, 1.300, 0.10)), start=1):
            ellipsoid(f'GEO_CheekStripe_{side}_{stripe_index}', (x, y, z), (0.012, 0.078, 0.024), mats['pattern'], face, 9, 5, rotation=(angle, 0, 0))

    # Mouth split and light whiskers make the face read as a cat without fur cards.
    curve_line('GEO_MouthLine_L', [(0.0, 1.108, 1.255), (0.028, 1.110, 1.220), (0.100, 1.082, 1.205)], 0.006, mats['pattern'], face)
    curve_line('GEO_MouthLine_R', [(0.0, 1.108, 1.255), (-0.028, 1.110, 1.220), (-0.100, 1.082, 1.205)], 0.006, mats['pattern'], face)
    whisker_shapes = (
        ((0.080, 1.080, 1.275), (0.285, 1.115, 1.310), (0.485, 1.080, 1.350)),
        ((0.080, 1.085, 1.245), (0.300, 1.125, 1.245), (0.505, 1.095, 1.255)),
        ((0.075, 1.080, 1.215), (0.285, 1.110, 1.185), (0.470, 1.070, 1.165)),
    )
    for side, sign in [('L', 1), ('R', -1)]:
        for whisker_index, points in enumerate(whisker_shapes, start=1):
            mirrored = [(x * sign, y, z) for x, y, z in points]
            curve_line(f'GEO_Whisker_{side}_{whisker_index}', mirrored, 0.004, mats['whisker'], face)
    create_tail(geo, mats['pattern'])

    enabled, loaded, operator_available, meta = create_metarig(rig)
    root['rigify_enabled_at_build'] = bool(enabled or loaded or operator_available)
    root['rigify_cat_operator_available'] = bool(operator_available)

    setup_camera_and_stage(presentation, mats)

    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_EEVEE'
    scene.render.resolution_x = 768
    scene.render.resolution_y = 768
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.film_transparent = False
    scene.render.filepath = QA_PATH + r"\CatHome_LowPolyCat_Foundation_v2.png"
    scene.render.image_settings.color_mode = 'RGBA'
    scene.view_settings.look = 'AgX - Medium High Contrast'
    scene.render.filepath = QA_PATH + r"\CatHome_LowPolyCat_Foundation_v2.png"
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    bpy.ops.render.render(write_still=True)
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)

    tris = 0
    for obj in list(geo.objects) + list(face.objects):
        if obj.type == 'MESH':
            tris += sum(max(0, len(poly.vertices) - 2) for poly in obj.data.polygons)
    print({
        'blend': BLEND_PATH,
        'preview': scene.render.filepath,
        'rigify_enabled': bool(enabled or loaded or operator_available),
        'cat_metarig_operator': operator_available,
        'metarig_created': meta is not None,
        'foundation_triangles': tris,
        'phase': 'foundation_silhouette_reference_revision_v2',
    })


if __name__ == '__main__':
    build()
