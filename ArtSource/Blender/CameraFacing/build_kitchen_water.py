"""Visible, shallow water inside the existing sink basin; existing Y-up authoring."""
import bpy, pathlib

HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parents[2]
OUT = ROOT / 'Assets/Art/PremiumFurniture/Models/KitchenSinkWater_Premium.fbx'
bpy.ops.wm.read_factory_settings(use_empty=True)
mesh = bpy.data.meshes.new('KitchenSinkWater')
# Basin inside edges are X +/- .30, Z +/- .20, counter Y .86.
mesh.from_pydata([(-.28,.80,-.18),(.28,.80,-.18),(.28,.80,.18),(-.28,.80,.18)], [], [(0,3,2,1)])
mesh.update()
obj = bpy.data.objects.new('KitchenSinkWater',mesh)
bpy.context.collection.objects.link(obj)
material = bpy.data.materials.new('CH_AquaBright')
material.diffuse_color = (.18,.72,.78,1)
material.use_nodes = True
material.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value = material.diffuse_color
material.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value = .24
mesh.materials.append(material)
uv = mesh.uv_layers.new(name='PaletteUV')
for loop in uv.data: loop.uv = (.5,.5)
obj.select_set(True)
bpy.context.view_layer.objects.active = obj
bpy.ops.export_scene.fbx(filepath=str(OUT),use_selection=True,object_types={'MESH'},
    apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',
    bake_space_transform=False,add_leaf_bones=False,use_mesh_modifiers=True,mesh_smooth_type='FACE',bake_anim=False)
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'KitchenWater_Source.blend'))
print('Visible basin water exported inside the unchanged sink footprint.')
