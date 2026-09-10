"""Second Runner art pass: a cohesive, cat-scale pedestrian town. Headless only."""
import os,sys,math,json
sys.path.insert(0,os.path.dirname(__file__))
import build_minigame_collection as g
from build_minigame_collection import box,ball,cyl,line,badge,seam
k=g.k
for name,color in {'CH_Paving':(.72,.62,.48),'CH_Roof':(.24,.40,.39),'CH_Stucco':(.91,.85,.73),'CH_Leaf':(.28,.49,.34),'CH_Terracotta':(.63,.31,.22),'CH_Porcelain':(.95,.93,.87)}.items():k.CH_COLORS[name]=color

def plant(x,y,z,scale=1):
    cyl('TerracottaPot',(x,y+.16*scale,z),.17*scale,.30*scale,'CH_Terracotta')
    for i in range(6):
        a=i*math.tau/6
        line('PlantStem',[(x,y+.25*scale,z),(x+.15*scale*math.cos(a),y+.63*scale,z+.15*scale*math.sin(a))],.009*scale,'CH_Leaf')
        g.P.append(k.sphere('Leaf',(x+.13*scale*math.cos(a),y+.61*scale,z+.13*scale*math.sin(a)),(.09*scale,.20*scale,.03*scale),g.M['CH_Leaf'],16,8,rotation=(0,-a,.2)))

def window(x,y,z,w=.74,h=.83,accent='CH_TealLight'):
    box('WindowArch',(x,y,z),(w,h,.09),'CH_Cream',.13)
    box('RecessedGlazing',(x,y,z-.053),(w-.12,h-.13,.025),'CH_Screen',.10)
    box('CenterMullion',(x,y,z-.077),(.032,h-.15,.025),'CH_White',.01)
    box('CrossMullion',(x,y-.10,z-.08),(w-.13,.029,.025),'CH_White',.009)
    for side in [-1,1]:
        box('PaintedShutter',(x+side*(w*.5+.09),y,z+.03),(.15,h*.89,.06),accent,.018)
        for j in range(6):box('ShutterSlat',(x+side*(w*.5+.09),y-h*.34+j*h*.135,z-.009),(.12,.025,.012),'CH_Cream',.006)
    box('StoneSill',(x,y-h*.5,z-.06),(w+.24,.07,.25),'CH_Porcelain',.025)

def facade(index):
    g.begin();w=2.65;h=2.75+index*.32;c=['CH_TealLight','CH_CoralBright','CH_LilacBright'][index]
    box('StoneFoundation',(0,.12,0),(w+.12,.24,1.7),'CH_Paving',.06)
    box('PlasterBody',(0,h*.5+.17,0),(w,h,1.55),'CH_Stucco',.075)
    for side in [-1,1]:
        for j in range(8):box('CornerQuoin',(side*1.28,.38+j*.32,-.81),(.17,.17,.12),'CH_Porcelain',.018)
    box('StorefrontRecess',(0,.86,-.795),(2.18,1.25,.06),c,.11)
    box('ShopDoor',(-.64,.83,-.843),(.61,1.17,.05),'CH_Screen',.09)
    box('DoorLowerPanel',(-.64,.47,-.88),(.44,.34,.026),c,.04)
    cyl('Handle',(-.41,.83,-.88),.025,.065,'CH_Gold','Z')
    box('DisplayWindow',(.40,.90,-.84),(1.12,.91,.045),'CH_Screen',.12)
    for x in [.04,.40,.76]:box('WindowDivider',(x,.90,-.875),(.024,.87,.021),'CH_Cream',.008)
    box('ShopFrieze',(0,1.67,-.865),(2.22,.30,.09),'CH_Porcelain',.055)
    badge((0,1.60,-.925),.72)
    for x in [-.40,.40]:box('SignRule',(x,1.70,-.922),(.25,.022,.012),'CH_Gold',.005)
    g.P.append(k.sheet('StripedShopAwning',18,10,lambda u,v:((u-.5)*2.37,1.64+.16*math.cos(v*math.pi*.5),-.90-v*.52),[g.M[c],g.M['CH_Porcelain']],lambda i:(i//3)%2,.018))
    for i in range(9):ball('ScallopedHem',(-1.05+i*.263,1.64,-1.415),(.135,.045,.015),c if i%2==0 else 'CH_Porcelain')
    for x in [-.65,.65]:window(x,h-.43,-.81,.77,.91,c)
    box('UpperCornice',(0,h+.18,0),(w+.20,.14,1.81),'CH_Porcelain',.05)
    # A proper pitched tiled roof, not two free-floating ear blocks.
    for side in [-1,1]:
        for row in range(5):
            z=side*(.12+row*.19);y=h+.70-row*.104
            for col in range(12):
                x=-1.34+col*.244
                g.P.append(k.cube('RoofTile',(x,y,z),(.251,.055,.245),g.M['CH_Roof'],.018,rotation=(side*.49,0,0)))
    line('RoofRidge',[(-1.47,h+.74,0),(1.47,h+.74,0)],.055,'CH_Roof')
    box('Chimney',(.81,h+.77,.24),(.27,.63,.29),'CH_Stucco',.035);box('ChimneyCap',(.81,h+1.1,.24),(.35,.07,.35),'CH_Porcelain',.018)
    if index==1:
        box('FlowerWindowBox',(.65,h-1.01,-.99),(.93,.17,.34),'CH_Terracotta',.025)
        for i in range(7):ball('WindowFlowers',(.29+i*.12,h-.85,-1.0),(.09,.10,.09),'CH_MintBright' if i%2 else 'CH_CoralBright')
    if index==2:
        box('BalconyDeck',(0,h-.98,-1.07),(2.28,.09,.64),'CH_Porcelain')
        line('BalconyTop',[(-1.10,h-.56,-1.37),(1.10,h-.56,-1.37)],.022,'CH_Gold')
        for i in range(13):cyl('BalconyBaluster',(-1.08+i*.18,h-.77,-1.37),.012,.42,'CH_Gold')
    plant(1.08,.20,-1.19,.76);plant(-1.08,.20,-1.18,.63)
    g.save('BoulevardFacade'+str(index))

def street():
    g.begin()
    # Continuous 8m tile grid, rounded edges and deliberate grout; no road dashes.
    for z in range(16):
        for x in range(10):box('LimestonePaver',(-2.16+x*.48,-.021,-3.75+z*.5),(.473,.092,.491),'CH_Paving',.009)
    for side in [-1,1]:
        for z in range(16):
            box('CurbStone',(side*2.47,.047,-3.75+z*.5),(.14,.18,.49),'CH_Porcelain',.025)
            for x in range(2):box('SidewalkStone',(side*(2.81+x*.48),.091,-3.75+z*.5),(.472,.13,.49),'CH_Stucco',.016)
        # Tiny inlaid brass marks delimit lanes without painting a motorway.
    for x in [-.675,.675]:
        for z in [-3,-1,1,3]:box('BrassInlay',(x,.029,z),(.028,.008,.10),'CH_Gold',.005)
    g.save('BoulevardStreet')

def lamp():
    g.begin();box('LampFoot',(0,.09,0),(.33,.18,.33),'CH_Paving',.05)
    cyl('FlutedPole',(0,.94,0),.035,1.65,'CH_Roof')
    line('Gooseneck',[(0,1.72,0),(0,1.91,-.02),(0,2.0,-.14),(0,1.99,-.28),(0,1.91,-.35)],.033,'CH_Roof')
    box('LanternGlass',(0,1.76,-.35),(.19,.26,.19),'CH_Porcelain',.03)
    for x in [-.09,.09]:
        for z in [-.44,-.26]:cyl('LanternFrame',(x,1.76,z),.012,.29,'CH_Roof')
    box('LanternHat',(0,1.92,-.35),(.29,.07,.27),'CH_Roof');box('LanternFoot',(0,1.60,-.35),(.23,.055,.23),'CH_Roof');g.save('BoulevardLamp')

def bench(x=0,z=0):
    for side in [-1,1]:box('BenchLeg',(x+side*.47,.22,z),(.065,.44,.47),'CH_Roof',.018)
    for i in range(5):box('SeatSlat',(x,.46,z-.20+i*.10),(1.16,.055,.083),'CH_Cream',.019)
    for i in range(3):box('BackSlat',(x,.61+i*.13,z+.25),(1.16,.10,.055),'CH_Cream',.024)

def landmark(index):
    g.begin()
    if index in [0,2,3,6]:
        # Different shop forecourts, one clear silhouette per theme.
        if index==6:
            box('LittleLibrary',(0,.84,0),(1.16,1.58,.42),'CH_TealLight',.06)
            for y in [.22,.68,1.12]:
                box('Shelf',(0,y,-.13),(1.10,.06,.49),'CH_Cream')
                for j in range(7):box('Book',(-.44+j*.14,y+.22,-.19),(.09,.38-(j%3)*.045,.22),['CH_CoralBright','CH_MintBright','CH_LilacBright'][j%3],.011)
            badge((0,1.53,-.24),.51);bench(1.27,0)
        elif index==3:
            cyl('CafeTableTop',(0,.67,0),.40,.07,'CH_Cream');cyl('CafeTableStem',(0,.34,0),.035,.65,'CH_Roof');cyl('TableFoot',(0,.06,0),.23,.08,'CH_Roof')
            for x in [-.71,.71]:
                cyl('StoolSeat',(x,.38,0),.23,.07,'CH_TealLight')
                for a in [0,2.1,4.2]:cyl('StoolLeg',(x+.13*math.cos(a),.18,.13*math.sin(a)),.023,.36,'CH_Cream')
            for x in [-.16,.16]:cyl('Teacup',(x,.76,0),.063,.12,'CH_Porcelain');cyl('Tea',(x,.823,0),.049,.004,'CH_Terracotta')
        else:
            box('MarketCabinet',(0,.37,0),(1.45,.74,.61),'CH_TealLight' if index==0 else 'CH_CoralBright',.055)
            box('Countertop',(0,.76,-.04),(1.60,.07,.79),'CH_Cream')
            for x in [-.66,.66]:cyl('AwningPost',(x,.94,.21),.029,1.88,'CH_Cream')
            g.P.append(k.sheet('Canopy',12,8,lambda u,v:((u-.5)*1.69,1.72+.17*math.sin(v*math.pi),-.58+v*.91),[g.M['CH_Porcelain'],g.M['CH_TealLight' if index==0 else 'CH_CoralBright']],lambda c:(c//2)%2,.02))
            for i in range(5):box('TreatTin',(-.52+i*.26,.92,-.15),(.20,.26,.25),['CH_Porcelain','CH_LilacBright','CH_CoralBright'][i%3],.028)
            badge((0,.42,-.32),.8)
    elif index in [1,5]:
        box('ToyCart',(0,.48,0),(1.62,.39,.80),'CH_CoralBright' if index==5 else 'CH_TealLight',.075)
        for x in [-.56,.56]:
            for z in [-.41,.41]:
                cyl('RubberWheel',(x,.20,z),.20,.075,'CH_Roof','Z');cyl('WheelHub',(x,.20,z-.04),.09,.085,'CH_Gold','Z')
        if index==5:line('CartPullHandle',[(.79,.54,0),(1.13,.98,0),(1.38,.98,0)],.028,'CH_Cream')
        for i in range(5):
            ball('ToyYarn',(-.60+i*.29,.86+(i%2)*.12,0),(.21,.21,.21),['CH_LilacBright','CH_MintBright','CH_CoralBright'][i%3])
            line('YarnStrand',[(-.60+i*.29+.21*math.cos(t*.2),.87+.21*math.sin(t*.2),-.018) for t in range(33)],.008,'CH_Porcelain')
        badge((0,.46,-.42),.75)
    elif index==4:
        for j in range(3):
            box('GardenTier',(0,.16+j*.24,.34*j),(1.90,.07,.47),'CH_Cream')
            for i in range(3):plant(-.63+i*.63,.2+j*.24,.34*j,.7)
    elif index==7:
        cyl('FountainFoot',(0,.09,0),.78,.18,'CH_Paving');cyl('FountainBasin',(0,.26,0),.71,.30,'CH_Porcelain')
        cyl('Water',(0,.415,0),.62,.017,'CH_Screen');cyl('FountainColumn',(0,.65,0),.15,.60,'CH_Stucco')
        cyl('UpperBowl',(0,.95,0),.34,.11,'CH_Porcelain');badge((0,.62,-.155),.6)
        for i in range(8):
            a=i*math.tau/8;line('WaterArc',[(math.cos(a)*(.3+t*.023),.99-t*t*.005,math.sin(a)*(.3+t*.023)) for t in range(11)],.012,'CH_Screen')
    else:
        for x in [-1,1]:
            for z in [-.60,.60]:box('PergolaPost',(x,1.13,z),(.10,2.26,.10),'CH_Cream')
        for i in range(8):box('PergolaRafter',(-1.15+i*.329,2.27,0),(.085,.12,1.55),'CH_Cream')
        for z in [-.61,.61]:box('CrossBeam',(0,2.15,z),(2.42,.14,.13),'CH_Cream')
        bench();plant(-.85,.02,-.8,1.2);plant(.85,.02,-.8,1.2)
    g.save('BoulevardLandmark'+str(index))

def portals():
    # Two distinct play obstacles with generous poles outside the cat's lane.
    for index in range(2):
        g.begin();c='CH_TealLight' if index==0 else 'CH_CoralBright'
        for x in [-.52,.52]:
            box('WeightedFoot',(x,.06,0),(.17,.12,.60),'CH_Paving',.035)
            box('TimberPost',(x,.46,0),(.072,.80,.072),'CH_Cream',.022)
        box('SoftLowerRail',(0,.554,0),(1.12,.108,.35),c,.045)
        g.P.append(k.sheet('TailoredCanopy',18,9,lambda u,v:((u-.5)*1.15,.75+.12*math.sin(v*math.pi),-.37+v*.74),[g.M[c],g.M['CH_Porcelain']],lambda col:(col//3)%2,.016))
        for x in [-.54,.54]:line('EdgePiping',[(x,.75+.12*math.sin(t*math.pi/18),-.37+t*.74/18) for t in range(19)],.012,'CH_Gold')
        badge((0,.565,-.19),.44)
        g.save('RunnerRibbonGate' if index==0 else 'RunnerNapCanopy')

def hazards():
    g.begin()
    # Woven basket: real open lip, continuous weave and linen interior.
    box('BasketBase',(0,.055,0),(.88,.11,.59),'CH_Cream',.065)
    for j in range(11):
        seam('RattanCourse',.12+j*.023,.42,.28,'CH_Cream')
    for j in range(34):
        a=j*math.tau/34
        line('WovenStake',[(.414*math.cos(a),.09,.274*math.sin(a)),(.437*math.cos(a),.39,.295*math.sin(a))],.009,'CH_Cream')
    seam('LinenCuff',.365,.434,.292,'CH_Porcelain')
    for x,z,c in [(-.20,.06,'CH_TealLight'),(.17,.06,'CH_CoralBright'),(0,-.13,'CH_LilacBright')]:
        ball('YarnCore',(x,.38,z),(.19,.19,.19),c)
        for j in range(6):
            a=j*.49
            line('SpunYarn',[(x+.192*math.cos(t*.15),.38+.192*math.sin(t*.15)*math.cos(a),z+.192*math.sin(t*.15)*math.sin(a)) for t in range(43)],.007,'CH_Porcelain')
    badge((0,.17,-.302),.62);g.save('RunnerYarnBasket')
    g.begin();box('WeightedPedestal',(0,.07,0),(.64,.14,.56),'CH_Porcelain',.06)
    cyl('SisalCore',(0,.44,0),.126,.60,'CH_Cream')
    line('SisalTwist',[(.133*math.cos(i*.38),.16+i*.0024,.133*math.sin(i*.38)) for i in range(242)],.0065,'CH_Cream')
    for y in [.17,.71]:cyl('SisalCollar',(0,y,0),.145,.04,'CH_Gold')
    box('SuedeCap',(0,.77,0),(.41,.10,.38),'CH_TealLight',.045);badge((0,.072,-.29),.48);g.save('RunnerScratchPost')
    g.begin();cyl('CeramicFoot',(0,.034,0),.37,.068,'CH_TealLight')
    for j in range(8):seam('PorcelainRim',.068+j*.016,.34+j*.006,.34+j*.006,'CH_Porcelain')
    cyl('GlazedWell',(0,.093,0),.30,.11,'CH_Terracotta')
    for i in range(24):
        a=i*2.4;r=.04+.215*(i%5)/4;ball('Kibble',(r*math.cos(a),.16,r*math.sin(a)),(.024,.014,.023),'CH_Cream')
    badge((0,.105,-.39),.42);g.save('RunnerFoodBowl')
    g.begin();cyl('RubberBumper',(0,.09,0),.41,.17,'CH_Roof');cyl('CeramicShell',(0,.18,0),.388,.11,'CH_Porcelain')
    cyl('SensorTurret',(0,.26,.10),.092,.11,'CH_TealLight');seam('ShellJoint',.236,.342,.342,'CH_Gold')
    for i in range(9):box('ExhaustVent',(-.16+i*.04,.235,-.15),(.016,.016,.07),'CH_Roof',.006)
    box('StatusBar',(0,.237,-.26),(.11,.012,.025),'CH_MintBright',.008)
    for x in [-.37,.37]:cyl('DriveWheel',(x,.06,0),.063,.08,'CH_Roof','X')
    badge((0,.21,-.373),.27);g.save('RunnerVacuum')
    g.begin();box('BedPlinth',(0,.035,0),(.91,.07,.72),'CH_Cream',.06)
    box('LinenCushion',(0,.14,-.035),(.79,.21,.60),'CH_TealLight',.088)
    for x in [-.385,.385]:box('SideBolster',(x,.26,.015),(.16,.31,.63),'CH_Porcelain',.076)
    box('BackBolster',(0,.33,.285),(.87,.24,.18),'CH_Porcelain',.082)
    seam('CushionPiping',.185,.35,.26,'CH_Cream')
    for x in [-.14,.14]:ball('TuftButton',(x,.247,-.015),(.025,.012,.025),'CH_MintBright')
    badge((0,.055,-.366),.41);g.save('RunnerCatBed')
    g.begin()
    for x,h,c in [(-.22,.64,'CH_TealLight'),(.20,.52,'CH_CoralBright')]:
        box('TreatTin',(x,h*.5,0),(.36,h,.33),c,.063)
        box('BrassBase',(x,.022,0),(.366,.044,.338),'CH_Gold',.025)
        box('TinLid',(x,h-.025,0),(.378,.052,.35),'CH_Cream',.03)
        box('Label',(x,h*.51,-.174),(.245,.28,.014),'CH_Porcelain',.028)
        badge((x,h*.53,-.185),.41)
        for y in [h*.32,h*.29]:box('PrintedRule',(x,y,-.187),(.13,.012,.005),'CH_Gold',.003)
    g.save('RunnerTreats')
    g.begin();box('CarrierFloor',(0,.05,0),(.88,.10,.70),'CH_Roof',.06)
    box('SoftBackPanel',(0,.37,.275),(.83,.59,.16),'CH_TealLight',.075)
    for x in [-.365,.365]:box('SidePanel',(x,.35,.005),(.14,.60,.64),'CH_TealLight',.06)
    box('ArchedRoof',(0,.627,.005),(.78,.135,.63),'CH_TealLight',.066)
    box('InteriorPad',(0,.127,0),(.69,.09,.55),'CH_Porcelain',.04)
    line('DoorPiping',[(-.33,.14,-.36),(-.33,.52,-.36),(-.24,.62,-.36),(.24,.62,-.36),(.33,.52,-.36),(.33,.14,-.36),(-.33,.14,-.36)],.022,'CH_Cream')
    for x in [-.22,-.11,0,.11,.22]:box('SafetyMesh',(x,.36,-.34),(.013,.44,.013),'CH_Roof',.005)
    for y in [.22,.32,.42,.52]:box('SafetyMesh',(0,y,-.34),(.62,.012,.012),'CH_Roof',.004)
    line('CarryHandle',[(-.15,.69,0),(-.15,.79,0),(.15,.79,0),(.15,.69,0)],.025,'CH_Cream')
    badge((0,.636,-.337),.36);g.save('RunnerCarrier')
    g.begin()
    for i,c in enumerate(['CH_CoralBright','CH_Porcelain','CH_TealLight']):
        box('TailoredPillow',(0,.078+i*.14,0),(.88-i*.10,.15,.65-i*.055),c,.071)
        seam('PipedHem',.081+i*.14,.418-i*.05,.306-i*.027,'CH_Cream')
    for x in [-.16,.16]:ball('QuiltTuft',(x,.43,0),(.026,.009,.026),'CH_MintBright')
    badge((0,.071,-.333),.38);g.save('RunnerPillows')

def boardwalk():
    g.begin()
    def height(z):
        t=min(1,max(0,(6.5-abs(z))/3))
        return .82*t*t*(3-2*t)
    for i in range(80):
        z=-6.5+(i+.5)*13/80
        h=height(z)
        slope=(height(z+.01)-height(z-.01))/.02
        g.P.append(k.cube('OakSlat',(0,h-.035,z),(1.18,.07,.156),g.M['CH_Cream'],.012,rotation=(-math.atan(slope),0,0)))
        for x in [-.52,.52]:
            ball('FlushBrassFastener',(x,h+.001,z),(.015,.004,.015),'CH_Gold')
    for x in [-.58,.58]:
        points=[(x,height(-6.5+i*13/64)-.025,-6.5+i*13/64) for i in range(65)]
        for a,b in zip(points,points[1:]):
            g.P.append(k.strut('MintStringer',a,b,.045,g.M['CH_TealLight'],vertices=8,width=0))
    for z in [-3.6,-1.8,0,1.8,3.6]:
        h=height(z)
        for x in [-.45,.45]:box('TrestleLeg',(x,h*.5-.035,z),(.12,max(.1,h-.07),.16),'CH_TealLight',.016)
        box('TrestleBrace',(0,h*.42,z),(1.06,.10,.13),'CH_Cream',.016)
    g.save('RunnerBoardwalk')

if '--hazards-only' not in sys.argv:
    for i in range(3):facade(i)
    street();lamp()
    for i in range(9):landmark(i)
    portals()
hazards();boardwalk()
with open(os.path.join(g.ROOT,'boulevard_metrics.json'),'w',encoding='utf8') as f:json.dump(g.metrics,f,indent=2)
print('BOULEVARD_COMPLETE',len(g.metrics))
