"""New original Runner props, in metres. One palette material per exported model.
Headless Blender; staged outside Assets while native Unity tests run.
"""
import bpy, math, os, sys, json
from mathutils import Vector
ROOT=os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0,os.path.join(ROOT,'..','PremiumFurniture'))
import premium_kit as k
k.clear_scene()
k.UNITY_MODELS=os.path.join(ROOT,'PlayfulGenerated','Models')
PALETTE={
 'Arcade_Coral':(1,.255,.25),'Arcade_Sky':(.11,.58,.89),'Arcade_Teal':(.025,.49,.47),
 'Arcade_Mint':(.29,.80,.57),'Arcade_Sun':(1,.69,.115),'Arcade_Cream':(1,.925,.76),
 'Arcade_White':(1,.985,.94),'Arcade_Ink':(.04,.16,.225),'Arcade_Lilac':(.49,.32,.77),
 'Arcade_Road':(.36,.67,.71),'Arcade_RoadLight':(.48,.76,.77),'Arcade_Sand':(.94,.70,.41),
 'Arcade_Grass':(.245,.63,.38),'Arcade_Court':(.86,.925,.71)}
k.CH_COLORS.update(PALETTE);M=k.palette(PALETTE);names=list(PALETTE)
atlas=bpy.data.images.load(os.path.abspath(os.path.join(ROOT,'../../../Assets/Art/MiniGames/ArcadeWorlds/Textures/ArcadePalette.png')))
mat=bpy.data.materials.new('PlayfulPalette');mat.use_nodes=True
node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=atlas;node.interpolation='Closest'
shader=mat.node_tree.nodes.get('Principled BSDF');mat.node_tree.links.new(node.outputs['Color'],shader.inputs['Base Color']);shader.inputs['Roughness'].default_value=.34
parts=[];exports=[]
def box(n,p,s,c,b=.025,rot=(0,0,0)):
 o=k.cube(n,p,s,M['Arcade_'+c],b,rot,segments=3);parts.append(o);return o
def ball(n,p,s,c):
 o=k.sphere(n,p,s,M['Arcade_'+c],16,8);parts.append(o);return o
def cyl(n,p,r,h,c,axis='Y'):
 o=k.cylinder(n,p,r,h,M['Arcade_'+c],axis,vertices=20,width=.008);parts.append(o);return o
def ring(n,p,r,t,c,rot=(math.pi/2,0,0)):
 o=k.torus(n,p,r,t,M['Arcade_'+c],rotation=rot,major_segments=24,minor_segments=8);parts.append(o);return o
def rod(n,a,b,r,c):
 o=k.strut(n,a,b,r,M['Arcade_'+c],vertices=12);parts.append(o);return o
def finish(name,floor=True):
 global parts
 obj=k.join_fixture(name,parts)
 if floor:k.drop_to_floor(obj)
 # Converted text can contribute an earlier UVMap. Unity samples channel 0,
 # whereas Blender renders the active layer: retain only the palette channel.
 for old_uv in list(obj.data.uv_layers):obj.data.uv_layers.remove(old_uv)
 uv=obj.data.uv_layers.new(name='PaletteUV');uv.active_render=True
 for polygon in obj.data.polygons:
  idx=names.index(obj.data.materials[polygon.material_index].name)
  for loop in polygon.loop_indices:uv.data[loop].uv=((idx*16+8)/256,.5)
 obj.data.materials.clear();obj.data.materials.append(mat)
 for polygon in obj.data.polygons:polygon.material_index=0
 k.export_fixture(obj,name+'.fbx')
 obj.hide_set(True);exports.append({'name':name,'vertices':len(obj.data.vertices),'polygons':len(obj.data.polygons),'size':list(obj.dimensions)})
 parts=[];return obj

# A satin deck, griptape paw, metal trucks and four contrasting wheels.
box('Deck',(0,.17,0),(.94,.07,.49),'Coral',.05)
box('Grip',(0,.211,0),(.70,.015,.34),'Ink',.03)
for x in [-.32,.32]:
 rod('Truck',(x,.105,-.18),(x,.105,.18),.035,'Cream')
 for z in [-.23,.23]:cyl('Wheel',(x,.10,z),.10,.07,'Sun','Z')
ball('Paw',(0,.225,0),(.065,.012,.055),'White')
for x,z in [(-.065,-.07),(-.025,-.10),(.025,-.10),(.065,-.07)]:ball('Toe',(x,.226,z),(.027,.011,.024),'White')
finish('PlayfulSkateboard')

# Rounded toy locomotive with a brass boiler, tall cabin and visible wheel hubs.
box('TrainBase',(0,.14,0),(.96,.12,.51),'Teal')
cyl('Boiler',(-.14,.32,0),.20,.55,'Sun','X')
box('Cabin',(.30,.36,0),(.29,.44,.43),'Coral')
box('Roof',(.30,.60,0),(.40,.07,.56),'Lilac')
for z in [-.222,.222]:
 box('Window',(.30,.43,z),(.19,.16,.014),'Ink',.02)
 for x in [-.31,.02,.33]:
  cyl('Wheel',(x,.12,z*1.24),.115,.065,'Ink','Z');cyl('Hub',(x,.12,z*1.4),.048,.013,'Cream','Z')
cyl('Chimney',(-.31,.55,0),.055,.16,'Lilac')
finish('PlayfulToyTrain')

# Parcel pile, crossed ribbons, label pockets and a flat carrying handle.
for i,(x,z,w,h,c) in enumerate([(-.17,.02,.63,.25,'Coral'),(.16,-.015,.56,.27,'Sky')]):
 y=.125 if i==0 else .385
 box('Parcel',(x,y,z),(w,h,.59),'Sand')
 box('RibbonFront',(x,y,z-.298),(.06,h,.012),c,.003)
 box('RibbonTop',(x,y+h/2+.004,z),(.06,.012,.58),c,.003)
 box('RibbonCross',(x,y+h/2+.005,z),(w,.012,.06),c,.003)
 box('Label',(x+.13,y,z-.307),(.15,.085,.012),'White',.008)
finish('PlayfulParcelStack')

# Bath duck with cheeks, wing feathers and a coral bill.
ball('Body',(0,.24,.015),(.36,.235,.30),'Sun');ball('Head',(-.08,.47,-.14),(.18,.18,.18),'Sun')
box('Bill',(-.08,.425,-.325),(.23,.065,.16),'Coral',.024)
for x in [-.18,.02]:ball('Eye',(x,.51,-.285),(.022,.026,.012),'Ink')
for x in [-.32,.32]:ball('Wing',(x,.29,.02),(.065,.125,.17),'Cream')
ball('Tail',(0,.30,.29),(.105,.13,.15),'Sun');finish('PlayfulDuck')

# A stack of toy doughnuts with sprinkles; solid collision is their whole silhouette.
for i,c in enumerate(['Coral','Lilac','Mint']):
 ring('Doughnut',(0,.09+i*.14,0),.235,.085,c)
 for j in range(7):
  angle=j*math.tau/7+i*.4
  box('Sprinkle',(.23*math.cos(angle),.16+i*.14,.23*math.sin(angle)),(.055,.015,.017),'Sun' if j%2 else 'White',.005,rot=(0,angle,0))
finish('PlayfulDonutStack')

# Small florist's wagon, slatted crate, leaf pairs and three sculpted daisies.
box('Cart',(0,.24,0),(.84,.25,.51),'Cream')
for x in [-.32,.32]:
 for z in [-.29,.29]:cyl('Wheel',(x,.105,z),.10,.055,'Teal','Z')
for x in [-.31,-.10,.10,.31]:box('CrateSlat',(x,.26,-.267),(.15,.20,.018),'Sand',.005)
for i,x in enumerate([-.25,0,.25]):
 top=.61+(.065 if i==1 else 0)
 rod('Stem',(x,.35,0),(x,top,0),.019,'Grass')
 for side in [-1,1]:ball('Leaf',(x+side*.055,.45,0),(.075,.026,.036),'Mint')
 for j in range(7):
  a=j*math.tau/7
  ball('Petal',(x+.075*math.cos(a),top+.075*math.sin(a),-.025),(.044,.044,.025),'White' if i%2 else 'Coral')
 ball('FlowerHeart',(x,top,-.048),(.044,.044,.025),'Sun')
finish('PlayfulFlowerCart')

# Bevelled five-point score star, embossed 2x face. The text is mesh, never a runtime font.
verts=[]
for z in [-.08,.08]:
 for i in range(10):
  a=math.pi/2+i*math.pi/5;r=.36 if i%2==0 else .18
  verts.append((math.cos(a)*r,math.sin(a)*r,z))
faces=[tuple(range(9,-1,-1)),tuple(range(10,20))]+[(i,(i+1)%10,(i+1)%10+10,i+10) for i in range(10)]
mesh=bpy.data.meshes.new('ScoreStar');mesh.from_pydata(verts,[],faces);mesh.update()
obj=bpy.data.objects.new('ScoreStar',mesh);bpy.context.collection.objects.link(obj);k.finish(obj,M['Arcade_Sun'],.023,3);parts.append(obj)
curve=bpy.data.curves.new('Multiplier','FONT');curve.body='2x';curve.align_x='CENTER';curve.align_y='CENTER';curve.size=.245;curve.extrude=.006;curve.bevel_depth=.002
label=bpy.data.objects.new('Multiplier',curve);bpy.context.collection.objects.link(label);label.location=(0,-.012,-.095);label.rotation_euler=(0,math.pi,0);label.data.materials.append(M['Arcade_Ink'])
bpy.ops.object.select_all(action='DESELECT');label.select_set(True);bpy.context.view_layer.objects.active=label;bpy.ops.object.convert(target='MESH');parts.append(bpy.context.object)
finish('BonusScoreStar',False)

# Mystery gift with a looping gold bow and contrasting lid.
box('Gift',(0,0,0),(.44,.42,.40),'Lilac',.045);box('Lid',(0,.235,0),(.50,.085,.46),'Coral',.024)
box('Ribbon',(0,0,-.207),(.072,.42,.016),'Sun',.004);box('RibbonTop',(0,.282,0),(.072,.012,.46),'Sun',.004)
for side in [-1,1]:ring('Bow',(side*.085,.33,0),.085,.023,'Sun',rot=(0,0,side*.4))
finish('BonusGift',False)

os.makedirs(os.path.join(ROOT,'PlayfulGenerated'),exist_ok=True)
with open(os.path.join(ROOT,'PlayfulGenerated','metrics.json'),'w') as f:json.dump(exports,f,indent=2)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT,'PlayfulBonuses_Source.blend'))
print('PLAYFUL_EXPORTS',json.dumps(exports))
