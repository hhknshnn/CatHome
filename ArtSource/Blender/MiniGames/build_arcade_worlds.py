"""Original toy-town / garden-clubhouse art, authored headlessly in metres.

Run Blender --background --factory-startup --python this_file.py.
Only this process is changed; the user's open Blender session is never touched.
The road contact plane stays .033 m, and the Catch court stays an empty 8x6 m.
Meshes use solid palette materials (no new texture downloads or light sources).
"""
import os
import sys
import math
import json
import struct
import zlib
import bpy
from mathutils import Vector

ROOT = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(ROOT, '..', 'PremiumFurniture'))
import premium_kit as k

OUTPUT = os.path.abspath(os.path.join(ROOT, '../../../Assets/Art/MiniGames/ArcadeWorlds/Models'))
k.UNITY_MODELS = OUTPUT
PALETTE = {
    'Arcade_Coral': (1.0, .255, .25),
    'Arcade_Sky': (.11, .58, .89),
    'Arcade_Teal': (.025, .49, .47),
    'Arcade_Mint': (.29, .80, .57),
    'Arcade_Sun': (1.0, .69, .115),
    'Arcade_Cream': (1.0, .925, .76),
    'Arcade_White': (1.0, .985, .94),
    'Arcade_Ink': (.04, .16, .225),
    'Arcade_Lilac': (.49, .32, .77),
    'Arcade_Road': (.36, .67, .71),
    'Arcade_RoadLight': (.48, .76, .77),
    'Arcade_Sand': (.94, .70, .41),
    'Arcade_Grass': (.245, .63, .38),
    'Arcade_Court': (.86, .925, .71),
}

k.clear_scene()
k.CH_COLORS.update(PALETTE)
M = k.palette(PALETTE)
atlas_folder = os.path.join(OUTPUT, '..', 'Textures')
os.makedirs(atlas_folder, exist_ok=True)
palette_names = list(PALETTE)
def png_chunk(kind, payload):
    return struct.pack('!I', len(payload)) + kind + payload + struct.pack('!I', zlib.crc32(kind + payload) & 0xffffffff)
scanline = bytearray([0])
for column in range(256):
    color = PALETTE[palette_names[min(column // 16, len(palette_names) - 1)]]
    scanline.extend(round(channel * 255) for channel in color)
atlas_path = os.path.join(atlas_folder, 'ArcadePalette.png')
with open(atlas_path, 'wb') as handle:
    handle.write(b'\x89PNG\r\n\x1a\n' + png_chunk(b'IHDR', struct.pack('!2I5B', 256, 16, 8, 2, 0, 0, 0)) +
                 png_chunk(b'sRGB', b'\0') + png_chunk(b'IDAT', zlib.compress(bytes(scanline) * 16)) +
                 png_chunk(b'IEND', b''))
atlas = bpy.data.images.load(atlas_path)
atlas.colorspace_settings.name = 'sRGB'
palette_material = bpy.data.materials.new('ArcadePalette')
palette_material.use_nodes = True
image_node = palette_material.node_tree.nodes.new('ShaderNodeTexImage')
image_node.image = atlas
image_node.interpolation = 'Closest'
principled = palette_material.node_tree.nodes.get('Principled BSDF')
palette_material.node_tree.links.new(image_node.outputs['Color'], principled.inputs['Base Color'])
principled.inputs['Roughness'].default_value = .64
parts = []
exports = []
metrics = {}


def box(name, pos, size, color, bevel=.035, rotation=(0, 0, 0)):
    ob = k.cube(name, pos, size, M[color], bevel, rotation=rotation, segments=2)
    parts.append(ob)
    return ob


def ball(name, pos, size, color, rotation=None):
    ob = k.sphere(name, pos, size, M[color], 16, 8, rotation=rotation)
    parts.append(ob)
    return ob


def cylinder(name, pos, radius, height, color, axis='Y', sides=16):
    # Bevel-free small cylinders avoid expensive 3-ring caps repeated on props.
    ob = k.cylinder(name, pos, radius, height, M[color], axis=axis, vertices=sides, width=0)
    parts.append(ob)
    return ob


def tube(name, points, radius, color):
    cu = bpy.data.curves.new(name, 'CURVE')
    cu.dimensions = '3D'
    cu.resolution_u = 1
    cu.bevel_depth = radius
    cu.bevel_resolution = 1
    sp = cu.splines.new('POLY')
    sp.points.add(len(points) - 1)
    for dst, src in zip(sp.points, points):
        dst.co = (*src, 1)
    ob = bpy.data.objects.new(name, cu)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(M[color])
    bpy.ops.object.select_all(action='DESELECT')
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.convert(target='MESH')
    ob.select_set(False)
    parts.append(ob)
    return ob


def prism(name, vertices, depth, color):
    # Triangular and heraldic signs use real volume rather than a UI quad.
    coords = [(x, y, z + dz) for dz in [-depth / 2, depth / 2] for x, y, z in vertices]
    count = len(vertices)
    faces = [tuple(range(count - 1, -1, -1)), tuple(range(count, count * 2))]
    faces += [(i, (i + 1) % count, (i + 1) % count + count, i + count) for i in range(count)]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(coords, [], faces)
    mesh.update()
    ob = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(ob)
    mesh.materials.append(M[color])
    parts.append(ob)
    return ob


def paw(pos, size=1, color='Arcade_Sun'):
    x, y, z = pos
    ball('PawPad', (x, y, z), (.20 * size, .155 * size, .045 * size), color)
    for dx, dy, tilt in [(-.225, .18, -.35), (-.08, .29, -.1), (.08, .29, .1), (.225, .18, .35)]:
        ball('PawToe', (x + dx * size, y + dy * size, z), (.072 * size, .104 * size, .048 * size), color,
             (0, 0, tilt))


def fish(pos, size=1, color='Arcade_White'):
    x, y, z = pos
    ball('FishBody', (x, y, z), (.37 * size, .205 * size, .09 * size), color)
    prism('FishTail', [(x + .27 * size, y, z), (x + .61 * size, y + .23 * size, z),
                       (x + .61 * size, y - .23 * size, z)], .12 * size, color)
    ball('FishEye', (x - .2 * size, y + .05 * size, z - .085 * size), (.032 * size, .04 * size, .02 * size), 'Arcade_Ink')
    tube('FishSmile', [(x - .25 * size, y - .04 * size, z - .087 * size),
                      (x - .18 * size, y - .08 * size, z - .095 * size),
                      (x - .12 * size, y - .055 * size, z - .095 * size)], .011 * size, 'Arcade_Coral')


def leaf(pos, size, yaw, color='Arcade_Mint'):
    x, y, z = pos
    ball('SculptedLeaf', (x, y, z), (.19 * size, .34 * size, .06 * size), color,
         (math.radians(20), yaw, math.radians(25)))


def planter(x, z, size=1, y=0):
    cylinder('PlanterFoot', (x, y + .055 * size, z), .25 * size, .11 * size, 'Arcade_Ink')
    box('GardenPot', (x, y + .22 * size, z), (.55 * size, .34 * size, .53 * size), 'Arcade_Coral', .065 * size)
    box('PlanterLip', (x, y + .39 * size, z), (.60 * size, .07 * size, .58 * size), 'Arcade_Sun', .028 * size)
    cylinder('Soil', (x, y + .425 * size, z), .235 * size, .015 * size, 'Arcade_Ink')
    for i in range(5):
        angle = i * math.tau / 5
        dx, dz = math.cos(angle) * .17 * size, math.sin(angle) * .17 * size
        tube('Stem', [(x, y + .4 * size, z), (x + dx, y + .79 * size, z + dz)], .012 * size, 'Arcade_Teal')
        leaf((x + dx, y + .82 * size, z + dz), size, -angle,
             'Arcade_Mint' if i % 2 else 'Arcade_Grass')


def save(name):
    global parts
    ob = k.join_fixture(name, parts)
    # One tiny colour palette and one submesh per model, including the clubhouse.
    # Every triangle samples the centre of a swatch; there are no texture seams.
    original_materials = list(ob.data.materials)
    uv_layer = ob.data.uv_layers.new(name='ArcadeSwatchUV')
    ob.data.uv_layers.active = uv_layer
    for poly in ob.data.polygons:
        material_name = original_materials[poly.material_index].name
        swatch = palette_names.index(material_name)
        for loop in poly.loop_indices:
            uv_layer.data[loop].uv = ((swatch + .5) / 16, .5)
        poly.material_index = 0
    # The exporter uses the active-render UV channel. Remove inherited primitive UVs.
    for layer in list(ob.data.uv_layers):
        if layer != uv_layer:
            ob.data.uv_layers.remove(layer)
    uv_layer.active_render = True
    ob.data.materials.clear()
    ob.data.materials.append(palette_material)
    verts = [ob.matrix_world @ v.co for v in ob.data.vertices]
    lo = [min(v[i] for v in verts) for i in range(3)]
    hi = [max(v[i] for v in verts) for i in range(3)]
    metrics[name] = {'min': lo, 'max': hi, 'size': [hi[i] - lo[i] for i in range(3)],
                     'vertices': len(verts), 'triangles': sum(len(p.vertices) - 2 for p in ob.data.polygons),
                     'materials': 1, 'paletteSwatches': len(original_materials)}
    k.export_fixture(ob, name + '.fbx')
    # Keep the complete source collection in a single saved blend, spaced for inspection.
    ob.location.x = len(exports) * 18
    exports.append(ob)
    parts = []


def street():
    # The full road crown remains .033 m: no movement or platform profile changes.
    box('PouredRoad', (0, -.007, 0), (4.82, .076, 8), 'Arcade_Road', 0)
    for x in [-1.60, 0, 1.60]:
        for z in [-2, 2]:
            box('LaneTint', (x, .0325, z), (1.53, .001, 3.98), 'Arcade_RoadLight', 0)
    for side in [-1, 1]:
        box('PaintedEdge', (side * 2.385, .0325, 0), (.025, .001, 8), 'Arcade_Cream', 0)
        box('WarmPromenade', (side * 2.98, .059, 0), (1.13, .19, 8), 'Arcade_Sand', .018)
        box('KerbLip', (side * 2.445, .085, 0), (.105, .14, 8), 'Arcade_Cream', .012)
        # Broad inset paving joints read as motion without neon streaks or extra particles.
        for i in range(8):
            box('PromenadeJoint', (side * 2.99, .155, -3.5 + i), (1.035, .004, .017), 'Arcade_Cream', 0)
        for i in range(4):
            box('KerbTile', (side * 2.446, .113, -3.45 + i * 2), (.106, .072, .82), 'Arcade_Coral', .003)
    save('ArcadeStreet')


def shop(index):
    body = ['Arcade_Coral', 'Arcade_Sky', 'Arcade_Sun'][index]
    accent = ['Arcade_Sun', 'Arcade_Coral', 'Arcade_Teal'][index]
    height = [2.90, 3.13, 3.33][index]
    box('ShopPlinth', (0, .11, 0), (2.87, .22, 1.9), 'Arcade_Cream', .055)
    box('CandyStucco', (0, height / 2 + .20, .04), (2.66, height, 1.72), body, .09)
    box('RoofShadowLine', (0, height + .21, .04), (2.88, .15, 1.94), 'Arcade_Ink', .035)
    box('RoofFascia', (0, height + .31, .04), (2.92, .12, 1.98), accent, .035)
    # The repeated street is recognizable through three different shop personalities.
    for x in [-.79, .79]:
        box('WindowSurround', (x, height - .30, -.853), (.88, .83, .12), 'Arcade_Cream', .13)
        box('WindowGlass', (x, height - .30, -.926), (.70, .66, .034), 'Arcade_Ink', .105)
        box('WindowReflection', (x - .17, height - .22, -.95), (.045, .39, .016), 'Arcade_Sky', .01,
            (0, 0, -.23))
        box('WindowSill', (x, height - .75, -.92), (.98, .075, .27), accent, .028)
    # Large friendly storefront glazing, cream outline and dimensional striped awning.
    for x in [-.76, .64]:
        width = .82 if x < 0 else 1.21
        box('ShopfrontSurround', (x, .89, -.873), (width + .13, 1.31, .12), 'Arcade_Cream', .10)
        box('ShopfrontGlass', (x, .88, -.947), (width, 1.16, .028), 'Arcade_Ink', .07)
        box('GlassGlint', (x - width * .3, 1.02, -.970), (.043, .63, .014), 'Arcade_Sky', .006,
            (0, 0, -.2))
    cylinder('DoorHandle', (-.49, .88, -.99), .026, .23, 'Arcade_Sun')
    box('WindowDisplayBase', (.64, .43, -.976), (1.18, .08, .075), accent, .012)
    for i in range(4):
        ball('WindowDisplay', (.21 + i * .28, .59, -.975), (.095, .105, .058),
             ['Arcade_Coral', 'Arcade_Sun', 'Arcade_Mint', 'Arcade_Lilac'][i])
    for i in range(10):
        x = -1.35 + (i + .5) * .27
        color = accent if i % 2 == 0 else 'Arcade_White'
        box('AwningStripe', (x, 1.80, -1.05), (.274, .10, .61), color, .014,
            (math.radians(-12), 0, 0))
        ball('AwningScallop', (x, 1.645, -1.344), (.139, .12, .044), color)
    # The roof icon is the shop name: works in every game language.
    cylinder('RoofSignFrame', (0, height + .87, -.49), .54, .10, 'Arcade_Cream', 'Z', 24)
    cylinder('RoofSignFace', (0, height + .87, -.556), .475, .043, accent, 'Z', 24)
    for x in [-.32, .32]:
        box('SignSupport', (x, height + .50, -.37), (.052, .50, .065), 'Arcade_Ink', .008)
    if index == 0:
        fish((-.08, height + .86, -.62), .88)
    elif index == 1:
        ball('YarnShopEmblem', (0, height + .87, -.66), (.305, .305, .13), 'Arcade_White')
        for j in range(4):
            r = .12 + j * .049
            tube('YarnWinding', [(math.cos(t * math.tau / 18) * r,
                                 height + .87 + math.sin(t * math.tau / 18) * r, -.801)
                                for t in range(19)], .012, 'Arcade_Sky')
        tube('LooseYarn', [(.24, height + .74, -.71), (.39, height + .62, -.66),
                           (.32, height + .51, -.61)], .022, 'Arcade_White')
    else:
        paw((0, height + .75, -.621), .9, 'Arcade_White')
    # Footway planters give the lower silhouette more variety without blocking the lane.
    planter(1.17, -.85, .56)
    save('ArcadeFacade' + str(index))


def arch():
    for side in [-1, 1]:
        box('PortalFoot', (side * 2.92, .13, 0), (.45, .26, .68), 'Arcade_Sun', .065)
        box('PortalPost', (side * 2.92, 1.65, 0), (.18, 3.10, .23), 'Arcade_Teal', .045)
        for y in [.4, 1.15, 1.9, 2.65]:
            box('PortalStripe', (side * 2.92, y, -.122), (.19, .20, .03), 'Arcade_Cream', .008)
    # Lowest overhead geometry is 3.20 m; this cannot be mistaken for a slide obstacle.
    tube('SunsetArch', [(math.cos(i * math.pi / 24) * 2.92,
                        3.2 + math.sin(i * math.pi / 24) * .76, 0) for i in range(25)], .12, 'Arcade_Coral')
    tube('InnerArchTrim', [(math.cos(i * math.pi / 24) * 2.92,
                           3.2 + math.sin(i * math.pi / 24) * .76 - .16, .045) for i in range(25)], .038, 'Arcade_Sun')
    cylinder('FestivalMedallion', (0, 3.9, -.06), .43, .10, 'Arcade_Teal', 'Z', 24)
    paw((0, 3.75, -.13), 1.0, 'Arcade_Cream')
    for x in [-2.2, -1.5, -.85, .85, 1.5, 2.2]:
        y = 3.2 + .72 * math.sqrt(1 - (x / 2.92) ** 2)
        prism('FestivalPennant', [(x - .13, y - .18, .07), (x + .13, y - .18, .07),
                                 (x, y - .52, .07)], .016, 'Arcade_Sun' if x < 0 else 'Arcade_Sky')
    save('ArcadeFestivalArch')


def palm():
    cylinder('PalmPotFoot', (0, .05, 0), .31, .10, 'Arcade_Ink')
    box('PalmPot', (0, .26, 0), (.73, .45, .68), 'Arcade_Sun', .09)
    box('PalmPotRim', (0, .48, 0), (.79, .065, .74), 'Arcade_Cream', .023)
    tube('PalmTrunk', [(0, .45, 0), (.04, .98, 0), (.13, 1.57, .025), (.10, 2.12, .04)], .077, 'Arcade_Sand')
    for i in range(7):
        a = i * math.tau / 7
        pts = [( .10 + math.cos(a) * r, y, .04 + math.sin(a) * r)
               for r, y in [(.02, 2.14), (.20, 2.35), (.58, 2.23), (.91, 1.96)]]
        # Broad gently bending leaves, 12 triangles per frond.
        vertices = []
        widths = [.025, .16, .20, .012]
        for (x, y, z), w in zip(pts, widths):
            vertices.extend([(x - math.sin(a) * w, y, z + math.cos(a) * w),
                             (x + math.sin(a) * w, y, z - math.cos(a) * w)])
        faces = [(j * 2, j * 2 + 1, j * 2 + 3, j * 2 + 2) for j in range(3)]
        mesh = bpy.data.meshes.new('PalmFrond'); mesh.from_pydata(vertices, [], faces); mesh.update()
        ob = bpy.data.objects.new('PalmFrond', mesh); bpy.context.collection.objects.link(ob)
        mesh.materials.append(M['Arcade_Mint' if i % 2 else 'Arcade_Grass'])
        k.solidify(ob, .018); parts.append(ob)
    save('ArcadePalm')


def clouds():
    for x, y, z, s in [(-7.5, 6.8, 0, 1), (-3.9, 8.2, 3, .7), (4.9, 7.4, 0, 1.25), (9, 5.8, 2, .75)]:
        for dx, dy, scale in [(-.7, 0, .6), (0, .22, .95), (.84, .01, .72)]:
            ball('Cloud', (x + dx * s, y + dy * s, z), (1.1 * scale * s, .49 * scale * s, .7 * scale * s), 'Arcade_White')
    save('ArcadeCloudBank')


def arena():
    # Landscape surrounds the play plane; all solid-looking props are outside it.
    box('GardenIsland', (0, -.34, .42), (12.5, .55, 9.8), 'Arcade_Teal', .28)
    box('GardenLawn', (0, -.09, .42), (12.2, .10, 9.5), 'Arcade_Grass', .045)
    box('CourtUnderlay', (0, -.043, 0), (8.18, .105, 6.18), 'Arcade_Cream', .03)
    box('OpenPlaySurface', (0, .014, 0), (8, .006, 6), 'Arcade_Court', .002)
    # Rounded track graphics, flush printed lines rather than collectible-like floor objects.
    tube('CourtOuterPrint', [(-3.64, .018, -2.2), (-3.64, .018, 2.2), (-3.4, .018, 2.58),
                            (3.4, .018, 2.58), (3.64, .018, 2.2), (3.64, .018, -2.2),
                            (3.4, .018, -2.58), (-3.4, .018, -2.58), (-3.64, .018, -2.2)], .015, 'Arcade_White')
    # Low border rails expose the cat and all target mice to the player camera.
    for side in [-1, 1]:
        box('CourtSideCushion', (side * 4.13, .22, 0), (.25, .43, 6.3), 'Arcade_Teal', .07)
        box('CourtSideCap', (side * 4.13, .45, 0), (.30, .08, 6.35), 'Arcade_Sun', .025)
        for z in [-2.7, -1.35, 0, 1.35, 2.7]:
            box('SidePanel', (side * 4.278, .25, z), (.032, .29, 1.17), 'Arcade_Coral' if z == 0 else 'Arcade_Sky', .01)
        # Toy stadium terraces are safely outside x=4.3.
        for level in range(2):
            box('Bleacher', (side * (4.58 + .36 * level), .18 + .22 * level, .27),
                (.39, .18, 3.38), 'Arcade_Cream', .035)
            for z in [-.91, -.23, .45, 1.13]:
                box('TerraceSeat', (side * (4.59 + .36 * level), .30 + .22 * level, z),
                    (.32, .07, .54), ['Arcade_Coral', 'Arcade_Sky'][level], .025)
        planter(side * 4.7, -2.52, 1.0)
        planter(side * 5.02, 2.74, 1.35)
    # The back clubhouse is a whole destination, not the previous empty beige wall.
    box('ClubhouseStep', (0, .075, 3.62), (8.68, .15, 1.08), 'Arcade_Sun', .045)
    box('ClubhouseLower', (0, .55, 3.67), (8.42, .91, .38), 'Arcade_Coral', .06)
    for x in [-3.37, -2.10, 2.10, 3.37]:
        box('ClubhousePanel', (x, .56, 3.45), (1.02, .59, .06), 'Arcade_Cream', .075)
        fish((x - .065, .57, 3.39), .57, 'Arcade_Teal')
    box('ClubhouseRim', (0, 1.055, 3.61), (8.65, .12, .63), 'Arcade_Cream', .045)
    for x in [-3.65, -1.62, 1.62, 3.65]:
        box('RoofPost', (x, 1.94, 3.79), (.13, 1.78, .18), 'Arcade_Teal', .038)
    # Long fabric canopy with scallops frames the tiny central cat-ear clubhouse sign.
    for i in range(18):
        x = -4.3 + (i + .5) * 8.6 / 18
        color = 'Arcade_Teal' if i % 2 else 'Arcade_White'
        box('PavilionCanopy', (x, 2.98, 4.20), (8.6 / 18 + .015, .11, 1.40), color, .012,
            (math.radians(-7), 0, 0))
        ball('CanopyScallop', (x, 2.78, 3.51), (.245, .17, .035), color)
    box('ClubEmblem', (0, 1.94, 3.48), (2.38, 1.72, .16), 'Arcade_Teal', .20)
    box('EmblemInset', (0, 1.96, 3.37), (2.12, 1.47, .075), 'Arcade_Sun', .19)
    for side in [-1, 1]:
        prism('CatEar', [(side * .94, 2.65, 3.47), (side * 1.0, 3.22, 3.47),
                         (side * .39, 2.7, 3.47)], .17, 'Arcade_Teal')
        prism('InnerEar', [(side * .87, 2.76, 3.36), (side * .92, 3.06, 3.36),
                           (side * .59, 2.79, 3.36)], .022, 'Arcade_Coral')
    paw((0, 1.71, 3.295), 1.82, 'Arcade_White')
    # Two oversized toy displays are clearly outside chase bounds behind the court.
    for side in [-1, 1]:
        box('ToyDisplay', (side * 3.05, 1.22, 3.72), (1.55, .25, .84), 'Arcade_Sky', .06)
        for i in range(3):
            x = side * 3.05 - .38 + i * .38
            ball('JumboYarn', (x, 1.62 + .12 * (i == 1), 3.58), (.25, .25, .23),
                 ['Arcade_Lilac', 'Arcade_Coral', 'Arcade_Sun'][i])
    # Rounded bushes and low corner flowerbeds make the near perimeter intentional.
    for side in [-1, 1]:
        for z in [-3.72, 3.95]:
            box('Flowerbed', (side * 5.1, .055, z), (1.52, .18, .59), 'Arcade_Cream', .065)
            for i in range(4):
                x = side * 5.1 - .50 + i * .33
                ball('GardenBush', (x, .26, z), (.23, .25, .23), 'Arcade_Mint')
                ball('Flower', (x - .03, .47, z - .07), (.075, .065, .075), 'Arcade_Coral' if i % 2 else 'Arcade_Sun')
    save('ArcadeGardenArena')


street()
for index in range(3):
    shop(index)
arch()
palm()
clouds()
arena()

with open(os.path.join(ROOT, 'arcade_worlds_metrics.json'), 'w', encoding='utf-8') as handle:
    json.dump({'models': metrics, 'palette': PALETTE,
               'constraints': {'runnerContactY': .033, 'catchEmptyCourt': [8, 6],
                               'paletteTexture': [256, 16], 'extraLights': 0, 'extraCameras': 0}}, handle, indent=2)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT, 'ArcadeWorlds_Source.blend'))
print('ARCADE_WORLDS_COMPLETE', json.dumps(metrics))
