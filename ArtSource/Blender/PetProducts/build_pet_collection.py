"""Seventeen authored Cat Home enrichment products. Y-up, front at -Z.
Every moving part exports as a separate single-object FBX on a shared origin.
Usage: blender --background --factory-startup --python build_pet_collection.py -- [Name] [--preview]
"""
import bpy, math, sys, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'ArtSource/Blender/PremiumFurniture'))
import premium_kit as k
k.UNITY_MODELS=str(ROOT/'Assets/Art/PremiumPet/Models')
k.PREVIEW_FOLDER=str(Path(__file__).with_name('Previews'))
import premium_preview as preview

SIZES={'BallBasket':(.78,.52,.62),'ScratchPost':(.72,.90,.72),
'CozyPodBed':(1.2,.6,.95),'ToyMouse':(.44,.34,.78),'PlayTunnel':(.58,.60,.76),
'CeramicBowl':(.50,.16,.42),'CloudBed':(1.15,.48,.85),'CanopyBed':(1.2,.75,1),
'TreatJar':(.38,.48,.38),'FeatherToy':(.42,.65,.42),'ClassicCollar':(.48,.16,.44),
'WalkingLeash':(.8,.15,.62),'BellCollar':(.48,.16,.44),'KibbleBag':(.50,.44,.44),
'NapPillow':(.72,.18,.58),'CatnipPlanter':(.52,.44,.52),'CardboardHideout':(.78,.48,.78)}
P=[];M=[];C={}
def cube(n,xyz,size,c='CH_White',r=.025):P.append(k.cube(n,xyz,size,C[c],width=r,segments=3));return P[-1]
def sphere(n,xyz,size,c='CH_CoralBright',moving=False):
    o=k.sphere(n,xyz,size,C[c],segments=24,rings=12);(M if moving else P).append(o);return o
def ring(n,xyz,rad,tube,c='CH_Gold',scale=(1,1,1),rot=(math.pi/2,0,0),moving=False):
    o=k.torus(n,xyz,rad,tube,C[c],scale=scale,rotation=rot,major_segments=40,minor_segments=8);(M if moving else P).append(o);return o
def rod(n,a,b,r=.018,c='CH_Gold'):P.append(k.strut(n,a,b,r,C[c],vertices=14));return P[-1]
def pipe(n,points,r=.018,c='CH_Gold',moving=False):k.tube(M if moving else P,n,points,r,C[c],vertices=12)
def badge(x,y,z,s=.6):k.paw_badge(P,(x,y,z),(0,0,-1),C['CH_Gold'],s)
def cushion(w,d,y=.085,c='CH_MintBright',h=.13):
    cube('PipedCushionWelt',(0,y,0),(w,h*.25,d),'CH_Gold',.016)
    cube('SoftCushion',(0,y+.009,0),(w-.028,h+.012,d-.028),c,.06)
    for x in [-.20,.20]:sphere('Tuft',(x*w,y+h*.5+.015,0),(.018,.005,.018),'CH_White')
def bed(w,d,cloud=False):
    cube('LowOpenPlatform',(0,.05,0),(w,.10,d),'CH_White',.065)
    cushion(w-.13,d-.14,.12,h=.10)
    for x in [-1,1]:cube('SideBolster',(x*(w*.5-.085),.225,.035),(.15,.24,d-.17),'CH_White',.07)
    cube('BackBolster',(0,.23,d*.5-.10),(w-.14,.27,.15),'CH_CoralBright',.065)
    badge(0,.057,-d*.5-.006,.55)
    if cloud:
        for i in range(5):sphere('CloudPetal',((i-2)*w*.16,.28+.065*math.sin(i*1.4),d*.5-.08),(w*.14,.105,.08),'CH_White')
def bowl(w,d,h,c='CH_AquaBright',food=True):
    r=min(w,d)*.43;sx=w/d
    P.append(k.revolve('CeramicBowl',[(r*.75,.022),(r,.06),(r*.98,h*.83),(r*.91,h)],C[c],segments=40,thickness=.026,close_bottom=True));P[-1].scale.x=sx;k.apply_all(P[-1])
    ring('PolishedLip',(0,h,0),r*.91,.014,'CH_Gold',(sx,1,1))
    P.append(k.cylinder('BowlInterior',(0,h*.44,0),r*.78,.035,C['CH_Cream'],scale=(sx,1,1),vertices=36))
    if food:
        for i in range(13):
            a=i*2.399;rr=r*.54*math.sqrt((i+1)/13);sphere('Kibble',(math.cos(a)*rr*sx,h*.54,math.sin(a)*rr),(.022,.018,.025),'CH_OrangeLight')
    badge(0,h*.5,-r-.004,.4)
def ball(x,y,z,r,c,moving=False):
    sphere('SoftPlayBall',(x,y,z),(r,r,r),c,moving)
    for rotation in [(0,0,0),(math.pi/2,0,0),(0,math.pi/2,0)]:ring('StitchedBallSeam',(x,y,z),r,.004,'CH_White',rot=rotation,moving=moving)
def build(name):
    global P,M,C
    k.clear_scene();P=[];M=[];C=k.palette(list(k.CH_COLORS)+['CH_OrangeLight'] if 'CH_OrangeLight' in k.CH_COLORS else list(k.CH_COLORS))
    # Food stays in the canonical warm palette.
    C['CH_OrangeLight']=C['CH_Cream']
    w,h,d=SIZES[name]
    if name in ['CozyPodBed','CloudBed','CanopyBed']:
        bed(w*.96,d*.96,name=='CloudBed')
        if name=='CozyPodBed':
            for z in [.15,.32]:pipe('PodArch',[(math.cos(a)*.48,.18+math.sin(a)*.38,z) for a in [i*math.pi/24 for i in range(25)]],.042,'CH_White')
            P.append(k.sheet('SoftPodRoof',20,6,lambda u,v:(math.cos(u*math.pi)*.48,.18+math.sin(u*math.pi)*.38,.14+v*.20),[C['CH_MintBright']],lambda c:0,thickness=.025))
        if name=='CanopyBed':
            for x in [-.52,.52]:
                for z in [-.35,.35]:rod('CanopyPost',(x,.10,z),(x,.68,z),.022)
            P.append(k.sheet('ScallopedCanopy',24,12,lambda u,v:((u-.5)*1.11,.69-.055*math.sin(v*math.pi), (v-.5)*.83),[C['CH_White'],C['CH_CoralBright']],lambda c:(c//3)%2,thickness=.028))
            for x in [-.53,.53]:cube('TiedCurtain',(x,.38,.35),(.055,.45,.10),'CH_CoralBright',.022)
    elif name=='NapPillow':cushion(w*.95,d*.95,.075,h=.13);badge(0,.06,-d*.47,.44)
    elif name=='CeramicBowl':bowl(w*.9,d*.9,.155)
    elif name in ['TreatJar','KibbleBag']:
        s=.56 if name=='TreatJar' else 1
        cube('NonSlipTray',(0,.022,0),(w*.95,.044,d*.96),'CH_White',.035)
        cube('FeederBody',(0,.21*s+.05,.08*s),(w*.74,.34*s,d*.58),'CH_MintBright',.055)
        cube('FoodWindow',(0,.20*s+.05,-.09*s),(w*.43,.21*s,.018),'CH_Teal',.025)
        for i in range(6):sphere('VisibleBiscuits',(((i%3)-1)*.055*s,.12*s+(i//3)*.08*s,-.105*s),(.032*s,.026*s,.022*s),'CH_Cream')
        sphere('NoseReleaseButton',(0,.08,-d*.37),(.068,.035,.036),'CH_CoralBright',True)
        lid=cube('FeederLid',(0,.39*s+.05,.08*s),(w*.78,.06,d*.62),'CH_White',.028);P.remove(lid);M.append(lid)
        badge(0,.30*s+.05,-d*.23,.38)
    elif name=='FeatherToy':
        P.append(k.cylinder('WeightedPawBase',(0,.025,0),.19,.05,C['CH_White'],vertices=36))
        ring('BaseRim',(0,.05,0),.173,.014)
        points=[(.025*math.cos(i*math.pi*.55),.055+i*.011,.025*math.sin(i*math.pi*.55)) for i in range(33)]
        pipe('RealSpring',points,.008,'CH_Gold',True)
        for x,c in [(-.06,'CH_CoralBright'),(0,'CH_MintBright'),(.06,'CH_LilacBright')]:
            M.append(k.sphere('SoftFeather',(x,.49,0),(.044,.12,.014),C[c],segments=24,rings=12,rotation=(0,0,-x*5)))
            pipe('FeatherQuill',[(0,.407,0),(x,.43,0),(x,.56,0)],.004,'CH_Gold',True)
        badge(0,.027,-.18,.32)
    elif name in ['ClassicCollar','BellCollar','WalkingLeash']:
        cube('PlayBase',(0,.023,0),(w*.97,.046,d*.97),'CH_White',.065)
        if name=='ClassicCollar':
            for rad in [.13,.19]:ring('BallTrack',(0,.085,0),rad,.018,'CH_MintBright')
            ball(.16,.092,0,.038,'CH_CoralBright',True);badge(0,.03,-d*.47,.35)
        elif name=='BellCollar':
            for x in [-.10,.10]:ring('JingleWheel', (x,.085,0),.065,.012,'CH_AquaBright',rot=(0,math.pi/2,0),moving=True)
            pipe('WheelAxle',[(-.10,.085,0),(.10,.085,0)],.012,'CH_Gold',True)
            for i in range(5):sphere('LooseBells',((i-2)*.029,.085,.012*math.sin(i)),(.020,.025,.020),'CH_Gold',True)
            badge(0,.03,-d*.47,.35)
        else:
            cushion(w*.90,d*.85,.045,'CH_MintBright',h=.055)
            pts=[(-.30+i*.015,.098+.010*math.sin(i*.6),.10*math.sin(i*.35)) for i in range(41)]
            pipe('SatinPlayRibbon',pts,.017,'CH_CoralBright',True)
            ball(.28,.104,pts[-1][2],.035,'CH_LilacBright',True)
            badge(0,.04,-d*.46,.35)
    elif name=='ToyMouse':
        # Low chase dock plus mouse: the moving mouse is a distinct shared-origin FBX.
        cube('MouseDock',(0,.035,.29),(.46,.07,.48),'CH_White',.05)
        cube('MouseDockArch',(0,.19,.44),(.40,.32,.10),'CH_MintBright',.10)
        sphere('MouseBody',(0,.11,-.08),(.105,.092,.16),'CH_LilacBright',True)
        sphere('MouseNose',(0,.09,-.23),(.036,.03,.039),'CH_CoralBright',True)
        for x in [-.074,.074]:
            sphere('VelvetEar',(x,.19,-.13),(.049,.060,.016),'CH_CoralBright',True)
            sphere('Eye',(x*.7,.142,-.199),(.010,.010,.008),'CH_Ink',True)
        pipe('FlexibleMouseTail',[(0,.08,.07),(.09,.06,.17),(.12,.055,.24)],.01,'CH_Gold',True)
        badge(0,.21,.38,.45)
    elif name=='PlayTunnel':
        # A genuinely open arch; no closed cube across the passage.
        for z in [-.34,-.17,0,.17,.34]:pipe('TunnelRib',[(math.cos(a)*.27,.015+math.sin(a)*.555,z) for a in [i*math.pi/28 for i in range(29)]],.015,'CH_Gold')
        P.append(k.sheet('StripedTunnelCloth',30,18,lambda u,v:(math.cos(u*math.pi)*.265,.018+math.sin(u*math.pi)*.55,(v-.5)*.66),[C['CH_White'],C['CH_MintBright']],lambda c:(c//5)%2,thickness=.012))
        pipe('HangingToyCord',[(0,.35,-.345),(0,.23,-.345)],.005,'CH_Gold',True)
        ball(0,.20,-.345,.028,'CH_CoralBright',True)
    elif name=='CardboardHideout':
        cube('CorrugatedFloor',(0,.025,0),(.74,.05,.73),'CH_Cream',.018)
        for x in [-.34,.34]:cube('BoxSide',(x,.235,0),(.055,.43,.72),'CH_Cream',.014)
        cube('BoxBack',(0,.235,.33),(.66,.43,.055),'CH_Cream',.014)
        # Split front keeps a 0.50-wide opening down to floor level.
        for x in [-.295,.295]:cube('DoorSide',(x,.22,-.335),(.085,.38,.05),'CH_Cream',.01)
        cube('DoorLintel',(0,.414,-.335),(.67,.05,.055),'CH_Cream',.014)
        for x in [-.35,.35]:cube('FoldedFlap',(x,.45,.15),(.10,.028,.34),'CH_White',.009)
        cushion(.55,.54,.061,'CH_CoralBright',h=.055)
        badge(0,.415,-.365,.36)
        ball(.22,.06,-.36,.025,'CH_MintBright',True)
    elif name=='CatnipPlanter':
        P.append(k.revolve('Planter',[(.25,.03),(.29,.10),(.30,.27)],C['CH_White'],segments=36,thickness=.022,close_bottom=True))
        ring('PotRim',(0,.27,0),.30,.017,'CH_Gold')
        P.append(k.cylinder('Soil',(0,.25,0),.276,.018,C['CH_Cream'],vertices=36))
        for i in range(30):
            a=i*2.399;r=.23*math.sqrt((i+1)/30);x=math.cos(a)*r;z=math.sin(a)*r
            pipe('SoftCatGrass',[(x,.26,z),(x*1.12,.37+(i%3)*.015,z*1.10),(x*1.15+.028*math.sin(i),.45+(i%4)*.025,z*1.18)],.009,'CH_MintBright' if i%2 else 'CH_AquaBright',True)
        badge(0,.14,-.296,.62)
    elif name=='ScratchPost':
        cube('StablePlinth',(0,.047,0),(.65,.094,.65),'CH_White',.08)
        P.append(k.cylinder('SisalCore',(0,.43,.06),.105,.68,C['CH_Cream'],vertices=32))
        for i in range(34):ring('SisalCoil',(0,.115+i*.019,.06),.107,.009,'CH_Cream')
        P.append(k.cylinder('SoftTop',(0,.80,.06),.17,.065,C['CH_MintBright'],vertices=36))
        pipe('SideToyCord',[(.15,.77,.06),(.23,.51,.03)],.006,'CH_Gold',True)
        ball(.23,.45,.03,.047,'CH_CoralBright',True);badge(0,.052,-.325,.65)
    elif name=='BallBasket':
        P.append(k.revolve('WovenBasket',[(.23,.025),(.28,.13),(.29,.31)],C['CH_White'],segments=36,thickness=.025,close_bottom=True))
        for i in range(28):
            a=i*2*math.pi/28;rod('WovenStrand',(math.cos(a)*.233,.04,math.sin(a)*.233),(math.cos(a)*.296,.30,math.sin(a)*.296),.011,'CH_Cream')
        for y,r in [(.1,.27),(.2,.29),(.31,.30)]:ring('BasketBand',(0,y,0),r,.015,'CH_MintBright')
        for x,z,c in [(-.10,.04,'CH_CoralBright'),(.10,.07,'CH_LilacBright'),(0,-.11,'CH_MintBright')]:ball(x,.33,z,.092,c)
        badge(0,.17,-.29,.65);ball(.24,.095,-.16,.085,'CH_CoralBright',True)
    else:raise ValueError(name)
    objects=[k.join_fixture(name+'_Body',P)]
    if M:objects.append(k.join_fixture(name+'_Moving',M))
    points=[o.matrix_world@v.co for o in objects for v in o.data.vertices]
    lo=Vector([min(v[i] for v in points) for i in range(3)]);hi=Vector([max(v[i] for v in points) for i in range(3)])
    factor=min(SIZES[name][i]/(hi[i]-lo[i]) for i in range(3))*.97
    # Never enlarge undersized intentional details; fit only if footprint requires it.
    factor=min(1,factor);offset=Vector(((lo.x+hi.x)/2,lo.y,(lo.z+hi.z)/2))
    for o in objects:
        for v in o.data.vertices:v.co=(v.co-offset)*factor
        o.data.update()
    for i,o in enumerate(objects):k.export_fixture(o,name+('Moving' if i else '')+'_Premium.fbx')
    bpy.ops.wm.save_as_mainfile(filepath=str(Path(__file__).with_name(name+'_Source.blend')))
    metadata={'fit':factor,'offset':list(offset),'meshes':{o.name:{'min':[min(v.co[i] for v in o.data.vertices) for i in range(3)],'max':[max(v.co[i] for v in o.data.vertices) for i in range(3)]} for o in objects}}
    Path(__file__).with_name(name+'_Metrics.json').write_text(json.dumps(metadata,indent=2))
    if '--preview' in sys.argv:
        combined=k.join_fixture(name,objects);preview.render_views(combined,name)
    print('[pet] COMPLETE',name,metadata)

args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
selected=[x for x in args if x in SIZES] or list(SIZES)
for name in selected:build(name)
