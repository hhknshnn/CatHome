"""Preserve authored kitchen meshes, expose real fruit parts and clear their counter."""
from pathlib import Path
import bpy,bmesh,sys,json,shutil
HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[2]
MODELS=ROOT/'Assets/Art/PremiumFurniture/Models'
INPUT=HERE/'KitchenInput'
INPUT.mkdir(exist_ok=True)
sys.path.insert(0,str(HERE.parent/'PremiumFurniture'))
import premium_kit as kit

def pieces(asset):
    source=INPUT/(asset+'_Premium.fbx')
    if not source.exists():shutil.copy2(MODELS/source.name,source)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(source))
    obj=next(o for o in bpy.context.scene.objects if o.type=='MESH')
    original=obj.name
    bpy.context.view_layer.objects.active=obj;obj.select_set(True)
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00001);bm.to_mesh(obj.data);bm.free()
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.separate(type='LOOSE');bpy.ops.object.mode_set(mode='OBJECT')
    return original,[o for o in bpy.context.scene.objects if o.type=='MESH']
def bounds(obj):
    p=[obj.matrix_world@v.co for v in obj.data.vertices]
    return [min(v[k] for v in p) for k in range(3)],[max(v[k] for v in p) for k in range(3)]
def join(name,objects):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=bpy.context.object;o.name=name;o.data.name=name+'Mesh';return o
def export(objects,path):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_space_transform=False,add_leaf_bones=False,use_mesh_modifiers=True,mesh_smooth_type='FACE',bake_anim=False)

name,parts=pieces('KitchenIsland')
removed=[];kept=[]
for obj in parts:
    low,high=bounds(obj)
    if low[1]>.855:removed.append(obj.name);bpy.data.objects.remove(obj,do_unlink=True)
    else:kept.append(obj)
counter=join(name,kept)
export([counter],MODELS/'KitchenIsland_Premium.fbx')
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'KitchenCounter_Clear_Source.blend'))

name,parts=pieces('KitchenFruitBasket')
fruit=[];pear=[];frame=[]
for obj in parts:
    low,high=bounds(obj)
    if len(obj.data.vertices)==222:fruit.append(obj)
    elif low[1]>.59 and high[0]-low[0]<.21:pear.append(obj)
    else:frame.append(obj)
fruit.sort(key=lambda o:(bounds(o)[0][1],bounds(o)[0][0],bounds(o)[0][2]))
fruit.append(join('Pear',pear))
basket=join('BasketFrame',frame)
for index,obj in enumerate(fruit):
    obj.name='Fruit_'+str(index+1).zfill(2);obj.data.name=obj.name+'Mesh'
    # Object origin at its real centre gives each fruit an independent tumble.
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj;bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY',center='BOUNDS')
export([basket]+fruit,MODELS/'KitchenFruitBasket_Scatter.fbx')
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'KitchenFruitScatter_Source.blend'))
(HERE/'KitchenScatter_Metrics.json').write_text(json.dumps({'counterRemovedDecorations':len(removed),'fruits':len(fruit),'basketBounds':bounds(basket),'fruitBounds':{o.name:bounds(o) for o in fruit}},indent=2),encoding='utf-8')
print('Counter cleared; original basket frame and',len(fruit),'independent fruit meshes exported.')
