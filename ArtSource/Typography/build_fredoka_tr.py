"""Static Fredoka instances with Turkish composites from the font's own accents.

Source: Google Fonts ofl/fredoka, SIL OFL (adjacent Fredoka-OFL.txt).
The upstream font has breve/cedilla/dotaccent but no cmap for Gbreve,
gbreve, Scedilla, scedilla or Idotaccent. No glyph from another face is used.
"""
import sys
from pathlib import Path
root=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(root/'Library/UiQaTools'))
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont
from fontTools.pens.ttGlyphPen import TTGlyphPen

source=Path(__file__).with_name('Fredoka-Variable.ttf')
for weight,style in [(500,'Medium'),(600,'SemiBold')]:
    font=instantiateVariableFont(TTFont(source),{'wght':weight,'wdth':100},inplace=True)
    glyf=font['glyf'];metrics=font['hmtx'].metrics
    for code,name,base,accent in [(0x11e,'Gbreve','G','breve'),(0x11f,'gbreve','g','breve'),(0x130,'Idotaccent','I','dotaccent'),(0x15e,'Scedilla','S','cedilla'),(0x15f,'scedilla','s','cedilla')]:
        b=glyf[base];a=glyf[accent];b.recalcBounds(glyf);a.recalcBounds(glyf)
        dx=round((b.xMin+b.xMax-a.xMin-a.xMax)/2)
        dy=0 if accent=='cedilla' else round(b.yMax+70-a.yMin)
        pen=TTGlyphPen(font.getGlyphSet());pen.addComponent(base,(1,0,0,1,0,0));pen.addComponent(accent,(1,0,0,1,dx,dy))
        glyph=pen.glyph();glyf[name]=glyph;glyph.recalcBounds(glyf)
        metrics[name]=(metrics[base][0],glyph.xMin)
        order=font.getGlyphOrder()
        if name not in order:font.setGlyphOrder(order+[name])
        for table in font['cmap'].tables:
            if table.isUnicode():table.cmap[code]=name
    for name_id,value in [(1,'Cat Home Fredoka'),(2,style),(4,'Cat Home Fredoka '+style),(6,'CatHomeFredoka-'+style),(16,'Cat Home Fredoka'),(17,style)]:
        for platform,encoding,language in [(3,1,0x409),(1,0,0)]:font['name'].setName(value,name_id,platform,encoding,language)
    font['OS/2'].usWeightClass=weight
    output=root/'Assets/Fonts'/('Fredoka-'+style+'.ttf');font.save(output)
    assert all(ord(c) in font.getBestCmap() for c in 'ĞğİıŞşÇçÖöÜü')
    print(style, 'all Turkish glyphs use Fredoka geometry')
