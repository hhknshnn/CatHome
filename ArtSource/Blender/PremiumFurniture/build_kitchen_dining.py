"""Kitchen centre dining set, authored in metres and Unity Y-up coordinates."""
import os, sys, math
import bpy
sys.path.insert(0, os.path.dirname(__file__))
from premium_kit import *

clear_scene()
m=palette(['CH_White','CH_Cream','CH_Gold','CH_MintBright','CH_CoralBright','CH_TealLight','CH_Ink'])
parts=[]
parts.append(cube('Mint apron',(0,.697,0),(1.70,.09,1.01),m['CH_MintBright'],.045))
parts.append(cube('Cream tabletop',(0,.75,0),(1.80,.06,1.12),m['CH_White'],.028))
for x in [-.73,.73]:
    for z in [-.40,.40]:
        parts.append(strut('Oak tapered leg',(x*1.06,.04,z*1.08),(x,.704,z),.041,m['CH_Cream']))
        parts.append(cylinder('Gold foot',(x*1.06,.045,z*1.08),.045,.09,m['CH_Gold']))
# A slim inset stripe leaves the working tabletop perfectly flat.
for z in [-.49,.49]: parts.append(cube('Inset mint line',(0,.7802,z),(1.59,.001,.009),m['CH_MintBright'],0))
for x in [-.795,.795]: parts.append(cube('Inset mint line',(x,.7802,0),(.009,.001,.98),m['CH_MintBright'],0))
for x in [-.55,.55]:
    for z in [-.385,.385]:
        parts.append(cube('Place mat',(x,.7818,z),(.34,.0024,.25),m['CH_MintBright'],.001))
        parts.append(cylinder('Dinner plate',(x-.025,.787,z),.095,.007,m['CH_White'],vertices=32,width=.003))
        parts.append(torus('Fine plate rim',(x-.025,.793,z),.089,.0035,m['CH_Gold'],major_segments=32,minor_segments=8))
        parts.append(cube('Folded napkin',(x+.125,.787,z),(.045,.010,.16),m['CH_CoralBright'],.004))
static=[join_fixture('DiningTable',parts)]
for row,z in enumerate([-.80,.80]):
    for col,x in enumerate([-.49,.49]):
        side=-1 if z<0 else 1
        p=[cube('Seat frame',(x,.386,z),(.43,.06,.41),m['CH_Cream'],.026),
           cube('Soft seat',(x,.438,z),(.40,.055,.385),m['CH_CoralBright' if (row+col)%2==0 else 'CH_MintBright'],.024)]
        for dx in [-.16,.16]:
            for dz in [-.145,.145]:
                p.append(strut('Chair leg',(x+dx*1.12,.025,z+dz*1.14),(x+dx,.39,z+dz),.024,m['CH_Cream']))
                p.append(cylinder('Chair gold foot',(x+dx*1.12,.028,z+dz*1.14),.026,.056,m['CH_Gold'],vertices=20,width=.008))
            p.append(strut('Back post',(x+dx,.40,z+side*.17),(x+dx,.90,z+side*.19),.023,m['CH_Gold']))
        p.append(cube('Rounded back',(x,.747,z+side*.19),(.39,.265,.052),m['CH_Cream'],.023))
        p.append(cube('Back upholstery',(x,.747,z+side*.154),(.325,.205,.029),m['CH_MintBright' if (row+col)%2==0 else 'CH_CoralBright'],.012))
        static.append(join_fixture('DiningChair_'+str(row*2+col+1),p))
# Lightweight, unbreakable tabletop props: a hollow cup, shallow bowl and shaker.
props=[]
p=[cylinder('Cup base',(.03,.789,-.16),.045,.012,m['CH_TealLight'],width=.005),
   torus('Cup rim',(.03,.862,-.16),.041,.006,m['CH_Gold'],major_segments=32,minor_segments=8),
   torus('Cup handle',(.081,.829,-.16),.025,.007,m['CH_TealLight'],rotation=(0,math.pi/2,0),major_segments=24,minor_segments=8)]
# Ring wall mesh avoids a solid filled mug.
verts=[];faces=[]
for y,r in [(.795,.045),(.86,.045),(.86,.034),(.800,.034)]:
    for i in range(32):
        t=2*math.pi*i/32;verts.append((.03+math.cos(t)*r,y,-.16+math.sin(t)*r))
for ring in range(3):
    for i in range(32):
        j=(i+1)%32;faces.append((ring*32+i,ring*32+j,(ring+1)*32+j,(ring+1)*32+i))
mesh=bpy.data.meshes.new('CupWallMesh');mesh.from_pydata(verts,[],faces);mesh.materials.append(m['CH_TealLight']);obj=bpy.data.objects.new('CupWall',mesh);bpy.context.collection.objects.link(obj);smooth(obj);p.append(obj)
props.append(join_fixture('ThrowProp_1_Cup',p))
p=[cylinder('Bowl base',(.18,.789,-.17),.036,.016,m['CH_White'],width=.006),
   torus('Bowl rim',(.18,.878,-.17),.055,.007,m['CH_Gold'],major_segments=32,minor_segments=10)]
verts=[];faces=[]
for y,r in [(.795,.036),(.878,.055),(.878,.047),(.807,.029)]:
    for i in range(32):
        t=2*math.pi*i/32;verts.append((.18+math.cos(t)*r,y,-.17+math.sin(t)*r))
for ring in range(3):
    for i in range(32):
        j=(i+1)%32;faces.append((ring*32+i,ring*32+j,(ring+1)*32+j,(ring+1)*32+i))
mesh=bpy.data.meshes.new('BowlWallMesh');mesh.from_pydata(verts,[],faces);mesh.materials.append(m['CH_CoralBright']);obj=bpy.data.objects.new('BowlWall',mesh);bpy.context.collection.objects.link(obj);smooth(obj);p.append(obj)
props.append(join_fixture('ThrowProp_2_Bowl',p))
p=[cylinder('Shaker body',(.11,.823,-.025),.031,.082,m['CH_Cream'],width=.01),
   sphere('Shaker gold cap',(.11,.864,-.025),(.032,.012,.032),m['CH_Gold'],20,10)]
for dx,dz in [(-.009,0),(.009,0),(0,.01)]:p.append(sphere('Salt hole',(.11+dx,.875,-.025+dz),(.0025,.001,.0025),m['CH_Ink'],8,6))
props.append(join_fixture('ThrowProp_3_Shaker',p))
for obj in props:
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj;bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY',center='BOUNDS');obj.select_set(False)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(os.path.dirname(__file__),'KitchenDiningSet_Source.blend'))
export_fixture(static+props,'KitchenDiningSet_Premium.fbx')
print('Kitchen dining exported: one table, four chairs, three separate props.')
