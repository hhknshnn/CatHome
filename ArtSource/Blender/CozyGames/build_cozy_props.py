"""Original beveled props. Adds an isolated scene; never clears the user's scene."""
import bpy, math, os, json
from mathutils import Vector
BASE=r'C:/Users/HAKAN/Desktop/CatHome/CatHome'
OUT=BASE+'/Assets/Art/CozyGames/Models'
SRC=BASE+'/ArtSource/Blender/CozyGames'
os.makedirs(OUT,exist_ok=True);os.makedirs(SRC,exist_ok=True)
if not os.path.exists(SRC+'/Blender_Before.blend'):
    bpy.ops.wm.save_as_mainfile(filepath=SRC+'/Blender_Before.blend',copy=True)
scene=bpy.data.scenes.new('CatHome_CozyGames')
bpy.context.window.scene=scene
colors={'Sage':(.35,.54,.45,1),'Petrol':(.10,.27,.29,1),'Mint':(.49,.72,.62,1),'Coral':(.82,.35,.27,1),'Oak':(.45,.28,.15,1),'Honey':(.71,.52,.29,1),'Cream':(.88,.81,.64,1),'Ink':(.035,.07,.075,1),'Pearl':(.95,.91,.78,1),'Water':(.13,.52,.49,1)}
mats={}
for n,c in colors.items():
    m=bpy.data.materials.new('Cozy_'+n);m.diffuse_color=c;m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=c;bs.inputs['Roughness'].default_value=.52 if n!='Water' else .24
    mats[n]=m
objects=[];metrics={}
def xyz(p):return(p[0],p[2],p[1])
def meshfinish(o,n,mat):
    o.name=n;o.data.materials.append(mats[mat]);objects.append(o)
    for f in o.data.polygons:f.use_smooth=True
    return o
def box(n,p,s,mat,bevel=.04):
    bpy.ops.mesh.primitive_cube_add(size=1,location=xyz(p));o=bpy.context.object;o.dimensions=xyz(s)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    mod=o.modifiers.new('Soft crafted edge','BEVEL');mod.width=bevel;mod.segments=3
    bpy.ops.object.modifier_apply(modifier=mod.name);o.modifiers.new('Weighted normals','WEIGHTED_NORMAL')
    return meshfinish(o,n,mat)
def sphere(n,p,s,mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=12,location=xyz(p));o=bpy.context.object;o.scale=xyz(s)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return meshfinish(o,n,mat)
def cord(n,points,width,mat,cyclic=False):
    c=bpy.data.curves.new(n,'CURVE');c.dimensions='3D';c.bevel_depth=width;c.bevel_resolution=2;c.resolution_u=1
    sp=c.splines.new('POLY');sp.points.add(len(points)-1)
    for a,b in zip(sp.points,points):a.co=(*xyz(b),1)
    sp.use_cyclic_u=cyclic;o=bpy.data.objects.new(n,c);scene.collection.objects.link(o);o.data.materials.append(mats[mat]);objects.append(o);return o
def save(name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.object.convert(target='MESH');bpy.ops.object.join();o=bpy.context.object;o.name=name
    scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    bpy.ops.export_scene.fbx(filepath=OUT+'/'+name+'.fbx',use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,use_mesh_modifiers=True)
    metrics[name]={'vertices':len(o.data.vertices),'faces':len(o.data.polygons)};o.hide_set(True);objects.clear()

# Yarn threads follow the sphere instead of a smooth, featureless ball.
sphere('Yarn core',(0,0,0),(.21,.21,.21),'Coral')
for i in range(13):
    a=i*math.pi/13
    pts=[]
    for j in range(90):
        t=j*math.tau/90;r=.216+.002*math.sin(t*6+i)
        pts.append((r*math.cos(t)*math.cos(a),r*math.sin(t),r*math.cos(t)*math.sin(a)))
    cord('Wound wool',pts,.006,'Coral',True)
save('YarnBall')

# Low open basket: the mouth is visible from the gameplay camera.
box('Woven base',(0,.025,0),(.92,.06,.83),'Honey',.10)
for row in range(7):
    y=.075+row*.033
    cord('Basket weave',[(.47*math.cos(a),y,.42*math.sin(a)) for a in [j*math.tau/64 for j in range(64)]],.018,'Cream' if row%2 else 'Honey',True)
for i in range(30):
    a=i*math.tau/30;cord('Vertical weave',[(.465*math.cos(a),.065,.415*math.sin(a)),(.47*math.cos(a),.285,.42*math.sin(a))],.009,'Honey')
cord('Rolled rim',[(.48*math.cos(j*math.tau/64),.30,.43*math.sin(j*math.tau/64)) for j in range(64)],.029,'Oak',True)
save('Basket')

box('Cushion',(0,.14,0),(1,.28,1),'Mint',.13)
cord('Piped seam',[(.46*math.cos(j*math.tau/80)*abs(math.cos(j*math.tau/80))**(-.6) if abs(math.cos(j*math.tau/80))>.001 else 0,.15,.46*math.sin(j*math.tau/80)*abs(math.sin(j*math.tau/80))**(-.6) if abs(math.sin(j*math.tau/80))>.001 else 0) for j in range(80)],.012,'Cream',True)
sphere('Tuft',(0,.283,0),(.044,.012,.044),'Sage');save('Cushion')

for kind in range(6):
    color=['Coral','Mint','Honey','Petrol','Pearl','Sage'][kind]
    sphere('Fish body',(0,0,0),(.14,.085,.27),color)
    sphere('Tail left',(-.085,0,-.29),(.105,.026,.11),color);sphere('Tail right',(.085,0,-.29),(.105,.026,.11),color)
    sphere('Dorsal fin',(0,.07,-.015),(.015,.065,.095),'Cream')
    for side in [-1,1]:
        sphere('Fin',(side*.145,-.01,-.04),(.075,.018,.07),'Cream')
        sphere('Eye pearl',(side*.107,.035,.15),(.032,.025,.033),'Pearl')
        sphere('Eye',(side*.123,.036,.165),(.015,.017,.019),'Ink')
    for k in range(3):cord('Gills',[(.127,.015,.04-k*.035),(.129,-.025,.01-k*.035)],.004,'Cream')
    save('Fish'+str(kind))

cord('Ripple',[(.45*math.cos(j*math.tau/80),0,.45*math.sin(j*math.tau/80)) for j in range(80)],.014,'Mint',True);save('Ripple')

# A small plant is reused at the room edges and garden banks.
box('Pot',(0,.14,0),(.33,.28,.32),'Coral',.065)
for i in range(7):
    a=i*math.tau/7;tip=(.21*math.cos(a),.54+(i%3)*.07,.21*math.sin(a))
    cord('Stem',[(0,.22,0),tip],.007,'Sage');leaf=sphere('Leaf',tip,(.085,.15,.035),'Mint' if i%2 else 'Sage');leaf.rotation_euler[2]=-.3*math.cos(a)
save('Plant')

for o in scene.objects:o.hide_set(False)
for i,o in enumerate(scene.objects):o.location.x+=(i%4)*1.7;o.location.y+=(i//4)*1.5
bpy.ops.wm.save_as_mainfile(filepath=SRC+'/CozyGames_Props.blend',copy=True)
with open(SRC+'/metrics.json','w') as f:json.dump(metrics,f,indent=2)
print(json.dumps(metrics))
