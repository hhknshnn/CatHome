"""Cat Home mini-game collection. Final metre dimensions, headless Blender 5.2.
Geometry uses the established Y-up kit; FBX import mirrors X only.
Run with --background --factory-startup --python. No dependencies/downloads.
"""
import os, sys, math, json
import bpy
from mathutils import Matrix, Vector
ROOT=os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0,os.path.join(ROOT,'..','PremiumFurniture'))
import premium_kit as k
k.UNITY_MODELS=os.path.abspath(os.path.join(ROOT,'../../../Assets/Art/MiniGames/Models'))
metrics={}
def begin():
    k.clear_scene()
    global M,P
    M=k.palette(k.CH_COLORS);P=[]
def box(n,p,d,c='CH_White',b=.035):
    o=k.cube(n,p,d,M[c],b,segments=3);P.append(o);return o
def ball(n,p,s,c='CH_CoralBright'):
    o=k.sphere(n,p,s,M[c],24,12);P.append(o);return o
def cyl(n,p,r,h,c='CH_Teal',axis='Y'):
    o=k.cylinder(n,p,r,h,M[c],axis=axis,vertices=24,width=.012);P.append(o);return o
def line(n,pts,r,c):
    # One continuous bevelled curve, rather than hundreds of joined spheres.
    cu=bpy.data.curves.new(n,'CURVE');cu.dimensions='3D';cu.resolution_u=2;cu.bevel_depth=r;cu.bevel_resolution=2
    sp=cu.splines.new('POLY');sp.points.add(len(pts)-1)
    for q,v in zip(sp.points,pts):q.co=(*v,1)
    ob=bpy.data.objects.new(n,cu);bpy.context.collection.objects.link(ob)
    ob.data.materials.append(M[c]);bpy.context.view_layer.objects.active=ob;ob.select_set(True)
    bpy.ops.object.convert(target='MESH');ob.select_set(False);P.append(ob);return ob
def badge(p,s=.35):k.paw_badge(P,p,(0,0,-1),M['CH_Gold'],s)
def seam(n,y,w,d,c='CH_White'):
    line(n,[(math.cos(t*math.pi/24)*w,y,math.sin(t*math.pi/24)*d) for t in range(49)],.008,c)
def save(name):
    ob=k.join_fixture(name,P)
    points=[ob.matrix_world@v.co for v in ob.data.vertices]
    lo=[min(v[i] for v in points) for i in range(3)];hi=[max(v[i] for v in points) for i in range(3)]
    metrics[name]={'min':lo,'max':hi,'size':[hi[i]-lo[i] for i in range(3)],'vertices':len(points),'triangles':sum(len(f.vertices)-2 for f in ob.data.polygons)}
    k.export_fixture(ob,name+'.fbx')

def basket():
    begin();box('WovenBase',(0,.08,0),(.86,.13,.58),'CH_Cream')
    for y in [.14,.22,.30,.38]:seam('WickerCourse',y,.42,.28,'CH_Cream')
    for i in range(24):
        a=i*math.tau/24;line('Rattan',[(.405*math.cos(a),.10,.265*math.sin(a)),(.43*math.cos(a),.39,.29*math.sin(a))],.011,'CH_Gold')
    for x,z,c in [(-.2,0,'CH_CoralBright'),(.17,.05,'CH_MintBright'),(0,-.13,'CH_LilacBright')]:
        ball('Yarn',(x,.38,z),(.19,.19,.19),c)
        for i in range(4):
            a=i*.55;line('WoundYarn',[(x+.191*math.cos(t*math.tau/32)*math.cos(a),.38+.191*math.sin(t*math.tau/32),z+.191*math.cos(t*math.tau/32)*math.sin(a)) for t in range(33)],.004,'CH_White')
    badge((0,.2,-.295));save('RunnerYarnBasket')
def obstacles():
    basket();begin();box('Foot',(0,.055,0),(.62,.11,.54),'CH_White');cyl('Post',(0,.40,0),.105,.6,'CH_Cream')
    line('Sisal',[(.109*math.cos(i*.42),.13+i*.0025,.109*math.sin(i*.42)) for i in range(205)],.007,'CH_Gold')
    box('PlushCap',(0,.76,0),(.43,.10,.38),'CH_MintBright');badge((0,.055,-.28));save('RunnerScratchPost')
    begin();cyl('CeramicFoot',(0,.04,0),.37,.07,'CH_White')
    for i in range(5):seam('BowlWall',.08+i*.022,.34+i*.005,.34+i*.005,'CH_TealLight')
    cyl('FoodWell',(0,.095,0),.29,.08,'CH_Cream')
    for i in range(18):
        a=i*2.4;r=.055+.19*(i%4)/3;ball('Kibble',(r*math.cos(a),.15,r*math.sin(a)),(.026,.018,.025),'CH_Gold')
    badge((0,.10,-.36));save('RunnerFoodBowl')
    begin();cyl('RubberBumper',(0,.09,0),.39,.16,'CH_Ink');cyl('EnamelLid',(0,.17,0),.37,.09,'CH_White');cyl('Lidar',(0,.24,.10),.095,.10,'CH_Teal')
    box('Status',(0,.221,-.16),(.16,.012,.035),'CH_MintBright',.005);seam('LidJoint',.209,.32,.32,'CH_Cream');badge((0,.224,-.045),.23);save('RunnerVacuum')
    begin();box('BedBase',(0,.05,0),(.90,.10,.70),'CH_Cream');box('TuftedCushion',(0,.15,-.02),(.77,.18,.58),'CH_MintBright',.07)
    box('BackBolster',(0,.32,.25),(.84,.24,.15),'CH_White',.07)
    for x in [-.39,.39]:box('SideBolster',(x,.26,-.015),(.13,.27,.53),'CH_White',.06)
    seam('Piping',.19,.37,.26);badge((0,.055,-.36));save('RunnerCatBed')
    begin()
    for x,h,c in [(-.21,.64,'CH_CoralBright'),(.22,.54,'CH_TealLight')]:
        box('TreatPouch',(x,h/2,0),(.38,h,.3),c,.075);box('Seal',(x,h-.03,0),(.35,.045,.30),'CH_Cream',.014)
        box('Label',(x,h*.54,-.155),(.25,.24,.012),'CH_White',.028);badge((x,h*.53,-.17),.5)
    save('RunnerTreats')
    begin();box('CarrierBase',(0,.06,0),(.88,.12,.68),'CH_Teal')
    box('CarrierBody',(0,.36,0),(.86,.56,.65),'CH_MintBright',.10)
    box('DoorRecess',(0,.33,-.333),(.64,.43,.018),'CH_Screen',.08)
    for x in [-.22,-.11,0,.11,.22]:box('DoorBar',(x,.33,-.36),(.016,.35,.025),'CH_White',.007)
    for x in [-.44,.44]:
        for z in [-.17,-.06,.05,.16]:box('Vent',(x,.38,z),(.013,.15,.025),'CH_Teal',.006)
    line('CarryHandle',[(-.17,.64,0),(-.15,.77,0),(.15,.77,0),(.17,.64,0)],.024,'CH_Cream');badge((0,.61,-.31));save('RunnerCarrier')
    begin()
    for i,c in enumerate(['CH_MintBright','CH_CoralBright']):
        box('Pillow',(0,.12+i*.19,0),(.88-i*.16,.22,.62-i*.1),c,.10);seam('PillowPiping',.12+i*.19,.425-i*.08,.295-i*.05)
    badge((0,.11,-.32));save('RunnerPillows')
    for name,c in [('RunnerRibbonGate','CH_TealLight'),('RunnerNapCanopy','CH_LilacBright')]:
        begin()
        for x in [-.51,.51]:
            box('Foot',(x,.045,0),(.15,.09,.56),'CH_Cream');cyl('Post',(x,.46,0),.042,.82,'CH_Cream')
        # Underside at .50m: honest, visible clearance matching the gameplay gate.
        box('PaddedLintel',(0,.575,0),(1.1,.15,.38),c,.055)
        P.append(k.sheet('FabricAwning',12,6,lambda u,v:((u-.5)*1.12,.70+.12*math.sin(v*math.pi), (v-.5)*.56),[M[c],M['CH_White']],lambda col:(col//2)%2,.012))
        badge((0,.60,-.205),.44);save(name)

def mouse():
    begin()
    # Sewn pear silhouette, felt ears, embroidered face, four moving fabric feet.
    rings=[];verts=[];faces=[]
    for j in range(13):
        z=-.20+j*.038;a=math.sin(math.pi*(j+.3)/13.6);r=.14*a*(1-.35*j/12)
        for i in range(24):
            t=i*math.tau/24;verts.append((r*math.cos(t),.12+.085*a*math.sin(t),z))
    for j in range(12):
        for i in range(24):a=j*24+i;b=j*24+(i+1)%24;faces.append((a,b,b+24,a+24))
    me=bpy.data.meshes.new('TailoredFelt');me.from_pydata(verts,[],faces);me.update();ob=bpy.data.objects.new('FeltBody',me);bpy.context.collection.objects.link(ob);me.materials.append(M['CH_LilacBright']);k.smooth(ob);P.append(ob)
    for x in [-.105,.105]:
        ball('FeltEar',(x,.21,.10),(.065,.085,.019),'CH_LilacBright');ball('EarLining',(x,.211,.12),(.043,.061,.01),'CH_Pink')
        ball('StitchedEye',(x*.58,.164,.226),(.014,.018,.01),'CH_Ink');ball('Catchlight',(x*.58-.004,.17,.235),(.004,.005,.003),'CH_White')
    ball('FabricNose',(0,.117,.286),(.024,.018,.024),'CH_CoralBright')
    line('BackSeam',[(0,.21-.02*(i/12),-.17+i*.028) for i in range(13)],.003,'CH_White')
    line('CordTail',[(.075*math.sin(i*.17),.07,-.20-i*.007) for i in range(33)],.009,'CH_CoralBright')
    body=k.join_fixture('FeltMouse',P);parts=[body]
    for side in [-1,1]:
        for z in [-.1,.13]:
            ob=k.sphere('Foot_'+str(side)+'_'+str(z),(side*.095,.027,z),(.048,.025,.056),M['CH_White'],16,8);k.apply_all(ob);parts.append(ob)
    k.export_fixture(parts,'CatchFeltMouse.fbx')
    metrics['CatchFeltMouse']={'size':[.34,.295,.72],'movingFeet':4}

def house(n,variant):
    begin();c=['CH_MintBright','CH_CoralBright','CH_LilacBright'][variant%3];h=2.5+(variant%3)*.45;w=2.15
    box('Plinth',(0,.10,0),(w+.2,.20,1.5),'CH_Cream');box('Facade',(0,h*.5+.15,0),(w,h,1.30),'CH_White',.09)
    box('RoofCornice',(0,h+.18,0),(w+.24,.18,1.53),c,.06)
    for x in [-.91,.91]:box('Pilaster',(x,h*.5+.1,-.70),(.12,h,.10),'CH_Cream')
    box('DoorFrame',(-.44,.73,-.69),(.67,1.34,.10),c,.09);box('GlazedDoor',(-.44,.76,-.754),(.51,1.12,.035),'CH_Screen',.08)
    cyl('BrassHandle',(-.26,.70,-.80),.025,.05,'CH_Gold','Z')
    for x in [-.48,.48]:
        for y in [h-.43]:
            box('WindowFrame',(x,y,-.71),(.74,.69,.10),c,.07);box('WindowGlass',(x,y,-.77),(.61,.56,.026),'CH_Screen',.05)
            box('Mullion',(x,y,-.795),(.035,.56,.028),'CH_Cream');box('Sill',(x,y-.36,-.79),(.84,.08,.29),'CH_Cream')
    box('ShopWindow',(.46,.83,-.71),(.65,.76,.12),'CH_Teal',.075)
    box('WindowInset',(.46,.83,-.785),(.52,.63,.025),'CH_Screen',.055)
    # A shaped striped canopy casts its own shadow across the shopfront.
    P.append(k.sheet('StripedCanopy',14,6,lambda u,v:((u-.5)*2.05,1.64+.20*math.sin(v*math.pi/2),-.72-v*.48),[M[c],M['CH_White']],lambda i:(i//2)%2,.025))
    box('PawSign',(.46,1.20,-.82),(.44,.22,.055),'CH_Cream',.05);badge((.46,1.15,-.86),.43)
    if variant%3==1:
        box('Balcony',(0,h-.80,-.96),(1.95,.09,.65),'CH_Cream');line('Rail',[(-.9,h-.38,-1.21),(.9,h-.38,-1.21)],.018,'CH_Gold')
        for i in range(9):box('Baluster',(-.88+i*.22,h-.59,-1.21),(.023,.39,.025),'CH_Gold',.006)
    if variant%3==2:
        for x in [-.61,.61]:
            P.append(k.cube('RoofEar',(x,h+.50,0),(.55,.52,.45),M[c],.06,rotation=(0,0,-.48 if x<0 else .48)))
    box('Planter',(.66,.25,-.98),(.65,.32,.35),'CH_Cream',.06)
    for i in range(7):ball('ClippedFoliage',(.43+i*.073,.48,-.98),(.13,.16,.13),'CH_TealLight')
    save(n)

def arena():
    begin()
    # Quiet parquet field; rear-only architecture leaves all 8x6m playable floor readable.
    box('Subfloor',(0,-.055,0),(8,.1,6),'CH_Cream')
    for z in range(12):
        for x in range(5):box('OakBoard',(-3.2+x*1.6+(z%2)*.04,-.007,-2.75+z*.5),(1.575,.025,.48),'CH_Cream',.006)
    box('SageWainscot',(0,.54,2.95),(8,1.08,.13),'CH_TealLight')
    box('IvoryWall',(0,1.95,3.02),(8,1.9,.12),'CH_White')
    for x in [-3,-1.5,0,1.5,3]:
        box('PanelInset',(x,.53,2.86),(1.30,.77,.035),'CH_MintBright');
    box('ChairRail',(0,1.12,2.82),(8,.07,.12),'CH_Cream');box('Skirting',(0,.09,2.78),(8,.18,.15),'CH_Cream')
    for x in [-2.45,2.45]:
        box('ArchedWindowFrame',(x,2.01,2.87),(1.34,1.53,.12),'CH_Cream',.21)
        box('WindowSky',(x,2.03,2.79),(1.16,1.33,.08),'CH_TealLight',.20)
        box('WindowMullion',(x,2.01,2.72),(.045,1.3,.04),'CH_White');box('WindowCrossbar',(x,2.01,2.72),(1.12,.045,.04),'CH_White')
        box('WindowSill',(x,1.25,2.72),(1.49,.10,.33),'CH_Cream')
        for side in [-1,1]:
            P.append(k.sheet('LinenCurtain',12,12,lambda u,v,x=x,side=side:(x+side*(.66+.16*u),1.22+1.62*v,2.63+.035*math.sin(u*math.pi*6)),[M['CH_White']],lambda i:0,.014))
        cyl('CurtainRod',(x,2.92,2.65),.026,1.94,'CH_Gold','X')
    box('GameEmblem',(0,2.0,2.87),(1.12,.86,.12),'CH_Cream',.16);box('EmblemFace',(0,2.0,2.79),(.96,.70,.05),'CH_White',.12);badge((0,1.88,2.74),1.35)
    # Low upholstered edge, not camera-blocking side walls.
    for x in [-4.02,4.02]:box('SoftSideBoundary',(x,.24,0),(.16,.48,6),'CH_TealLight',.07)
    for x in [-4.48,4.48]:
        box('PlanterFoot',(x,.10,2.35),(.62,.20,.66),'CH_Cream',.08)
        cyl('PlantPot',(x,.39,2.35),.27,.49,'CH_White')
        for i in range(7):
            a=i*math.tau/7
            line('Stem',[(x,.58,2.35),(x+.26*math.cos(a),1.05,2.35+.26*math.sin(a))],.011,'CH_Teal')
            P.append(k.sphere('Leaf',(x+.25*math.cos(a),1.09,2.35+.25*math.sin(a)),(.15,.27,.045),M['CH_MintBright'],24,12,rotation=(0,-a,0)))
    # Recessed circular play mat, at the same contact plane as parquet.
    cyl('PlayMat',(0,.014,-.08),2.15,.014,'CH_MintBright')
    seam('MatBinding',.023,2.10,2.10,'CH_White')
    # An inviting toy studio: decoration stays behind the arena edge and the
    # printed floor motifs are flush, so every mouse remains readable.
    for i,c in enumerate(['CH_CoralBright','CH_Gold','CH_MintBright','CH_LilacBright']):
        r=.72+i*.085
        line('RainbowMural',[(math.cos(a*math.pi/32)*r,1.87+math.sin(a*math.pi/32)*r,2.72) for a in range(33)],.034,c)
    for side in [-1,1]:
        for j in range(3):
            x=side*(3.28+j*.12)
            ball('MuralStar',(x,1.95+j*.22,2.71),(.045,.07,.014),['CH_CoralBright','CH_Gold','CH_LilacBright'][j])
    line('BuntingCord',[(-3.7,2.82,2.58),(0,2.48,2.58),(3.7,2.82,2.58)],.012,'CH_Gold')
    for i in range(14):
        x=-3.45+i*.53;y=2.49+abs(x)*.092
        mesh=bpy.data.meshes.new('Flag');mesh.from_pydata([(x-.16,y,2.56),(x+.16,y,2.56),(x,y-.29,2.56)],[],[(0,1,2)])
        ob=bpy.data.objects.new('FabricPennant',mesh);bpy.context.collection.objects.link(ob);ob.data.materials.append(M[['CH_CoralBright','CH_MintBright','CH_LilacBright','CH_Gold'][i%4]]);P.append(ob)
    for x in [-3.5,3.5]:
        box('ToyCubby',(x,.40,2.66),(.62,.65,.25),'CH_White',.045)
        box('CubbyRecess',(x,.41,2.515),(.49,.48,.016),'CH_Teal',.015)
        for j in range(3):ball('StoredYarn',(x-.13+j*.13,.31,2.49),(.09,.10,.07),['CH_CoralBright','CH_Gold','CH_LilacBright'][j])
    # A subtle circular stitch, no false gameplay targets on the floor.
    for i in range(48):
        a=i*math.tau/48
        box('MatStitch',(math.cos(a)*2.04,.024,math.sin(a)*2.04-.08),(.016,.003,.026),'CH_White',.001)
    save('CatchPlayroom')

def textures():
    import numpy as np
    folder=os.path.join(k.UNITY_MODELS,'..','Textures');os.makedirs(folder,exist_ok=True)
    size=2048;y,x=np.mgrid[0:size,0:size].astype(np.float32)/size
    rng=np.random.default_rng(19)
    for name in ['OakGrain','LinenWeave']:
        if name=='OakGrain':
            grain=np.sin((x*190+2*np.sin(y*9)+np.sin(y*21))*math.tau)*.026+np.sin(x*78*math.tau)*.012
            v=.91+grain+rng.normal(0,.008,(size,size))
        else:v=.94+(.027*np.sin(x*math.tau*320)+.027*np.sin(y*math.tau*320))+rng.normal(0,.006,(size,size))
        pixels=np.ones((size,size,4),np.float32);pixels[:,:,:3]=np.clip(v,0,1)[:,:,None]
        img=bpy.data.images.new(name,width=size,height=size,alpha=False);img.pixels.foreach_set(pixels.ravel());img.filepath_raw=os.path.join(folder,name+'.png');img.file_format='PNG';img.save()

if __name__=='__main__':
    obstacles();mouse()
    for i in range(3):house('RunnerTownhouse'+str(i),i)
    arena();textures()
    with open(os.path.join(ROOT,'collection_metrics.json'),'w',encoding='utf8') as f:json.dump(metrics,f,indent=2)
    print('MINIGAME_COLLECTION_COMPLETE',len(metrics))
