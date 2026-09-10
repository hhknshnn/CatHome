"""Bake original repeatable surface detail in Blender; no game mesh is changed.

Run: blender --background --factory-startup --python build_modern_surfaces.py
Packed linear RGB: red = neutral albedo / 2; green = roughness variation;
blue = micro-height. Unity samples one map in object-metric projection.
All patterns are periodic because their noise uses a four-dimensional torus.
"""
import bpy
import math
import os
import json
from mathutils import Vector

ROOT = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(ROOT, '..', '..', '..'))
OUT = os.path.join(PROJECT, 'Assets', 'Art', 'ModernPolish', 'Textures')
os.makedirs(OUT, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.samples = 16
scene.cycles.device = 'CPU'
scene.render.bake.margin = 0
scene.view_settings.view_transform = 'AgX'
scene.view_settings.look = 'None'
scene.view_settings.exposure = 0
scene.view_settings.gamma = 1
bpy.ops.mesh.primitive_plane_add(size=2)
plane = bpy.context.object
plane.name = 'SurfaceBakePlane'
FAMILIES = ['Oak', 'Linen', 'Stone', 'Ceramic', 'Plaster', 'Leaf', 'Rattan', 'Suede']
results = {}

def make_surface(family):
    material = bpy.data.materials.new('Modern_' + family)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    nodes.clear()
    links = material.node_tree.links
    def node(kind):
        return nodes.new(kind)
    def inp(socket, value):
        if hasattr(value, 'node'):
            links.new(value, socket)
        else:
            socket.default_value = value
    def calc(op, a, b=None):
        n = node('ShaderNodeMath'); n.operation = op
        inp(n.inputs[0], a)
        if b is not None: inp(n.inputs[1], b)
        return n.outputs[0]
    uv = node('ShaderNodeTexCoord')
    sep = node('ShaderNodeSeparateXYZ'); links.new(uv.outputs['UV'], sep.inputs[0])
    x, y = sep.outputs['X'], sep.outputs['Y']
    ax, ay = calc('MULTIPLY', x, math.tau), calc('MULTIPLY', y, math.tau)
    torus = node('ShaderNodeCombineXYZ')
    inp(torus.inputs[0], calc('COSINE', ax)); inp(torus.inputs[1], calc('SINE', ax)); inp(torus.inputs[2], calc('COSINE', ay))
    def noise(scale, detail=2):
        n = node('ShaderNodeTexNoise'); n.noise_dimensions = '4D'
        links.new(torus.outputs[0], n.inputs['Vector'])
        inp(n.inputs['W'], calc('SINE', ay)); n.inputs['Scale'].default_value = scale
        n.inputs['Detail'].default_value = detail; n.inputs['Roughness'].default_value = .64
        return n.outputs['Fac']
    def wave(axis, count, distortion=None):
        p = calc('MULTIPLY', axis, math.tau * count)
        if distortion is not None: p = calc('ADD', p, distortion)
        return calc('ADD', calc('MULTIPLY', calc('SINE', p), .5), .5)
    fine = noise(22, 2)
    soft = noise(2.5, 3)
    if family == 'Oak':
        grain = wave(x, 21, calc('MULTIPLY', soft, 10))
        pores = calc('POWER', grain, 7)
        h = calc('ADD', calc('MULTIPLY', pores, .55), calc('MULTIPLY', fine, .45))
        contrast = .19
    elif family in ('Linen', 'Rattan'):
        count = 62 if family == 'Linen' else 18
        warp, weft = wave(x, count), wave(y, count)
        h = calc('MULTIPLY', warp, weft)
        h = calc('ADD', calc('MULTIPLY', h, .78), calc('MULTIPLY', fine, .22))
        contrast = .18 if family == 'Linen' else .29
    elif family == 'Stone':
        h = calc('ADD', calc('MULTIPLY', soft, .65), calc('MULTIPLY', fine, .35))
        contrast = .29
    elif family == 'Ceramic':
        h = calc('ADD', calc('MULTIPLY', soft, .82), calc('MULTIPLY', fine, .18))
        contrast = .045
    elif family == 'Leaf':
        vein = wave(y, 16, calc('MULTIPLY', x, math.tau * 8))
        h = calc('ADD', calc('MULTIPLY', vein, .40), calc('MULTIPLY', soft, .60))
        contrast = .15
    elif family == 'Suede':
        h = calc('ADD', calc('MULTIPLY', fine, .85), calc('MULTIPLY', soft, .15))
        contrast = .16
    else:
        h = calc('ADD', calc('MULTIPLY', fine, .50), calc('MULTIPLY', soft, .50))
        contrast = .10
    packed = node('ShaderNodeCombineXYZ')
    inp(packed.inputs[0], calc('ADD', calc('MULTIPLY', calc('SUBTRACT', h, .5), contrast), .5))
    inp(packed.inputs[1], calc('ADD', calc('MULTIPLY', h, .55), .225))
    inp(packed.inputs[2], h)
    emission = node('ShaderNodeEmission'); links.new(packed.outputs[0], emission.inputs['Color'])
    output = node('ShaderNodeOutputMaterial'); links.new(emission.outputs[0], output.inputs['Surface'])
    image = bpy.data.images.new('Modern_' + family + '_Surface', width=512, height=512, alpha=False)
    image.colorspace_settings.name = 'Non-Color'
    image.file_format = 'PNG'; image.filepath_raw = os.path.join(OUT, image.name + '.png')
    target = node('ShaderNodeTexImage'); target.image = image; nodes.active = target
    plane.data.materials.clear(); plane.data.materials.append(material)
    bpy.ops.object.bake(type='EMIT')
    image.save()
    # The source .blend includes the live procedural recipe and its packed bake.
    image.pack()
    results[family] = {'width': 512, 'height': 512, 'bytes': os.path.getsize(image.filepath_raw), 'source': 'Blender Cycles emission bake', 'packed': ['albedoHalf', 'roughnessVariation', 'height']}
    return material, image

all_materials = []
for family in FAMILIES:
    material, image = make_surface(family)
    all_materials.append((family, material, image))

# A real Blender material review render; stored outside Assets and never packaged.
plane.hide_render = True
colors = [(0.63,.39,.18),(.11,.47,.43),(.65,.63,.54),(.05,.49,.54),(.24,.36,.45),(.10,.37,.12),(.50,.30,.13),(.72,.22,.20)]
roughness = [.48,.85,.82,.22,.88,.52,.80,.90]
for index, (family, recipe, texture) in enumerate(all_materials):
    preview = bpy.data.materials.new('Review_' + family); preview.use_nodes = True
    nt = preview.node_tree; p = nt.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value = (*colors[index], 1)
    p.inputs['Roughness'].default_value = roughness[index]
    coord = nt.nodes.new('ShaderNodeTexCoord'); tex = nt.nodes.new('ShaderNodeTexImage'); tex.image = texture
    tex.extension = 'REPEAT'; nt.links.new(coord.outputs['UV'], tex.inputs[0])
    split = nt.nodes.new('ShaderNodeSeparateColor'); nt.links.new(tex.outputs[0], split.inputs[0])
    bump = nt.nodes.new('ShaderNodeBump'); bump.inputs['Strength'].default_value = .30
    bump.inputs['Distance'].default_value = .023 if family in ('Linen','Rattan') else .006
    nt.links.new(split.outputs['Blue'], bump.inputs['Height']); nt.links.new(bump.outputs[0], p.inputs['Normal'])
    bpy.ops.mesh.primitive_uv_sphere_add(segments=48, ring_count=24, radius=.70, location=((index % 4)*1.65-2.475, (index // 4)*2.45-1.1, .72))
    ob=bpy.context.object; ob.name='Review_' + family; ob.data.materials.append(preview)
    for poly in ob.data.polygons: poly.use_smooth=True
    bpy.ops.object.text_add(location=(ob.location.x-.64, ob.location.y-.90, .015))
    label=bpy.context.object; label.data.body=family; label.data.size=.24
    label.data.extrude=.002; label.name='Label_'+family
    ink=bpy.data.materials.get('Review_Label') or bpy.data.materials.new('Review_Label')
    ink.use_nodes=True; ink.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.68,.82,1,1)
    label.data.materials.append(ink)

bpy.ops.mesh.primitive_plane_add(size=200, location=(0,0,-.02))
floor=bpy.context.object; floor.name='Review_Background'
mat=bpy.data.materials.new('Review_Background'); mat.use_nodes=True
mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.013,.031,.054,1)
mat.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.92; floor.data.materials.append(mat)
world=scene.world or bpy.data.worlds.new('World'); scene.world=world; world.use_nodes=True
world.node_tree.nodes['Background'].inputs[0].default_value=(.43,.55,.72,1)
world.node_tree.nodes['Background'].inputs[1].default_value=.35
for loc, energy, size in [((-4,-3,7),850,5),((4,2,5),600,4)]:
    bpy.ops.object.light_add(type='AREA', location=loc)
    light=bpy.context.object; light.data.energy=energy; light.data.shape='DISK'; light.data.size=size
    light.rotation_euler=(Vector((0,0,.6))-light.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(0,-4.8,11.6))
camera=bpy.context.object; camera.rotation_euler=(Vector((0,.15,.1))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO'; camera.data.ortho_scale=7.8; scene.camera=camera
scene.render.resolution_x=1600; scene.render.resolution_y=1080; scene.render.resolution_percentage=100
scene.cycles.samples=48; scene.cycles.use_denoising=True
scene.render.filepath=os.path.join(ROOT,'ModernSurface_Review.png')
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT,'ModernSurfaces_Source.blend'))
bpy.ops.render.render(write_still=True)
with open(os.path.join(ROOT,'surface_metrics.json'),'w',encoding='utf-8') as handle:
    json.dump({'families':results,'runtimeTextureSamplesPerPixel':1,'extraRuntimeLights':0,'geometryChanges':0,'textureTotalBytes':sum(x['bytes'] for x in results.values())},handle,indent=2)
print('MODERN_SURFACES_COMPLETE', json.dumps(results))
