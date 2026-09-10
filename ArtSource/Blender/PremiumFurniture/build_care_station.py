"""Wall-backed paired care tray; headless Blender, authored in Unity Y-up."""
from pathlib import Path
import bpy
import sys
sys.path.insert(0,str(Path(__file__).parent))
import premium_kit as k

ROOT=Path(__file__).resolve().parents[3]
k.UNITY_MODELS=str(ROOT/'Assets/Art/PremiumFurniture/Models')
k.clear_scene()
c=k.palette(k.CH_COLORS)
parts=[]
def box(name,p,s,mat,r=.02):
    parts.append(k.cube(name,p,s,c[mat],width=r,segments=4))
box('IvoryTray',(0,.012,0),(1.40,.024,.55),'CH_White',.012)
box('MintInset',(0,.026,-.015),(1.34,.004,.48),'CH_MintBright',.012)
box('WallBack',(0,.085,.255),(1.40,.17,.04),'CH_White',.018)
box('GoldLip',(0,.044,.231),(1.33,.01,.016),'CH_Gold',.004)
for x in [-.34,.34]:
    k.paw_badge(parts,(x,.111,.231),(0,0,-1),c['CH_Gold'],.14)
obj=k.join_fixture('CareStationTray',parts)
k.export_fixture(obj,'CareStationTray_Premium.fbx')
bpy.ops.wm.save_as_mainfile(filepath=str(Path(__file__).with_name('CareStationTray_Source.blend')))
