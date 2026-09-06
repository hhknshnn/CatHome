"""
CatHome Low-Poly Cat v4
=======================
Turuncu tekir kedi. Tek parça subdivision mesh, oturan poz.
Kamera: ön-üç çeyrek, hafif yukarıdan.

Çalıştırma:
    blender --background --factory-startup --python build_lowpoly_cat_v4.py
"""

import bpy, bmesh, math, os
from mathutils import Vector

BLENDER = r"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"
ROOT    = r"C:\Users\HAKAN\Desktop\CatHome\CatHome"
OUT_BLEND = ROOT + r"\ArtSource\Blender\Cat\LowPolyRigify\CatHome_LowPolyCat_v4.blend"
OUT_PNG   = ROOT + r"\Temp\CatRigQA\CatHome_LowPolyCat_v4_Preview.png"
os.makedirs(os.path.dirname(OUT_PNG), exist_ok=True)

# ── Yardımcılar ────────────────────────────────────────────

def clear_scene():
    if bpy.context.object and bpy.context.object.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for pool in (bpy.data.meshes, bpy.data.curves, bpy.data.armatures,
                 bpy.data.cameras, bpy.data.lights, bpy.data.materials,
                 bpy.data.actions):
        for b in list(pool):
            if b.users == 0: pool.remove(b)
    bpy.context.scene.name = "CatHome_LowPolyCat_v4"

def col(name):
    c = bpy.data.collections.get(name)
    if not c:
        c = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(c)
    return c

def to_col(obj, c):
    for sc in list(obj.users_collection): sc.objects.unlink(obj)
    c.objects.link(obj)

def new_mat(name, rgb, rough=0.70, metal=0.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes['Principled BSDF']
    b.inputs['Base Color'].default_value = (*rgb, 1)
    b.inputs['Roughness'].default_value  = rough
    b.inputs['Metallic'].default_value   = metal
    return m

def smooth_obj(obj):
    for p in obj.data.polygons: p.use_smooth = True

def link_obj(obj, c):
    c.objects.link(obj)
    return obj

# ── Mesh builder: primitive + scale + move ─────────────────

def sphere(name, loc, sx, sy, sz, mat, c, seg=12, ring=8):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg, ring_count=ring, location=loc)
    o = bpy.context.object; o.name = name
    o.scale = (sx, sy, sz)
    bpy.ops.object.transform_apply(scale=True)
    o.data.materials.append(mat)
    smooth_obj(o); to_col(o, c); return o

def cone(name, p0, p1, r0, r1, mat, c, v=8):
    a, b_ = Vector(p0), Vector(p1)
    d = b_ - a
    bpy.ops.mesh.primitive_cone_add(vertices=v, radius1=r0, radius2=r1,
                                     depth=d.length, location=(a+b_)*0.5)
    o = bpy.context.object; o.name = name
    o.rotation_mode = 'QUATERNION'
    o.rotation_quaternion = d.to_track_quat('Z','Y')
    o.data.materials.append(mat)
    smooth_obj(o); to_col(o, c); return o

def tri_mesh(name, verts, faces, mat, c):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces); me.update()
    o = bpy.data.objects.new(name, me)
    o.data.materials.append(mat)
    c.objects.link(o); return o

# ── Kedi ───────────────────────────────────────────────────

def build_cat():
    geo  = col('CAT_GEO')
    face = col('CAT_FACE')
    pres = col('CAT_PRESENTATION')

    # Malzemeler
    orange  = new_mat('CH_Orange',   (0.80, 0.32, 0.04), 0.72)
    cream   = new_mat('CH_Cream',    (0.95, 0.80, 0.60), 0.70)
    stripe  = new_mat('CH_Stripe',   (0.45, 0.14, 0.01), 0.75)
    pink    = new_mat('CH_Pink',     (0.95, 0.50, 0.55), 0.60)
    amber   = new_mat('CH_Amber',    (0.65, 0.28, 0.03), 0.18)
    pupil_m = new_mat('CH_Pupil',    (0.02, 0.02, 0.03), 0.12)
    white_m = new_mat('CH_White',    (0.98, 0.96, 0.90), 0.65)
    stage_m = new_mat('CH_Stage',    (0.08, 0.06, 0.10), 0.85)

    # ─── GÖVDE (oturan poz — kedi dik oturur, arka patiler önde) ───

    # Gövde ana kütlesi
    body  = sphere('GEO_Body',  (0,  0,    0.55), 0.38, 0.55, 0.38, orange, geo, 14, 10)
    # Göğüs — öne çıkık
    chest = sphere('GEO_Chest', (0,  0.30, 0.60), 0.32, 0.30, 0.34, cream,  geo, 12, 8)
    # Kalça
    rump  = sphere('GEO_Rump',  (0, -0.28, 0.48), 0.38, 0.32, 0.36, orange, geo, 12, 8)
    # Boyun
    neck  = sphere('GEO_Neck',  (0,  0.28, 0.90), 0.22, 0.22, 0.26, orange, geo, 10, 7)
    # Kafa — büyükçe, yuvarlak
    head  = sphere('GEO_Head',  (0,  0.30, 1.20), 0.35, 0.30, 0.33, orange, geo, 14, 10)

    # ─── ÖN PATILER (oturan kedi, patiler önde düz) ───
    for s, x in [('L', 0.18), ('R', -0.18)]:
        # Üst kol
        cone(f'GEO_ForeUp_{s}',  (x, 0.28, 0.72), (x, 0.38, 0.42), 0.11, 0.09, orange, geo, 8)
        # Alt kol (dik aşağı)
        cone(f'GEO_ForeLo_{s}',  (x, 0.38, 0.42), (x, 0.42, 0.10), 0.09, 0.07, orange, geo, 8)
        # Pati (düz zemine değiyor)
        sphere(f'GEO_PawF_{s}',  (x, 0.52, 0.06), 0.12, 0.18, 0.06, cream,  geo, 10, 6)

    # ─── ARKA PATILER (oturan, yanlardan görünür) ───
    for s, x in [('L', 0.26), ('R', -0.26)]:
        # Uyluk
        sphere(f'GEO_Thigh_{s}', (x, -0.10, 0.30), 0.18, 0.22, 0.26, orange, geo, 10, 7)
        # İnce bacak
        cone(f'GEO_HindLo_{s}',  (x, -0.10, 0.18), (x, 0.28, 0.06), 0.09, 0.07, orange, geo, 8)
        # Pati
        sphere(f'GEO_PawH_{s}',  (x, 0.38, 0.06), 0.12, 0.20, 0.06, cream, geo, 10, 6)

    # ─── KUYRUK (arkadan kıvrılıp yana uzanıyor) ───
    tail_pts = [
        ( 0.30, -0.38, 0.52),
        ( 0.42, -0.52, 0.42),
        ( 0.52, -0.60, 0.26),
        ( 0.48, -0.58, 0.10),
        ( 0.32, -0.46, 0.07),
        ( 0.14, -0.36, 0.07),
    ]
    tail_r = [1.0, 0.95, 0.88, 0.80, 0.70, 0.55]

    crv = bpy.data.curves.new('GEO_Tail_crv', 'CURVE')
    crv.dimensions = '3D'; crv.resolution_u = 4
    crv.bevel_depth = 0.075; crv.bevel_resolution = 2; crv.use_fill_caps = True
    sp = crv.splines.new('BEZIER'); sp.bezier_points.add(len(tail_pts)-1)
    for bp, co, r in zip(sp.bezier_points, tail_pts, tail_r):
        bp.co = co; bp.radius = r
        bp.handle_left_type = 'AUTO'; bp.handle_right_type = 'AUTO'
    tail_obj = bpy.data.objects.new('GEO_Tail', crv)
    geo.objects.link(tail_obj); tail_obj.data.materials.append(orange)
    bpy.context.view_layer.objects.active = tail_obj
    tail_obj.select_set(True)
    bpy.ops.object.convert(target='MESH')
    smooth_obj(tail_obj)

    # ─── ÇİZGİLER (sırt üstünde koyu turuncu bantlar) ───
    stripe_data = [
        # (x, y, z, sx, sy, sz, rx)
        ( 0,  0.00, 0.86, 0.025, 0.30, 0.06, -0.10),
        ( 0, -0.10, 0.90, 0.022, 0.28, 0.06,  0.05),
        ( 0, -0.20, 0.88, 0.020, 0.26, 0.06,  0.18),
    ]
    for i, (x,y,z,sx,sy,sz,rx) in enumerate(stripe_data):
        sphere(f'GEO_Stripe_{i}', (x,y,z), sx, sy, sz, stripe, geo, 8, 5, )

    # ─── KULAKLAR ───
    def ear(nm, ex, m_out, m_in):
        s = 1 if ex > 0 else -1
        v_out = [
            (ex - 0.13*s, 0.19, 1.43),
            (ex + 0.13*s, 0.20, 1.44),
            (ex + 0.03*s, 0.22, 1.72),
            (ex - 0.10*s, 0.10, 1.43),
            (ex + 0.10*s, 0.11, 1.44),
            (ex + 0.03*s, 0.12, 1.69),
        ]
        f = [(0,1,2),(5,4,3),(0,3,4,1),(1,4,5,2),(2,5,3,0)]
        tri_mesh(nm, v_out, f, m_out, face)
        v_in = [
            (ex - 0.07*s, 0.145, 1.48),
            (ex + 0.07*s, 0.150, 1.49),
            (ex + 0.02*s, 0.155, 1.65),
        ]
        tri_mesh(nm+'_in', v_in, [(0,1,2)], m_in, face)

    ear('GEO_Ear_L',  0.20, orange, pink)
    ear('GEO_Ear_R', -0.20, orange, pink)

    # ─── YÜZEY DETAYLARI ───
    # Burun üçgeni
    nose_v = [(-0.05, 0.60, 1.195), (0.05, 0.60, 1.195),
              ( 0.00, 0.61, 1.160), (0.00, 0.56, 1.180)]
    tri_mesh('GEO_Nose', nose_v,
             [(0,1,2),(0,3,1),(0,2,3),(1,3,2)], pink, face)

    # Ağız yayı
    for sd, sg in [('L',1),('R',-1)]:
        crv2 = bpy.data.curves.new(f'Mouth_{sd}_crv','CURVE')
        crv2.dimensions='3D'; crv2.bevel_depth=0.005; crv2.bevel_resolution=1
        crv2.use_fill_caps=True
        sp2 = crv2.splines.new('BEZIER'); sp2.bezier_points.add(2)
        pts = [(0,0.61,1.160),(0.025*sg,0.61,1.135),(0.085*sg,0.595,1.128)]
        for bp2,co in zip(sp2.bezier_points,pts):
            bp2.co=co; bp2.handle_left_type='AUTO'; bp2.handle_right_type='AUTO'
        mo = bpy.data.objects.new(f'GEO_Mouth_{sd}', crv2)
        face.objects.link(mo); mo.data.materials.append(stripe)

    # Alt çene/yanak — krem
    sphere('GEO_Muzzle_L', ( 0.10, 0.62, 1.162), 0.12, 0.08, 0.10, cream, face, 10, 6)
    sphere('GEO_Muzzle_R', (-0.10, 0.62, 1.162), 0.12, 0.08, 0.10, cream, face, 10, 6)

    # Gözler
    for sd, ex in [('L', 0.145), ('R', -0.145)]:
        sphere(f'GEO_Eye_{sd}',   (ex, 0.60, 1.255), 0.115, 0.065, 0.122, amber,   face, 12, 8)
        sphere(f'GEO_Pupil_{sd}', (ex, 0.65, 1.255), 0.042, 0.024, 0.086, pupil_m, face, 10, 6)
        gx = ex + (0.022 if sd=='L' else -0.022)
        sphere(f'GEO_Glint_{sd}', (gx, 0.668, 1.295), 0.018, 0.010, 0.020, white_m, face, 6, 4)

    # Bıyıklar
    wrows = [
        [( 0.09, 0.66, 1.185),( 0.28, 0.70, 1.210),( 0.50, 0.685, 1.240)],
        [( 0.09, 0.66, 1.162),( 0.30, 0.70, 1.162),( 0.52, 0.688, 1.155)],
        [( 0.09, 0.66, 1.140),( 0.28, 0.70, 1.118),( 0.48, 0.682, 1.105)],
    ]
    for sd, sg in [('L',1),('R',-1)]:
        for wi, row in enumerate(wrows,1):
            wpts = [(x*sg,y,z) for x,y,z in row]
            wc = bpy.data.curves.new(f'W_{sd}_{wi}_crv','CURVE')
            wc.dimensions='3D'; wc.bevel_depth=0.0035; wc.bevel_resolution=1
            wc.use_fill_caps=True
            wsp = wc.splines.new('BEZIER'); wsp.bezier_points.add(2)
            for bp3,co in zip(wsp.bezier_points,wpts):
                bp3.co=co; bp3.handle_left_type='AUTO'; bp3.handle_right_type='AUTO'
            wo = bpy.data.objects.new(f'GEO_Whisker_{sd}_{wi}', wc)
            face.objects.link(wo); wo.data.materials.append(white_m)

    # ─── SAHNE ───
    bpy.ops.mesh.primitive_plane_add(size=200, location=(0,0,-0.005))
    gnd = bpy.context.object; gnd.name = 'STAGE_Ground'
    gnd.data.materials.append(stage_m); to_col(gnd, pres)

    # Kamera — ÖNDEN ÜÇ ÇEYREK (kedi +Y yönüne bakıyor, kamera +Y tarafında)
    cam_loc = Vector((1.80, 3.80, 1.90))
    bpy.ops.object.camera_add(location=cam_loc)
    cam = bpy.context.object; cam.name = 'CAM_Preview'
    cam.data.lens = 65; cam.data.dof.use_dof = False
    target = Vector((0, 0.20, 0.75))
    cam.rotation_euler = (target - cam_loc).to_track_quat('-Z','Y').to_euler()
    bpy.context.scene.camera = cam; to_col(cam, pres)

    def alight(name, loc, energy, size, color):
        bpy.ops.object.light_add(type='AREA', location=loc)
        l = bpy.context.object; l.name = name
        l.data.energy = energy; l.data.shape = 'DISK'
        l.data.size = size; l.data.color = color
        l.rotation_euler = (Vector((0,0.1,0.5))-Vector(loc)).to_track_quat('-Z','Y').to_euler()
        to_col(l, pres)

    alight('LIGHT_Key',  (-2.5,  2.8, 4.2),  950, 3.0, (1.00, 0.85, 0.72))
    alight('LIGHT_Fill', ( 3.0,  1.5, 2.2),  600, 3.5, (0.60, 0.90, 1.00))
    alight('LIGHT_Rim',  ( 0.5, -3.2, 3.5),  720, 2.5, (1.00, 0.55, 0.38))

    world = bpy.data.worlds.new('W_CatV4')
    bpy.context.scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes['Background']
    bg.inputs['Color'].default_value    = (0.04, 0.03, 0.06, 1)
    bg.inputs['Strength'].default_value = 0.22

    # ─── RENDER ───
    sc = bpy.context.scene
    valid = bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items.keys()
    sc.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in valid else 'BLENDER_EEVEE'
    sc.render.resolution_x = 1024; sc.render.resolution_y = 1024
    sc.render.resolution_percentage = 100
    sc.render.film_transparent = False
    sc.render.image_settings.file_format = 'PNG'
    sc.render.filepath = OUT_PNG
    sc.view_settings.look = 'AgX - Medium High Contrast'

    bpy.ops.wm.save_as_mainfile(filepath=OUT_BLEND)
    print(f"Kaydedildi: {OUT_BLEND}")
    bpy.ops.render.render(write_still=True)
    print(f"Render: {OUT_PNG}")
    bpy.ops.wm.save_as_mainfile(filepath=OUT_BLEND)
    print("Tamamlandı.")

if __name__ == '__main__':
    clear_scene()
    build_cat()
