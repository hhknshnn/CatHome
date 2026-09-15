"""Lift the low entrance garland above the cat's turning space."""
import bpy
from pathlib import Path
ROOT=Path(__file__).resolve().parent
MODEL=ROOT.parents[2]/'Assets/Art/PremiumFurniture/Models/BedroomStarCanopy_Premium.fbx'
SOURCE=ROOT/'BedroomStarCanopy_Source.blend'
if SOURCE.exists():bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
else:
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.fbx(filepath=str(MODEL))
obj=bpy.data.objects['BedroomStarCanopy'];m=obj.data
base=m.attributes.get('CatHome_original_position')
if base is None:
 base=m.attributes.new('CatHome_original_position','FLOAT_VECTOR','POINT')
 for v,item in zip(m.vertices,base.data):item.vector=v.co
for v,item in zip(m.vertices,base.data):v.co=item.vector
edges={v.index:set() for v in m.vertices}
for e in m.edges:a,b=e.vertices;edges[a].add(b);edges[b].add(a)
seen=set();changed=0
for v in m.vertices:
 if v.index in seen:continue
 todo=[v.index];group=[];seen.add(v.index)
 while todo:
  i=todo.pop();group.append(i)
  for j in edges[i]:
   if j not in seen:seen.add(j);todo.append(j)
 lo=[min(m.vertices[i].co[k] for i in group) for k in range(3)];hi=[max(m.vertices[i].co[k] for i in group) for k in range(3)]
 if lo[1]>.27 and hi[1]<.46 and hi[2]<-.18 and hi[2]-lo[2]<.03:
  for i in group:
   p=m.vertices[i].co.copy();m.vertices[i].co=(p.x*.45,p.y+.48,-.07+(p.z+.20)*.45)
  changed+=1
m.update();bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE))
bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
bpy.ops.export_scene.fbx(filepath=str(MODEL),use_selection=True,object_types={'MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
print('Entrance garland components moved:',changed)
