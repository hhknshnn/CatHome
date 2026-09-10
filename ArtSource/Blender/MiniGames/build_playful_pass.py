"""Small, original festival accents; no new runtime lights or animated props."""
import os,sys,math,json
import bpy
sys.path.insert(0,os.path.dirname(os.path.abspath(__file__)))
import build_minigame_collection as g
from build_minigame_collection import box,ball,cyl,line

g.arena()
g.begin()
for side in [-1,1]:
    cyl('StreetPole',(side*2.8,1.78,0),.035,3.4,'CH_Teal')
    ball('PoleFinial',(side*2.8,3.51,0),(.075,.075,.075),'CH_Gold')
    box('PoleFoot',(side*2.8,.11,0),(.27,.22,.27),'CH_Cream',.035)
line('FestivalCord',[(x,3.12+.34*(abs(x)/2.8)**2,0) for x in [-2.8+i*.2 for i in range(29)]],.012,'CH_Gold')
for i in range(12):
    x=-2.5+i*.455;y=3.12+.34*(abs(x)/2.8)**2
    mesh=bpy.data.meshes.new('Flag');mesh.from_pydata([(x-.15,y,0),(x+.15,y,0),(x,y-.28,0)],[],[(0,1,2)])
    ob=bpy.data.objects.new('FestivalPennant',mesh);bpy.context.collection.objects.link(ob);ob.data.materials.append(g.M[['CH_CoralBright','CH_MintBright','CH_LilacBright','CH_White'][i%4]]);g.P.append(ob)
g.save('RunnerFestivalSpan')
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(g.ROOT,'PlayfulPass_Source.blend'))
with open(os.path.join(g.ROOT,'playful_metrics.json'),'w',encoding='utf8') as f:json.dump(g.metrics,f,indent=2)
print('PLAYFUL_PASS_COMPLETE')
