"""Approved living-room media/care collection. Headless Blender, Y-up.
Media fronts are +Z to retain the left-wall catalog mapping. Care fronts -Z.
"""
import bpy, math, sys, json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(Path(__file__).parent))
import premium_kit as k
k.UNITY_MODELS=str(ROOT/'Assets/Art/PremiumFurniture/Models')
P=[];C={}
def box(n,p,s,c='CH_White',r=.025):
    o=k.cube(n,p,s,C[c],width=r,segments=4);P.append(o);return o
def orb(n,p,s,c='CH_CoralBright'):
    o=k.sphere(n,p,s,C[c],segments=32,rings=16);P.append(o);return o
def cyl(n,p,r,h,c='CH_Gold',axis='Y'):
    o=k.cylinder(n,p,r,h,C[c],axis=axis,vertices=40);P.append(o);return o
def badge(p,normal=(0,0,1),s=.35):k.paw_badge(P,p,normal,C['CH_Gold'],s)
def save(name):
    contents=[o for o in P if name in ['MainFoodBowl','MainWaterBowl'] and (o.name.startswith('Kibble') or o.name=='Water')]
    if contents:
        for o in contents:P.remove(o)
        content=k.join_fixture(name+'Content',contents);k.export_fixture(content,name+'Content_Premium.fbx')
    obj=k.join_fixture(name,P);k.export_fixture(obj,name+'_Premium.fbx')
    bpy.ops.wm.save_as_mainfile(filepath=str(Path(__file__).with_name(name+'_Source.blend')))
    verts=[v.co for v in obj.data.vertices]
    Path(__file__).with_name(name+'_Metrics.json').write_text(json.dumps({'min':[min(v[i] for v in verts) for i in range(3)],'max':[max(v[i] for v in verts) for i in range(3)]},indent=2))
def build(name):
    global P,C
    k.clear_scene();P=[];C=k.palette(k.CH_COLORS)
    if name=='TvUnit':
        # Open centre bay; opaque side cupboards carry the small speakers.
        for x in [-.91,.91]:
            for z in [-.17,.17]:cyl('BrassFoot',(x,.055,z),.027,.11)
        box('LowerShelf',(0,.135,0),(2.17,.06,.51))
        box('Top',(0,.49,0),(2.20,.065,.54),r=.035)
        box('Back',(0,.31,-.235),(2.12,.31,.035),'CH_Cream',.012)
        for x in [-.81,.81]:
            box('SideCabinet',(x,.31,0),(.53,.30,.48),r=.032)
            box('Door',(x,.31,.255),(.47,.255,.028),'CH_Cream',.026)
            cyl('Handle',(x,.35,.278),.016,.016,axis='Z')
        badge((.79,.245,.279),s=.26)
    elif name=='GameConsoleSet':
        # This low base also supports the console before the cabinet is bought.
        box('LowStand',(0,.08,0),(.46,.16,.32),'CH_White',.024)
        box('ConsoleBody',(0,.205,0),(.42,.075,.28),'CH_Teal',.027)
        box('PearlTop',(0,.238,0),(.43,.024,.29),r=.02)
        box('DiscSlot',(-.055,.21,.143),(.17,.009,.007),'CH_Ink',.003)
        orb('StatusLight',(.146,.213,.145),(.008,.008,.006),'CH_AquaBright')
        box('Controller',(0,.265,.054),(.18,.055,.095),'CH_CoralBright',.028)
        for x in [-.045,.045]:cyl('ThumbStick',(x,.297,.054),.012,.012,'CH_Teal')
    elif name=='SpeakerSystem':
        # A matched pair. Slender stands remain usable without a TV cabinet;
        # cabinet side cupboards conceal the stands when the media set is full.
        for x in [-.95,.95]:
            box('StandBase',(x,.025,0),(.24,.05,.25),r=.025)
            cyl('Stand',(x,.255,0),.018,.46)
            box('SpeakerBody',(x,.675,0),(.23,.31,.22),'CH_White',.035)
            box('FabricFront',(x,.675,.114),(.19,.265,.017),'CH_Teal',.022)
            cyl('Woofer',(x,.634,.129),.058,.012,'CH_Ink',axis='Z')
            cyl('Driver',(x,.634,.138),.037,.009,'CH_Gold',axis='Z')
            cyl('Tweeter',(x,.735,.129),.025,.012,'CH_Cream',axis='Z')
    elif name=='MainCatBed':
        from build_main_cat_bed import build as build_bed
        build_bed();return
    elif name in ['MainFoodBowl','MainWaterBowl']:
        P.append(k.revolve('CeramicShell',[(.096,.019),(.131,.043),(.131,.091),(.122,.106)],C['CH_White'],segments=56,thickness=.012,close_bottom=True))
        cyl('InnerBowl',(0,.042,0),.113,.014,'CH_MintBright')
        if name=='MainFoodBowl':
            for i in range(25):
                a=i*2.399;r=.086*math.sqrt((i+1)/25)
                orb('Kibble',(math.cos(a)*r,.066,math.sin(a)*r),(.014,.009,.017),'CH_Cream')
        else:cyl('Water',(0,.073,0),.116,.008,'CH_AquaBright')
        badge((0,.057,-.132),(0,0,-1),.16)
    save(name)
targets=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
for n in targets or ['TvUnit','GameConsoleSet','SpeakerSystem','MainCatBed','MainFoodBowl','MainWaterBowl']:build(n)
