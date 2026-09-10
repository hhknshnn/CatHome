"""Canonical PawCoin, sculpted relief and a transparent HD icon. Headless Blender."""
import bpy,sys,os,math
from mathutils import Vector, Matrix
root=os.path.abspath(os.path.join(os.path.dirname(__file__),'../../..'))
sys.path.insert(0,os.path.join(root,'ArtSource/Blender/PremiumFurniture'))
import premium_kit as k
k.UNITY_MODELS=os.path.join(root,'Assets/Art/PremiumCurrency')
k.clear_scene()
parts=[]
def material(name,color,metal,rough):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*[k.srgb_to_linear(c) for c in color],1)
    p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
    return m
gold=material('CoinGold',(.89,.66,.27),.74,.27)
edge=material('CoinDarkEdge',(.44,.27,.10),.65,.30)
face=material('CoinCoreInset',(.91,.74,.43),.56,.35)
paw=material('CoinPawRelief',(1,.88,.60),.60,.32)
def disc(name,radius,depth,z,mat):parts.append(k.cylinder(name,(0,0,z),radius,depth,mat,'Z',vertices=96,width=.012))
def ring(name,r,z,width,mat):
    bpy.ops.mesh.primitive_torus_add(major_radius=r,minor_radius=width,major_segments=128,minor_segments=16,location=(0,0,z))
    obj=bpy.context.object;obj.name=name;obj.data.materials.append(mat);k.smooth(obj);parts.append(obj)
disc('ReededEdge',.50,.105,0,edge)
disc('GoldBezel',.489,.110,.005,gold)
disc('SatinInset',.423,.075,.038,face)
ring('RaisedOuterRim',.475,.059,.018,paw);ring('InnerMachinedRing',.420,.077,.007,edge)
for i in range(64):
    a=i*math.tau/64
    parts.append(k.cube('EdgeReeding',(.496*math.cos(a),.496*math.sin(a),0),(.018,.032,.070),gold,.004,rotation=(0,0,a)))
# Pad contour is a rounded heart-shaped cushion, not a printed orange blob.
outline=[(-.20,-.09),(-.19,-.20),(-.10,-.245),(0,-.21),(.10,-.245),(.19,-.20),(.20,-.09),(.145,-.025),(.07,-.02),(0,-.065),(-.07,-.02),(-.145,-.025)]
control=[Vector(p) for p in outline];outline=[]
for i in range(len(control)):
    a,b,c,d=[control[j%len(control)] for j in [i-1,i,i+1,i+2]]
    for step in range(8):
        t=step/8
        p=.5*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t)
        outline.append(tuple(p))
verts=[];faces=[]
for z,scale in [(.073,.94),(.11,1),(.15,.84),(.16,.68)]:
    for x,y in outline:verts.append((x*scale,y*scale,z))
n=len(outline)
for j in range(3):
    for i in range(n):a=j*n+i;b=j*n+(i+1)%n;faces.append((a,b,b+n,a+n))
faces.extend([tuple(range(n-1,-1,-1)),tuple(range(3*n,4*n))])
mesh=bpy.data.meshes.new('PawPad');mesh.from_pydata(verts,[],faces);mesh.update();obj=bpy.data.objects.new('PawPadRelief',mesh);bpy.context.collection.objects.link(obj);obj.data.materials.append(paw)
bev=obj.modifiers.new('Soft relief','BEVEL');bev.width=.025;bev.segments=3;k.smooth(obj);parts.append(obj)
for x,y,sx,sy,angle in [(-.235,.072,.068,.087,-.28),(-.092,.198,.071,.10,-.12),(.092,.198,.071,.10,.12),(.235,.072,.068,.087,.28)]:
    parts.append(k.sphere('RaisedToe',(x,y,.107),(sx,sy,.048),paw,32,16,rotation=(0,0,angle)))
for side in [-1,1]:
    for i in range(5):
        a=(-1.06+i*.15) if side>0 else (math.pi+1.06-i*.15)
        parts.append(k.sphere('LaurelGrain',(.373*math.cos(a),.373*math.sin(a),.080),(.018,.028,.011),gold,12,8,rotation=(0,0,-a)))
coin=k.join_fixture('PawCoin',parts);k.export_fixture(coin,'PawCoin.fbx')
# The same mesh, photographed with a softbox rig. No stock/generative substitute.
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=80
scene.render.resolution_x=1536;scene.render.resolution_y=1536;scene.render.resolution_percentage=100
scene.render.film_transparent=True;scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA'
scene.world.color=(.35,.35,.35)
def aim(obj,point):obj.rotation_euler=(Vector(point)-obj.location).to_track_quat('-Z','Y').to_euler()
camdata=bpy.data.cameras.new('Coin portrait');camera=bpy.data.objects.new('Coin portrait',camdata);bpy.context.collection.objects.link(camera)
camera.location=(1.1,.6,5)
back=camera.location.normalized();right=Vector((0,1,0)).cross(back).normalized();up=back.cross(right)
camera.rotation_euler=Matrix((right,up,back)).transposed().to_euler()
camdata.type='ORTHO';camdata.ortho_scale=1.22;scene.camera=camera
for name,position,power,size,color in [('Key',(-2.5,3,4),450,3.8,(1,.92,.76)),('Fill',(3,1,3),300,2.6,(.78,.90,1)),('Rim',(-1,-2,1),220,2,(1,1,1))]:
    data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;data.color=color
    light=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(light);light.location=position;aim(light,(0,0,0))
scene.view_settings.view_transform='AgX'
scene.render.filepath=os.path.join(root,'Assets/Art/PremiumCurrency/Icons/PawCoin_Icon.png');bpy.ops.render.render(write_still=True)
print('PAW_COIN_COMPLETE',scene.render.filepath)
