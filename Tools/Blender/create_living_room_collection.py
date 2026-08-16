import bpy
import math
import os


ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
MODEL_DIR = os.path.join(ROOT, "Assets", "Art", "StoreProducts", "Models")
SOURCE_PATH = os.path.join(MODEL_DIR, "CatHome_LivingRoomCollection.blend")


def clean_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def material(name, color, metallic=0.0, roughness=0.65):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metallic
    return mat


ORANGE = material("CH_Orange", (0.89, 0.31, 0.08), roughness=0.58)
ORANGE_LIGHT = material("CH_OrangeLight", (1.0, 0.56, 0.24), roughness=0.62)
TEAL = material("CH_Teal", (0.06, 0.42, 0.43), roughness=0.55)
TEAL_LIGHT = material("CH_TealLight", (0.18, 0.67, 0.65), roughness=0.55)
CREAM = material("CH_Cream", (0.96, 0.79, 0.52), roughness=0.7)
GOLD = material("CH_Gold", (0.78, 0.46, 0.08), metallic=0.35, roughness=0.35)
INK = material("CH_Ink", (0.045, 0.055, 0.07), roughness=0.42)
SCREEN = material("CH_Screen", (0.02, 0.18, 0.25), metallic=0.15, roughness=0.25)
PURPLE = material("CH_Purple", (0.35, 0.20, 0.55), roughness=0.6)
PINK = material("CH_Pink", (0.91, 0.45, 0.50), roughness=0.62)


def assign(obj, mat):
    obj.data.materials.append(mat)
    return obj


def bevel(obj, amount=0.03, segments=2):
    mod = obj.modifiers.new("Soft premium edges", "BEVEL")
    mod.width = amount
    mod.segments = segments
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=mod.name)
    return obj


def cube(name, location, scale, mat, edge=0.0, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if edge:
        bevel(obj, edge)
    return assign(obj, mat)


def cylinder(name, location, radius, depth, mat, vertices=16, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices, radius=radius, depth=depth, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    return assign(obj, mat)


def sphere(name, location, radius, scale, mat):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=radius, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return assign(obj, mat)


def cone(name, location, radius1, radius2, depth, mat, vertices=16):
    bpy.ops.mesh.primitive_cone_add(
        vertices=vertices, radius1=radius1, radius2=radius2, depth=depth, location=location)
    obj = bpy.context.object
    obj.name = name
    return assign(obj, mat)


def parent_all(objects, name):
    root = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(root)
    for obj in objects:
        obj.parent = root
    return root


def sofa():
    o = [
        cube("Sofa_Base", (0, 0, .34), (1.05, .43, .18), ORANGE, .08),
        cube("Sofa_Back", (0, .34, .88), (1.02, .16, .55), ORANGE, .09,
             (math.radians(-5), 0, 0)),
        cube("Seat_Left", (-.51, -.06, .59), (.48, .37, .14), ORANGE_LIGHT, .08),
        cube("Seat_Right", (.51, -.06, .59), (.48, .37, .14), ORANGE_LIGHT, .08),
        cube("Cushion_Left", (-.51, .24, .91), (.47, .11, .38), ORANGE, .08,
             (math.radians(-4), 0, 0)),
        cube("Cushion_Right", (.51, .24, .91), (.47, .11, .38), ORANGE, .08,
             (math.radians(-4), 0, 0)),
        cube("Arm_Left", (-1.08, -.01, .64), (.14, .45, .32), CREAM, .07),
        cube("Arm_Right", (1.08, -.01, .64), (.14, .45, .32), CREAM, .07),
    ]
    for x in (-.84, .84):
        o.append(cube("Sofa_Leg", (x, -.25, .11), (.07, .07, .12), INK, .015))
    return parent_all(o, "TwoSeatSofa")


def coffee_table():
    o = [cylinder("Table_Top", (0, 0, .55), .67, .15, ORANGE_LIGHT, 20)]
    o.append(cylinder("Table_Inlay", (0, 0, .635), .51, .025, CREAM, 20))
    o.append(cylinder("Table_Stem", (0, 0, .30), .12, .45, TEAL, 14))
    o.append(cylinder("Table_Base", (0, 0, .08), .34, .14, INK, 16))
    for a in range(0, 360, 120):
        rad = math.radians(a)
        o.append(cube("Table_Foot", (.30*math.cos(rad), .30*math.sin(rad), .035),
                      (.20, .045, .035), INK, .015, (0, 0, rad)))
    return parent_all(o, "CoffeeTable")


def tv_console():
    o = [
        cube("Console_Body", (0, 0, .34), (1.0, .31, .32), ORANGE_LIGHT, .07),
        cube("Console_Top", (0, 0, .68), (1.05, .34, .055), CREAM, .025),
        cube("Cabinet_Left", (-.52, -.322, .34), (.45, .025, .25), ORANGE, .025),
        cube("Cabinet_Right", (.52, -.322, .34), (.45, .025, .25), ORANGE, .025),
        cube("Screen_Frame", (0, .02, 1.24), (.78, .09, .52), INK, .055),
        cube("Screen", (0, -.078, 1.24), (.70, .018, .44), SCREEN, .025),
        cube("Tv_Stand", (0, .02, .75), (.18, .13, .07), INK, .02),
    ]
    for x in (-.82, .82):
        o.append(cube("Console_Leg", (x, 0, .07), (.06, .06, .10), GOLD, .015))
    return parent_all(o, "TvConsole")


def floor_lamp():
    o = [
        cylinder("Lamp_Base", (0, 0, .07), .30, .14, INK, 18),
        cylinder("Lamp_Base_Rim", (0, 0, .15), .23, .035, GOLD, 18),
        cylinder("Lamp_Stem", (0, 0, .95), .035, 1.62, GOLD, 12),
        cone("Lamp_Shade", (0, 0, 1.73), .36, .19, .56, ORANGE_LIGHT, 18),
        cylinder("Lamp_Glow", (0, 0, 1.47), .16, .035, CREAM, 18),
    ]
    return parent_all(o, "FloorLamp")


def side_table():
    o = [
        cylinder("Side_Top", (0, 0, .64), .46, .13, ORANGE_LIGHT, 18),
        cylinder("Side_Inlay", (0, 0, .715), .34, .025, CREAM, 18),
        cylinder("Side_Stem", (0, 0, .36), .10, .48, GOLD, 12),
        cylinder("Side_Base", (0, 0, .10), .27, .14, INK, 16),
    ]
    return parent_all(o, "SideTable")


def carpet():
    o = [
        cube("Carpet_Base", (0, 0, .025), (1.25, .86, .025), TEAL, .10),
        cube("Carpet_Center", (0, 0, .054), (.98, .59, .012), TEAL_LIGHT, .10),
    ]
    for x in (-.90, -.45, 0, .45, .90):
        o.append(cube("Carpet_Detail", (x, 0, .072), (.045, .50, .009), CREAM, .018,
                      (0, 0, math.radians(24))))
    return parent_all(o, "WovenCarpet")


def bookshelf():
    o = [
        cube("Shelf_Back", (0, .17, .93), (.70, .07, .93), TEAL, .035),
        cube("Shelf_Left", (-.70, 0, .93), (.075, .25, .93), ORANGE, .035),
        cube("Shelf_Right", (.70, 0, .93), (.075, .25, .93), ORANGE, .035),
        cube("Shelf_Top", (0, 0, 1.86), (.77, .28, .075), ORANGE_LIGHT, .035),
        cube("Shelf_Base", (0, 0, .08), (.77, .28, .08), ORANGE_LIGHT, .035),
    ]
    for z in (.48, .93, 1.38):
        o.append(cube("Shelf_Board", (0, 0, z), (.68, .25, .045), CREAM, .018))
    colors = [PINK, CREAM, PURPLE, ORANGE_LIGHT, TEAL_LIGHT]
    for shelf, z in enumerate((.27, .71, 1.16, 1.61)):
        for i in range(5):
            x = -.50 + i * .25
            h = .25 + ((i + shelf) % 3) * .045
            o.append(cube("Book", (x, -.08, z), (.08, .08, h), colors[(i+shelf) % len(colors)], .012,
                          (0, 0, math.radians((i % 3 - 1) * 3))))
    return parent_all(o, "TallBookshelf")


def houseplant():
    o = [
        cone("Plant_Pot", (0, 0, .28), .30, .23, .48, ORANGE_LIGHT, 16),
        cylinder("Plant_Rim", (0, 0, .51), .32, .10, CREAM, 16),
    ]
    for i, (x, y, z, rz) in enumerate((
            (0, 0, .95, 0), (-.16, .02, .90, -25), (.16, -.01, 1.05, 25),
            (-.10, .02, 1.30, -14), (.12, 0, 1.48, 14), (0, 0, 1.68, 0))):
        o.append(cylinder("Plant_Stem", (x*.45, y, .77 + (z-.9)*.5), .018, max(.3, z-.42), GOLD, 8,
                          (0, math.radians(rz*.45), 0)))
        o.append(sphere("Plant_Leaf", (x, y, z), .28, (1.0, .32, .58),
                        TEAL_LIGHT if i % 2 else TEAL))
    return parent_all(o, "TallHouseplant")


def painting():
    o = [
        cube("Painting_Frame", (0, 0, .55), (.56, .055, .55), GOLD, .035),
        cube("Painting_Canvas", (0, -.061, .55), (.48, .016, .47), CREAM, .015),
        cube("Painting_Teal", (-.15, -.082, .59), (.22, .008, .32), TEAL, .035,
             (0, 0, math.radians(-16))),
        cube("Painting_Orange", (.18, -.085, .43), (.19, .009, .24), ORANGE_LIGHT, .05,
             (0, 0, math.radians(20))),
        sphere("Painting_Sun", (.18, -.105, .76), .11, (1, .20, 1), PINK),
    ]
    return parent_all(o, "ModernPainting")


def wall_clock():
    o = [
        cylinder("Clock_Frame", (0, 0, .40), .38, .10, GOLD, 24, (math.pi/2, 0, 0)),
        cylinder("Clock_Face", (0, -.056, .40), .32, .018, CREAM, 24, (math.pi/2, 0, 0)),
        cube("Clock_Hand_Hour", (-.055, -.074, .46), (.025, .012, .13), INK, .01,
             (0, 0, math.radians(-28))),
        cube("Clock_Hand_Min", (.075, -.078, .50), (.018, .011, .18), TEAL, .008,
             (0, 0, math.radians(40))),
        sphere("Clock_Pin", (0, -.094, .40), .035, (1, .45, 1), ORANGE),
    ]
    for a in range(0, 360, 30):
        rad = math.radians(a)
        o.append(sphere("Clock_Marker", (.255*math.sin(rad), -.09, .40+.255*math.cos(rad)),
                        .018, (1, .35, 1), INK))
    return parent_all(o, "RoundWallClock")


def export_root(root, filename):
    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    for child in root.children_recursive:
        child.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.fbx(
        filepath=os.path.join(MODEL_DIR, filename), use_selection=True,
        use_mesh_modifiers=True, add_leaf_bones=False, bake_anim=False,
        object_types={'EMPTY', 'MESH', 'OTHER'})


def main():
    os.makedirs(MODEL_DIR, exist_ok=True)
    clean_scene()
    definitions = [
        (sofa(), "TwoSeatSofa.fbx"),
        (coffee_table(), "CoffeeTable.fbx"),
        (tv_console(), "TvConsole.fbx"),
        (floor_lamp(), "FloorLamp.fbx"),
        (side_table(), "SideTable.fbx"),
        (carpet(), "WovenCarpet.fbx"),
        (bookshelf(), "TallBookshelf.fbx"),
        (houseplant(), "TallHouseplant.fbx"),
        (painting(), "ModernPainting.fbx"),
        (wall_clock(), "RoundWallClock.fbx"),
    ]
    for index, (root, _) in enumerate(definitions):
        root.location = ((index % 5) * 3.2 - 6.4, (index // 5) * 3.2, 0)
    bpy.ops.wm.save_as_mainfile(filepath=SOURCE_PATH)
    for root, filename in definitions:
        root.location = (0, 0, 0)
        export_root(root, filename)


main()
