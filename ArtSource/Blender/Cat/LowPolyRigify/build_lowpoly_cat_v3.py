"""
CatHome Low-Poly Cat v3
=======================
Calico (turuncu/siyah/beyaz) renk şeması.
Rigify cat metarig otomatik eklenir ve mesh'e fit edilir.
Render: 1024x1024 üç-çeyrek bakış.

Çalıştırma:
    blender --background --factory-startup --python build_lowpoly_cat_v3.py
"""

import bpy
import math
import os
from mathutils import Vector, Matrix, Euler

ROOT      = r"C:\Users\HAKAN\Desktop\CatHome\CatHome"
OUT_BLEND = ROOT + r"\ArtSource\Blender\Cat\LowPolyRigify\CatHome_LowPolyCat_v3.blend"
OUT_PNG   = ROOT + r"\Temp\CatRigQA\CatHome_LowPolyCat_v3_Preview.png"
os.makedirs(os.path.dirname(OUT_PNG), exist_ok=True)


# ─────────────────────────────────────────────────────────────
#  Yardımcı fonksiyonlar
# ─────────────────────────────────────────────────────────────

def clear_scene():
    if bpy.context.object and bpy.context.object.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for pool in (bpy.data.meshes, bpy.data.curves, bpy.data.armatures,
                 bpy.data.cameras, bpy.data.lights, bpy.data.materials,
                 bpy.data.actions):
        for blk in list(pool):
            if blk.users == 0:
                pool.remove(blk)
    bpy.context.scene.name = "CatHome_LowPolyCat_v3"


def col(name):
    c = bpy.data.collections.get(name)
    if not c:
        c = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(c)
    return c


def to_col(obj, c):
    for sc in list(obj.users_collection):
        sc.objects.unlink(obj)
    c.objects.link(obj)


def mat(name, rgb, roughness=0.72, metallic=0.0, emission=None):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value  = (*rgb, 1.0)
    bsdf.inputs['Roughness'].default_value   = roughness
    bsdf.inputs['Metallic'].default_value    = metallic
    if emission:
        bsdf.inputs['Emission Color'].default_value  = (*emission, 1.0)
        bsdf.inputs['Emission Strength'].default_value = 0.35
    m.diffuse_color = (*rgb, 1.0)
    return m


def set_shade_smooth(obj):
    if obj.type == 'MESH':
        for p in obj.data.polygons:
            p.use_smooth = True


def assign_mat(obj, m):
    if not obj.data.materials:
        obj.data.materials.append(m)
    else:
        obj.data.materials[0] = m


# ─────────────────────────────────────────────────────────────
#  Temel mesh oluşturucular
# ─────────────────────────────────────────────────────────────

def add_sphere(name, loc, scale, m, c, segs=10, rings=7, rot=(0,0,0)):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segs, ring_count=rings,
                                          location=loc, rotation=rot)
    o = bpy.context.object
    o.name = name
    o.scale = scale
    bpy.ops.object.transform_apply(scale=True)
    assign_mat(o, m)
    set_shade_smooth(o)
    to_col(o, c)
    return o


def add_cone(name, p0, p1, r0, r1, m, c, verts=8):
    v0, v1 = Vector(p0), Vector(p1)
    d = v1 - v0
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r0, radius2=r1,
                                     depth=d.length, location=(v0+v1)*0.5)
    o = bpy.context.object
    o.name = name
    o.rotation_mode = 'QUATERNION'
    o.rotation_quaternion = d.to_track_quat('Z','Y')
    assign_mat(o, m)
    set_shade_smooth(o)
    to_col(o, c)
    return o


def add_mesh(name, verts, faces, m, c):
    mesh = bpy.data.meshes.new(name + '_mesh')
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    o = bpy.data.objects.new(name, mesh)
    c.objects.link(o)
    assign_mat(o, m)
    return o


def add_curve_tube(name, pts, radius_seq, bevel_r, m, c, res=3):
    crv = bpy.data.curves.new(name + '_crv', 'CURVE')
    crv.dimensions = '3D'
    crv.resolution_u = res
    crv.bevel_depth = bevel_r
    crv.bevel_resolution = 1
    crv.use_fill_caps = True
    sp = crv.splines.new('BEZIER')
    sp.bezier_points.add(len(pts) - 1)
    for bp, co, r in zip(sp.bezier_points, pts, radius_seq):
        bp.co = co
        bp.radius = r
        bp.handle_left_type = 'AUTO'
        bp.handle_right_type = 'AUTO'
    o = bpy.data.objects.new(name, crv)
    c.objects.link(o)
    assign_mat(o, m)
    return o


# ─────────────────────────────────────────────────────────────
#  Malzemeler — Calico Kedi (turuncu, siyah, beyaz)
# ─────────────────────────────────────────────────────────────

def make_materials():
    return {
        'white'   : mat('CH_Cat_White',         (0.92, 0.90, 0.88), 0.74),
        'orange'  : mat('CH_Cat_Orange',         (0.85, 0.35, 0.05), 0.70),
        'black'   : mat('CH_Cat_Black',          (0.05, 0.04, 0.04), 0.78),
        'pink'    : mat('CH_Cat_Pink',           (0.95, 0.52, 0.56), 0.60),
        'amber'   : mat('CH_Cat_EyeAmber',       (0.72, 0.30, 0.03), 0.18),
        'pupil'   : mat('CH_Cat_Pupil',          (0.01, 0.01, 0.02), 0.15),
        'glint'   : mat('CH_Cat_Glint',          (1.00, 0.98, 0.92), 0.08, emission=(1,1,0.9)),
        'whisker' : mat('CH_Cat_Whisker',        (0.98, 0.90, 0.78), 0.55),
        'stage'   : mat('CH_Stage',              (0.06, 0.04, 0.07), 0.80),
    }


# ─────────────────────────────────────────────────────────────
#  Kulak
# ─────────────────────────────────────────────────────────────

def build_ear(name, x, outer_m, inner_m, c):
    s = 1 if x > 0 else -1
    ox = x
    verts = [
        (ox - 0.14*s, 0.72, 1.41),
        (ox + 0.14*s, 0.73, 1.42),
        (ox + 0.03*s, 0.76, 1.78),
        (ox - 0.11*s, 0.63, 1.41),
        (ox + 0.11*s, 0.64, 1.42),
        (ox + 0.03*s, 0.66, 1.74),
    ]
    faces = [(0,1,2),(5,4,3),(0,3,4,1),(1,4,5,2),(2,5,3,0)]
    ear = add_mesh(name, verts, faces, outer_m, c)
    inner_verts = [
        (ox - 0.08*s, 0.64, 1.46),
        (ox + 0.08*s, 0.645, 1.47),
        (ox + 0.02*s, 0.66, 1.68),
    ]
    add_mesh(name + '_inner', inner_verts, [(0,1,2)], inner_m, c)
    return ear


# ─────────────────────────────────────────────────────────────
#  Kuyruk (bezier)
# ─────────────────────────────────────────────────────────────

def build_tail(tail_m, tip_m, c):
    pts = [
        (0.28, -0.63, 0.68),
        (0.42, -0.84, 0.54),
        (0.50, -1.02, 0.32),
        (0.46, -1.07, 0.14),
        (0.30, -0.99, 0.09),
        (0.07, -0.88, 0.09),
        (-0.16, -0.82, 0.10),
    ]
    radii = (1.10, 1.06, 1.0, 0.92, 0.84, 0.75, 0.66)
    tail_crv = bpy.data.curves.new('GEO_Tail_crv', 'CURVE')
    tail_crv.dimensions = '3D'
    tail_crv.resolution_u = 4
    tail_crv.bevel_depth = 0.082
    tail_crv.bevel_resolution = 2
    tail_crv.use_fill_caps = True
    sp = tail_crv.splines.new('BEZIER')
    sp.bezier_points.add(len(pts)-1)
    for bp, co, r in zip(sp.bezier_points, pts, radii):
        bp.co = co
        bp.radius = r
        bp.handle_left_type = 'AUTO'
        bp.handle_right_type = 'AUTO'
    tail_obj = bpy.data.objects.new('GEO_Tail', tail_crv)
    c.objects.link(tail_obj)
    assign_mat(tail_obj, tail_m)
    # Çevirmek için seç + convert
    bpy.context.view_layer.objects.active = tail_obj
    tail_obj.select_set(True)
    bpy.ops.object.convert(target='MESH')
    set_shade_smooth(tail_obj)
    return tail_obj


# ─────────────────────────────────────────────────────────────
#  Ana kedi geometrisi
# ─────────────────────────────────────────────────────────────

def build_cat(mats):
    geo  = col('CAT_GEO')
    face = col('CAT_FACE')

    root = bpy.data.objects.new('CHR_CatRoot', None)
    bpy.context.scene.collection.objects.link(root)
    root['variant'] = 'calico_v3'
    root['faces_positive_y'] = True

    # ── Gövde ──
    body  = add_sphere('GEO_Body',  (0, -0.07, 0.74), (0.40, 0.68, 0.34), mats['white'],  geo, 14, 9)
    chest = add_sphere('GEO_Chest', (0,  0.40, 0.80), (0.35, 0.37, 0.38), mats['white'],  geo, 12, 8)
    rump  = add_sphere('GEO_Rump',  (0, -0.50, 0.79), (0.44, 0.44, 0.44), mats['orange'], geo, 12, 8)
    neck  = add_sphere('GEO_Neck',  (0,  0.56, 1.05), (0.27, 0.27, 0.29), mats['white'],  geo, 10, 7)
    head  = add_sphere('GEO_Head',  (0,  0.76, 1.35), (0.37, 0.31, 0.33), mats['white'],  geo, 12, 9)
    for o in (body, chest, rump, neck, head):
        o.parent = root

    # Calico yama: sol omuz üstünde turuncu leke
    patch_o = add_sphere('GEO_CalicoPatch_Orange', (-0.22, 0.15, 0.90),
                          (0.18, 0.10, 0.22), mats['orange'], geo, 10, 6)
    patch_b = add_sphere('GEO_CalicoPatch_Black',  ( 0.18, -0.22, 0.92),
                          (0.16, 0.09, 0.20), mats['black'],  geo, 10, 6)
    for o in (patch_o, patch_b):
        o.parent = root

    # ── Kulaklar ──
    build_ear('GEO_Ear_L',  0.21, mats['orange'], mats['pink'], geo)
    build_ear('GEO_Ear_R', -0.21, mats['black'],  mats['pink'], geo)

    # ── Yüz ──
    add_sphere('GEO_Muzzle_L', ( 0.11, 1.02, 1.24), (0.14, 0.09, 0.11), mats['white'], face, 10, 6)
    add_sphere('GEO_Muzzle_R', (-0.11, 1.02, 1.24), (0.14, 0.09, 0.11), mats['white'], face, 10, 6)
    add_sphere('GEO_Chin',     ( 0.00, 1.00, 1.17), (0.13, 0.08, 0.06), mats['white'], face, 10, 6)

    nose_v = [(-0.05, 1.11, 1.30), (0.05, 1.11, 1.30),
              ( 0.00, 1.11, 1.25), (0.00, 1.05, 1.28)]
    add_mesh('GEO_Nose', nose_v, [(0,1,2),(0,3,1),(0,2,3),(1,3,2)], mats['pink'], face)

    # Ağız eğrisi
    for side, sign in [('L',1),('R',-1)]:
        pts = [(0, 1.11, 1.255), (0.03*sign, 1.11, 1.22), (0.10*sign, 1.08, 1.205)]
        crv = bpy.data.curves.new(f'GEO_Mouth_{side}_crv', 'CURVE')
        crv.dimensions = '3D'; crv.bevel_depth = 0.006; crv.bevel_resolution = 1
        crv.use_fill_caps = True
        sp = crv.splines.new('BEZIER'); sp.bezier_points.add(2)
        for bp, co in zip(sp.bezier_points, pts):
            bp.co = co; bp.handle_left_type='AUTO'; bp.handle_right_type='AUTO'
        mo = bpy.data.objects.new(f'GEO_Mouth_{side}', crv)
        face.objects.link(mo); assign_mat(mo, mats['black'])

    # ── Gözler ──
    for side, x in [('L', 0.160), ('R', -0.160)]:
        eye   = add_sphere(f'GEO_Eye_{side}',   (x, 1.02, 1.385), (0.125, 0.070, 0.135), mats['amber'], face, 14, 9)
        pupil = add_sphere(f'GEO_Pupil_{side}', (x, 1.07, 1.385), (0.046, 0.024, 0.092), mats['pupil'], face, 10, 7)
        gx = x + (0.024 if side=='L' else -0.024)
        add_sphere(f'GEO_Glint_{side}', (gx, 1.09, 1.432), (0.021, 0.011, 0.024), mats['glint'], face, 6, 4)
        eye.parent   = root
        pupil.parent = root

    # ── Bıyıklar ──
    whisker_rows = [
        [(0.085, 1.085, 1.275), (0.28, 1.12, 1.31), (0.48, 1.085, 1.35)],
        [(0.085, 1.090, 1.245), (0.30, 1.13, 1.245),(0.50, 1.095, 1.255)],
        [(0.080, 1.085, 1.215), (0.28, 1.11, 1.185),(0.47, 1.072, 1.165)],
    ]
    for side, sign in [('L',1),('R',-1)]:
        for wi, row in enumerate(whisker_rows, 1):
            pts_m = [(x*sign,y,z) for x,y,z in row]
            wo = add_curve_tube(f'GEO_Whisker_{side}_{wi}', pts_m,
                                [1.0,1.0,1.0], 0.004, mats['whisker'], face)

    # ── Ön bacaklar ──
    for side, x in [('L', 0.21), ('R', -0.21)]:
        sh  = (x, 0.41, 0.77)
        el  = (x, 0.46, 0.43)
        wr  = (x, 0.51, 0.14)
        add_sphere(f'GEO_Shoulder_F_{side}', sh, (0.15, 0.16, 0.20), mats['white'], geo, 9,6)
        add_cone(f'GEO_ForeLegUp_{side}',   sh, el, 0.12, 0.10, mats['white'], geo, 9)
        add_cone(f'GEO_ForeLegLo_{side}',   el, wr, 0.10, 0.07, mats['white'], geo, 9)
        paw = add_sphere(f'GEO_Paw_F_{side}', (x, 0.59, 0.068), (0.14,0.20,0.07), mats['white'], geo,10,6)
        paw['ground_contact_z'] = 0.0
        for ti, tx in enumerate((-0.058,0,0.058),1):
            add_sphere(f'GEO_Toe_F_{side}_{ti}', (x+tx, 0.74, 0.052),(0.048,0.070,0.052), mats['white'], geo,8,5)

    # ── Arka bacaklar ──
    for side, x in [('L', 0.29), ('R', -0.29)]:
        hip   = (x, -0.47, 0.67)
        knee  = (x, -0.20, 0.40)
        hock  = (x, -0.51, 0.19)
        ankle = (x, -0.40, 0.115)
        thigh_m = mats['orange'] if side=='L' else mats['black']
        add_sphere(f'GEO_Thigh_{side}', (x,-0.44,0.56),(0.21,0.26,0.29), thigh_m, geo,11,7)
        add_cone(f'GEO_HindUp_{side}',  hip,  knee, 0.135, 0.105, thigh_m, geo, 9)
        add_cone(f'GEO_HindShin_{side}',knee, hock,  0.105, 0.072, mats['white'], geo, 9)
        add_cone(f'GEO_HindHock_{side}',hock, ankle, 0.075, 0.062, mats['white'], geo, 9)
        paw = add_sphere(f'GEO_Paw_H_{side}', (x,-0.30,0.068),(0.15,0.23,0.07), mats['white'], geo,10,6)
        paw['ground_contact_z'] = 0.0
        for ti, tx in enumerate((-0.060,0,0.060),1):
            add_sphere(f'GEO_Toe_H_{side}_{ti}', (x+tx,-0.115,0.052),(0.050,0.072,0.052), mats['white'], geo,8,5)

    # ── Kuyruk ──
    build_tail(mats['orange'], mats['white'], geo)

    return root, geo, face


# ─────────────────────────────────────────────────────────────
#  Rigify Metarig
# ─────────────────────────────────────────────────────────────

def build_metarig():
    import addon_utils
    enabled, loaded = addon_utils.check('rigify')
    if not loaded:
        try: bpy.ops.preferences.addon_enable(module='rigify')
        except: pass

    rig_col = col('CAT_RIG')
    has_op  = hasattr(bpy.ops.object, 'armature_cat_metarig_add')
    meta    = None

    if has_op:
        bpy.ops.object.armature_cat_metarig_add()
        meta = bpy.context.object
    else:
        # Manuel minimal cat armature (spine + neck + head + 4 legs + tail)
        bpy.ops.object.armature_add(location=(0,0,0))
        meta = bpy.context.object
        arm  = meta.data
        arm.name = 'Armature_CatMeta'
        bpy.ops.object.mode_set(mode='EDIT')
        eb = arm.edit_bones

        # Temizle
        for b in list(eb): eb.remove(b)

        def bone(nm, head, tail, parent=None, roll=0):
            b = eb.new(nm)
            b.head = Vector(head)
            b.tail = Vector(tail)
            b.roll = roll
            if parent: b.parent = eb[parent]; b.use_connect = True
            return b

        # Omurga
        bone('spine',       (0,0, 0.55), (0,0, 0.75))
        bone('spine.001',   (0,0, 0.75), (0,0, 0.94), 'spine')
        bone('spine.002',   (0,0, 0.94), (0,0, 1.05), 'spine.001')
        bone('neck',        (0,0.55,1.05),(0,0.72,1.22),'spine.002')
        bone('head',        (0,0.72,1.22),(0,0.78,1.65),'neck')

        # Ön sol bacak
        bone('upper_arm.L', ( 0.21, 0.41, 0.77),( 0.21, 0.46, 0.43))
        bone('forearm.L',   ( 0.21, 0.46, 0.43),( 0.21, 0.51, 0.14),'upper_arm.L')
        bone('hand.L',      ( 0.21, 0.51, 0.14),( 0.21, 0.59, 0.00),'forearm.L')
        # Ön sağ bacak
        bone('upper_arm.R', (-0.21, 0.41, 0.77),(-0.21, 0.46, 0.43))
        bone('forearm.R',   (-0.21, 0.46, 0.43),(-0.21, 0.51, 0.14),'upper_arm.R')
        bone('hand.R',      (-0.21, 0.51, 0.14),(-0.21, 0.59, 0.00),'forearm.R')
        # Arka sol bacak
        bone('thigh.L',     ( 0.29,-0.47, 0.67),( 0.29,-0.20, 0.40))
        bone('shin.L',      ( 0.29,-0.20, 0.40),( 0.29,-0.51, 0.19),'thigh.L')
        bone('foot.L',      ( 0.29,-0.51, 0.19),( 0.29,-0.30, 0.00),'shin.L')
        # Arka sağ bacak
        bone('thigh.R',     (-0.29,-0.47, 0.67),(-0.29,-0.20, 0.40))
        bone('shin.R',      (-0.29,-0.20, 0.40),(-0.29,-0.51, 0.19),'thigh.R')
        bone('foot.R',      (-0.29,-0.51, 0.19),(-0.29,-0.30, 0.00),'shin.R')
        # Kuyruk
        bone('tail.001',    (0.28,-0.63, 0.68),(0.44,-0.87, 0.50))
        bone('tail.002',    (0.44,-0.87, 0.50),(0.50,-1.05, 0.26),'tail.001')
        bone('tail.003',    (0.50,-1.05, 0.26),(0.30,-1.00, 0.09),'tail.002')
        bone('tail.004',    (0.30,-1.00, 0.09),(-0.16,-0.82,0.10),'tail.003')

        bpy.ops.object.mode_set(mode='OBJECT')

    meta.name = 'RIGIFY_Cat_Meta'
    meta.hide_render = True
    to_col(meta, rig_col)
    # Görünürlüğü koruyalım (sonradan edit edilebilsin)
    meta.hide_set(False)
    return meta


# ─────────────────────────────────────────────────────────────
#  Sahne, kamera, ışıklar
# ─────────────────────────────────────────────────────────────

def build_stage(mats):
    pres = col('CAT_PRESENTATION')

    # Zemin
    bpy.ops.mesh.primitive_plane_add(size=200, location=(0,0,-0.01))
    ground = bpy.context.object
    ground.name = 'STAGE_Ground'
    assign_mat(ground, mats['stage'])
    to_col(ground, pres)

    # Kamera — üç çeyrek bakış
    cam_loc = Vector((2.90, -4.30, 2.35))
    bpy.ops.object.camera_add(location=cam_loc)
    cam = bpy.context.object
    cam.name = 'CAM_Preview'
    cam.data.lens = 62
    cam.data.dof.use_dof = False
    target = Vector((0.0, 0.05, 0.80))
    cam.rotation_euler = (target - cam_loc).to_track_quat('-Z','Y').to_euler()
    bpy.context.scene.camera = cam
    to_col(cam, pres)

    def area_light(name, loc, energy, size, color):
        bpy.ops.object.light_add(type='AREA', location=loc)
        l = bpy.context.object
        l.name = name
        l.data.energy  = energy
        l.data.shape   = 'DISK'
        l.data.size    = size
        l.data.color   = color
        l.rotation_euler = (Vector((0.0,0.1,0.8)) - Vector(loc)).to_track_quat('-Z','Y').to_euler()
        to_col(l, pres)

    area_light('LIGHT_Key',  ( 2.8,  3.2, 4.5), 900,  3.0, (1.00, 0.82, 0.70))
    area_light('LIGHT_Fill', (-3.4,  2.0, 2.5), 650,  3.5, (0.55, 0.95, 1.00))
    area_light('LIGHT_Rim',  (-1.0, -3.0, 3.6), 800,  2.5, (1.00, 0.48, 0.60))

    world = bpy.data.worlds.new('CatHome_World_v3')
    bpy.context.scene.world = world
    world.use_nodes = True
    world.node_tree.nodes['Background'].inputs['Color'].default_value    = (0.05, 0.035, 0.06, 1)
    world.node_tree.nodes['Background'].inputs['Strength'].default_value = 0.28


# ─────────────────────────────────────────────────────────────
#  Render ayarları
# ─────────────────────────────────────────────────────────────

def setup_render():
    sc = bpy.context.scene
    sc.render.engine             = 'BLENDER_EEVEE_NEXT' if hasattr(bpy.context.scene.render, 'eevee') else 'BLENDER_EEVEE'
    sc.render.resolution_x       = 1024
    sc.render.resolution_y       = 1024
    sc.render.resolution_percentage = 100
    sc.render.film_transparent   = False
    sc.render.image_settings.file_format  = 'PNG'
    sc.render.image_settings.color_mode  = 'RGBA'
    sc.render.filepath           = OUT_PNG
    sc.view_settings.look        = 'AgX - Medium High Contrast'
    # EEVEE shadow kalitesi (Blender 5.x uyumlu)
    if hasattr(sc, 'eevee'):
        eevee = sc.eevee
        for attr, val in [('use_bloom', False), ('shadow_cube_size', '2048'),
                          ('shadow_cascade_size', '2048')]:
            if hasattr(eevee, attr):
                setattr(eevee, attr, val)


# ─────────────────────────────────────────────────────────────
#  İstatistik
# ─────────────────────────────────────────────────────────────

def count_tris(*collections):
    total = 0
    for c in collections:
        for o in c.objects:
            if o.type == 'MESH':
                total += sum(max(0, len(p.vertices)-2) for p in o.data.polygons)
    return total


# ─────────────────────────────────────────────────────────────
#  Ana giriş noktası
# ─────────────────────────────────────────────────────────────

def main():
    print("── CatHome Low-Poly Cat v3 başlıyor ──")
    clear_scene()

    mats = make_materials()
    root, geo, face = build_cat(mats)
    meta = build_metarig()
    build_stage(mats)
    setup_render()

    print(f"  Metarig oluşturuldu : {meta.name}")
    tris = count_tris(geo, face)
    print(f"  Toplam üçgen        : {tris}")

    bpy.ops.wm.save_as_mainfile(filepath=OUT_BLEND)
    print(f"  .blend kaydedildi   : {OUT_BLEND}")

    bpy.ops.render.render(write_still=True)
    print(f"  Render tamamlandı   : {OUT_PNG}")

    bpy.ops.wm.save_as_mainfile(filepath=OUT_BLEND)
    print("── Tamamlandı ──")


if __name__ == '__main__':
    main()
