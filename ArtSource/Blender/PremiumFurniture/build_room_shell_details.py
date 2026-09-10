"""Small architectural assets for the shared room finish. Headless Blender only.

These are fixed architecture, not store products. All existing product footprints,
colliders, placements and cat contact surfaces remain owned by the layout catalog.
Front is -Z; Unity import mirrors X. Leaves are centered for measured fitting.
"""
import os, sys, math, json
import bpy
sys.path.insert(0, os.path.dirname(__file__))
import premium_kit as k

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
k.UNITY_MODELS = os.path.join(ROOT, 'Assets', 'Art', 'RoomShellPolish', 'Models')
os.makedirs(k.UNITY_MODELS, exist_ok=True)
metrics = {}

def export(name, parts):
    obj = k.join_fixture(name, parts)
    k.export_fixture(obj, name + '.fbx')
    metrics[name] = {'size':list(obj.dimensions), 'polygons':len(obj.data.polygons)}

k.clear_scene()
m = k.palette(['CH_White','CH_Gold','CH_MintBright','CH_TealLight','CH_Cream'])
p=[]
# Restrained ivory door with two recessed mint panels and real stepped molding.
p.append(k.cube('Door', (0,1.0,0), (1.42,2.0,.10), m['CH_White'], .028))
for y,h in [(.49,.66),(1.38,.84)]:
    p.append(k.cube('Inset', (0,y,-.064), (1.12,h,.028), m['CH_MintBright'], .024))
    for x in [-.575,.575]:
        p.append(k.cube('Stile',(x,y,-.084),(.035,h+.04,.025),m['CH_White'],.007))
    for yy in [y-h/2-.015,y+h/2+.015]:
        p.append(k.cube('Rail',(0,yy,-.084),(1.18,.035,.025),m['CH_White'],.007))
for x in [-.765,.765]:
    p.append(k.cube('Architrave',(x,1.02,0),(.15,2.04,.20),m['CH_White'],.024))
    p.append(k.cube('FineBrass',(x-.046*(1 if x>0 else -1),1.02,-.107),(.014,1.98,.009),m['CH_Gold'],.003))
p.append(k.cube('Lintel',(0,2.04,0),(1.68,.12,.22),m['CH_White'],.028))
p.append(k.cylinder('HandleRose',(.50,.92,-.089),.053,.035,m['CH_Gold'],axis='Z',vertices=24))
p.append(k.cube('Lever',(.455,.92,-.128),(.17,.03,.035),m['CH_Gold'],.014))
export('RoomDoor_Premium',p)

k.clear_scene();m=k.palette(['CH_White','CH_Gold','CH_MintBright'])
p=[k.cube('Recess',(0,0,.012),(.94,.66,.026),m['CH_MintBright'],.014)]
for x in [-.49,.49]:p.append(k.cube('Side',(x,0,-.012),(.042,.74,.043),m['CH_White'],.009))
for y in [-.35,.35]:p.append(k.cube('Rail',(0,y,-.012),(.98,.042,.043),m['CH_White'],.009))
for x in [-.456,.456]:p.append(k.cube('BrassSide',(x,0,-.025),(.009,.644,.010),m['CH_Gold'],.003))
for y in [-.318,.318]:p.append(k.cube('BrassRail',(0,y,-.025),(.918,.009,.010),m['CH_Gold'],.003))
export('RoomWallPanel_Premium',p)

k.clear_scene();m=k.palette(['CH_MintBright','CH_TealLight'])
p=[]
# Overlapping almond leaves, with a real central crease; no sphere-stack foliage.
p.append(k.sphere('DenseFoliageCore',(0,0,0),(.38,.43,.38),m['CH_TealLight'],segments=16,rings=10))
for i in range(64):
    a=i*2.399963; y=-.37+.74*i/63; r=.36*math.sqrt(max(.1,1-(y/.46)**2))
    cx=math.cos(a)*r;cz=math.sin(a)*r
    length=.13+.016*(i%3); width=.06+.008*(i%2)
    # Tangential leaves lie against the crown, instead of sticking out like thorns.
    forward=(-math.sin(a),.10,math.cos(a)); side=(math.cos(a),0,math.sin(a))
    vertices=[]
    for t,w,zoff in [(0,0,0),(.38,1,0),(.48,0,.035),(.38,-1,0),(1,0,0)]:
        vertices.append((cx+forward[0]*length*t+side[0]*width*w,y+forward[1]*length*t+zoff,cz+forward[2]*length*t+side[2]*width*w))
    mesh=bpy.data.meshes.new('AlmondLeaf');mesh.from_pydata(vertices,[],[(0,1,2),(1,4,2),(4,3,2),(3,0,2)]);mesh.update()
    obj=bpy.data.objects.new('Leaf',mesh);bpy.context.collection.objects.link(obj);obj.data.materials.append(m['CH_MintBright' if i%3 else 'CH_TealLight']);k.solidify(obj,.006);p.append(obj)
obj=k.join_fixture('RoomFoliage_Premium',p)
# Normalize to a centered 1m cube; later fit exactly to the old architecture bounds.
mins=[min(v.co[j] for v in obj.data.vertices) for j in range(3)];maxs=[max(v.co[j] for v in obj.data.vertices) for j in range(3)]
for v in obj.data.vertices:
    for j in range(3):v.co[j]=(v.co[j]-(mins[j]+maxs[j])/2)/(maxs[j]-mins[j])
obj.data.update();k.export_fixture(obj,'RoomFoliage_Premium.fbx')
metrics['RoomFoliage_Premium']={'size':[1,1,1],'polygons':len(obj.data.polygons)}
with open(os.path.join(os.path.dirname(__file__),'RoomShellDetails_Metrics.json'),'w') as f:json.dump(metrics,f,indent=2)
print(json.dumps(metrics))
