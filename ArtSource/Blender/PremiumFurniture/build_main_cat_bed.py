"""Wall-backed upholstered cat daybed. Headless Blender; front -Z, Y up."""
import bpy, math, sys, json
from pathlib import Path
sys.path.insert(0,str(Path(__file__).parent))
import premium_kit as k

def build(preview=False):
    k.clear_scene()
    root=Path(__file__).resolve().parents[3]
    k.UNITY_MODELS=str(root/'Assets/Art/PremiumFurniture/Models')
    parts=[]; c=k.palette(k.CH_COLORS)
    def box(name,at,size,material,bevel=.04):
        o=k.cube(name,at,size,c[material],width=bevel,segments=6);parts.append(o);return o
    # Recessed feet, a fine champagne reveal and a softly rounded ivory shell.
    for x in [-.43,.43]:
        for z in [-.27,.27]:
            parts.append(k.cylinder('TuckedBrassFoot',(x,.033,z),.026,.066,c['CH_Gold'],vertices=32))
    box('ChampagneReveal',(0,.073,0),(1.075,.027,.745),'CH_Gold',.013)
    box('IvoryUpholsteredBase',(0,.122,0),(1.10,.093,.78),'CH_White',.046)
    # The back is straight, with its rear face at +.39; no narrow curved wall pocket.
    box('PaddedBack',(0,.288,.297),(1.10,.30,.186),'CH_White',.075)
    for x in [-.49,.49]:
        box('RoundedBolster',(x,.247,-.035),(.12,.225,.67),'CH_White',.059)
    box('MintPillowTop',(0,.186,-.05),(.958,.112,.66),'CH_MintBright',.055)
    # Sewn border follows the real cushion edge. Fine thread, not chunky trim.
    border=[]
    for cx,cz,begin in [(.425,.225,0),(-.425,.225,90),(-.425,-.325,180),(.425,-.325,270)]:
        for i in range(13):
            a=math.radians(begin+i*90/12)
            border.append((cx+.025*math.cos(a),.243,cz+.025*math.sin(a)))
    border.append(border[0]);k.tube(parts,'IvoryCushionPiping',border,.0045,c['CH_White'],vertices=10)
    # Vertical upholstered channels add softness to the back without occupying the sleep pad.
    for x in [-.36,-.12,.12,.36]:
        box('MintBackChannel',(x,.302,.193),(.225,.223,.047),'CH_MintBright',.023)
    # Small side label and a centered gold paw stitch on the low front apron.
    box('CoralFabricTab',(.405,.135,-.398),(.10,.045,.014),'CH_CoralBright',.007)
    k.paw_badge(parts,(0,.128,-.395),(0,0,-1),c['CH_Gold'],.25)
    obj=k.join_fixture('MainCatBed',parts)
    k.export_fixture(obj,'MainCatBed_Premium.fbx')
    bpy.ops.wm.save_as_mainfile(filepath=str(Path(__file__).with_name('MainCatBed_Source.blend')))
    verts=[obj.matrix_world@v.co for v in obj.data.vertices]
    Path(__file__).with_name('MainCatBed_Metrics.json').write_text(json.dumps({'min':[min(v[i] for v in verts) for i in range(3)],'max':[max(v[i] for v in verts) for i in range(3)],'sleepCenter':[0,.242,-.065]},indent=2))
    if preview:
        import premium_preview as p
        p.PREVIEW_FOLDER=str(root/'Docs/QA/TV_BED_2026-09-07/art')
        p.render_views(obj,'MainCatBed',front_plus_z=False)
    return obj

if __name__=='__main__':build('--preview' in sys.argv)
