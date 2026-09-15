"""Separate excavatable sand and remove the obsolete rigid paper tail.
Run headless. Inputs are archived once so rebuilding is deterministic.
The legacy furniture coordinates use Y up inside Blender; preserve that contract.
"""
import bpy, bmesh, pathlib, shutil, sys
from mathutils import Vector

ROOT = pathlib.Path(__file__).resolve().parents[3]
OUT = ROOT / 'Assets/Art/PremiumFurniture/Models'
HERE = pathlib.Path(__file__).resolve().parent
INPUT = HERE / 'Input'
INPUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)

def load(name):
    source = INPUT / name
    if not source.exists(): shutil.copy2(OUT / name, source)
    before = set(bpy.context.scene.objects)
    bpy.ops.import_scene.fbx(filepath=str(source))
    return next(o for o in set(bpy.context.scene.objects) - before if o.type == 'MESH')

def groups(obj):
    mesh=obj.data; parent=list(range(len(mesh.vertices)))
    def root(i):
        while parent[i]!=i: parent[i]=parent[parent[i]]; i=parent[i]
        return i
    for edge in mesh.edges:
        a,b=map(root,edge.vertices)
        if a!=b: parent[b]=a
    result={}
    for vertex in mesh.vertices: result.setdefault(root(vertex.index), []).append(vertex.index)
    return list(result.values())

def erase(obj, ids):
    bm=bmesh.new(); bm.from_mesh(obj.data); bm.verts.ensure_lookup_table()
    bmesh.ops.delete(bm, geom=[bm.verts[i] for i in ids], context='VERTS')
    bm.to_mesh(obj.data); bm.free(); obj.data.update()

def export(objects,name):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects: obj.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.export_scene.fbx(filepath=str(OUT/name),use_selection=True,object_types={'MESH'},
        apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',
        bake_space_transform=False,add_leaf_bones=False,use_mesh_modifiers=True,mesh_smooth_type='FACE',bake_anim=False)

if '--paper-only' not in sys.argv:
    tray=load('BathroomLitterBox_Premium.fbx')
    removed=[]
    for ids in groups(tray):
        points=[tray.matrix_world @ tray.data.vertices[i].co for i in ids]
        lo=[min(p[k] for p in points) for k in range(3)]
        hi=[max(p[k] for p in points) for k in range(3)]
        # Entire former solid sand slab and decorative blobs/scoop/bucket in its usable bed.
        if lo[1]>.10 and lo[0]>=-.596 and hi[0]<=.596 and lo[2]>=-.456 and hi[2]<=.456:
            removed.extend(ids)
    assert len(removed)>3000, len(removed)
    erase(tray,removed)
    export([tray],'BathroomLitterBox_Premium.fbx')

    mesh=bpy.data.meshes.new('BathroomLitterSand')
    mesh.from_pydata([(-.595,.195,-.455),(.595,.195,-.455),(.595,.195,.455),(-.595,.195,.455)],[],[(0,3,2,1)])
    mesh.update()
    sand=bpy.data.objects.new('BathroomLitterSand',mesh); bpy.context.collection.objects.link(sand)
    cream=next(m for m in tray.data.materials if m.name=='CH_Cream')
    mesh.materials.append(cream)
    uv=mesh.uv_layers.new(name='PaletteUV')
    # The existing premium pipeline replaces these named materials consistently.
    for item in uv.data: item.uv=(.5,.5)
    export([sand],'BathroomLitterSand_Premium.fbx')

roll=load('BathroomToiletRoll_Premium.fbx')
tails=[]
for ids in groups(roll):
    if min((roll.matrix_world @ roll.data.vertices[i].co).y for i in ids)<.50: tails.extend(ids)
assert len(tails)==156,len(tails)
erase(roll,tails)
for vertex in roll.data.vertices: vertex.co.z -= .60
export([roll],'BathroomToiletRoll_Premium.fbx')

# Move the real holder along the toilet's side, into the clear paw reach bay.
# Its X extent and the toilet's overall footprint stay unchanged.
toilet=load('BathroomToilet_Premium.fbx')
holder=[]
for ids in groups(toilet):
    points=[toilet.matrix_world @ toilet.data.vertices[i].co for i in ids]
    center=sum(points,Vector())/len(points)
    if center.x < -.20 and .59 < center.y < .81: holder.extend(ids)
assert len(holder)==562,len(holder)
for i in holder: toilet.data.vertices[i].co.z-=.60
gold=next(m for m in toilet.data.materials if m.name.startswith('CH_Gold'))
start=Vector((-.43,.46,.205)); end=Vector((-.43,.46,-.395))
bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=.024,depth=(end-start).length,location=(start+end)/2)
arm=bpy.context.object; arm.name='PaperHolderReachBridge';arm.rotation_euler=(end-start).to_track_quat('Z','Y').to_euler();arm.data.materials.append(gold)
bevel=arm.modifiers.new('Soft bracket edges','BEVEL');bevel.width=.006;bevel.segments=2
bpy.context.view_layer.objects.active=arm;bpy.ops.object.modifier_apply(modifier=bevel.name)
bpy.ops.object.select_all(action='DESELECT');toilet.select_set(True);arm.select_set(True);bpy.context.view_layer.objects.active=toilet;bpy.ops.object.join()
export([toilet],'BathroomToilet_Premium.fbx')
if '--paper-only' not in sys.argv:
    bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'BathroomActionParts_Source.blend'))
print('Rebuilt paper roll and connected holder only.' if '--paper-only' in sys.argv else
      'Preserved tray shell; replaced solid sand with an independent surface; rebuilt paper roll and holder.')
