from pathlib import Path
from PIL import Image, ImageOps, ImageDraw, ImageFont

base=Path(__file__).resolve().parent
previous=base.parent/'UIUX_2026-09-06_Refinement'/'build_gallery.py'
script=previous.read_text(encoding='utf-8')
script=script.replace("files=sorted((base/'screens').glob('*_Final.png'))", "files=sorted(p for p in (base/'screens').glob('*_Final.png') if not p.name.startswith('Trial'))")
script=script.replace('Daha net yazılar, daha özenli yüzeyler.', 'Referansın çizgisini oyuna taşıyan yeni görünüm.')
script=script.replace('Sıcak krem ve mint yüzeyler, belirgin turkuaz seçimler, mercan ana eylemler.', 'Blender’da modellenen üç boyutlu simgeler, katmanlı krem ve şampanya çerçeveler, belirgin eylemler ve daha temiz oda ışığı.')
script=script.replace('../UIUX_2026-09-06/index.html', '../UIUX_2026-09-06_Refinement/index.html')
script=script.replace('UIUX_REFINEMENT_2026-09-06.md', 'UIUX_REFERENCE_MATCH_2026-09-06.md')
comparison='''<section class="comparison"><h2>Önceki oyun → Yeni oyun</h2><p>Kaydırıcıyı hareket ettirerek gerçek oyun görüntülerini karşılaştırın. Kedinin konumu ve ihtiyaç değerleri canlı oturumlar arasında değişir.</p><div class="compare"><img src="screens/05_Home_Final.png" alt="Yeni oyun"><img id="before" src="../UIUX_2026-09-06_Refinement/screens/05_Home_Final.png" alt="Önceki oyun"><span class="old">Önceki</span><span class="new">Yeni</span></div><input id="compare" type="range" min="0" max="100" value="50" aria-label="Önceki ve yeni görünümü karşılaştır"><details><summary>Onaylanan görsel referansı da göster</summary><img class="reference" src="../../UIUX_References_2026-09-06/images/02_Home.png" alt="Tasarım referansı"><p>Bu kare tasarım referansıdır. Üstteki karşılaştırma ve aşağıdaki galeri çalışan oyunu gösterir.</p></details></section>'''
script=script.replace('<nav><button', comparison+'<nav><button')
script=script.replace('</style>', '.comparison{margin-top:36px}.compare{position:relative;overflow:hidden;border-radius:22px;margin-top:18px}.compare img{display:block;width:100%}.compare #before{position:absolute;inset:0;clip-path:inset(0 50% 0 0)}.compare span{position:absolute;top:16px;background:#fff9efd9;padding:6px 16px;border-radius:18px}.old{left:16px}.new{right:16px}#compare{width:100%;padding:0;accent-color:#218f87}.reference{width:100%;margin-top:16px;border-radius:22px}summary{cursor:pointer;color:#14776f}</style>')
script=script.replace("<script>let filter=", "<script>document.querySelector('#compare').oninput=e=>document.querySelector('#before').style.clipPath='inset(0 '+(100-e.target.value)+'% 0 0)';let filter=")
exec(compile(script,str(base/'gallery_template.py'),'exec'), {'__file__':str(base/'gallery_template.py')})

# Room photographs are actual Unity outputs, reviewed together after camera/lighting rebake.
room_paths=sorted((base.parents[2]/'Assets/Art/RoomPreviews').glob('*.png'))
sheet=Image.new('RGB',(1440,960),'#fff9ef');draw=ImageDraw.Draw(sheet)
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',20)
for i,p in enumerate(room_paths):
    x=(i%2)*720;y=(i//2)*240
    with Image.open(p) as im:
        thumb=ImageOps.contain(im.convert('RGB'),(704,208));sheet.paste(thumb,(x+(720-thumb.width)//2,y))
    draw.text((x+15,y+213),p.stem,font=font,fill='#243536')
sheet.save(base/'room_previews.jpg',quality=94)
