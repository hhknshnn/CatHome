"""Assemble review sheets from the actual, unretouched Unity product renders."""
from pathlib import Path
import re, math
from PIL import Image, ImageDraw, ImageFont

folder=Path(__file__).resolve().parent
page=folder/'index.html'
markup=page.read_text(encoding='utf-8')
cards=re.findall(r'<article><a href="([^"]+)".*?<h2>(.*?)</h2>',markup)
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',19)
sheet=Image.new('RGB',(1280,math.ceil(len(cards)/4)*350),'#fff8ee')
draw=ImageDraw.Draw(sheet)
for i,(path,title) in enumerate(cards):
    x,y=(i%4)*320,(i//4)*350
    shot=Image.open(folder/path).convert('RGB').resize((310,310),Image.Resampling.LANCZOS)
    sheet.paste(shot,(x+5,y));draw.text((x+12,y+315),title,font=font,fill='#253b3b')
sheet.save(folder/'CAT_shop_contact.jpg',quality=94)
files=sorted((folder/'screens').glob('*_contact.png'))
sheet=Image.new('RGB',(1440,math.ceil(len(files)/3)*304),'#fff8ee');draw=ImageDraw.Draw(sheet)
for i,path in enumerate(files):
    x,y=(i%3)*480,(i//3)*304
    sheet.paste(Image.open(path).convert('RGB').resize((480,270),Image.Resampling.LANCZOS),(x,y))
    draw.text((x+12,y+274),path.stem.replace('_contact',''),font=font,fill='#253b3b')
sheet.save(folder/'CAT_interaction_contact.jpg',quality=94)
shots=[]
for name,title in [('CanopyBed_contact','Tenteli yatakta dinlenme'),('PlayTunnel_contact','Tünelden geçiş'),('Shop_CAT_1','Mağaza · ilk ürünler'),('Shop_CAT_Row2','Mağaza · mama ve merak oyunları'),('Shop_CAT_2','Mağaza · oyuncaklar ve yataklar'),('Shop_CAT_Row4','Mağaza · oyun koleksiyonu'),('Shop_CAT_3','Mağaza · tenteli yatak'),('Shop_CAT_Purchase','Ürünün satın alma ön izlemesi')]:
    if (folder/'screens'/f'{name}.png').exists():
        shots.append(f'<figure><a href="screens/{name}.png"><img src="screens/{name}.png" alt="{title}" loading="lazy"></a><figcaption>{title}</figcaption></figure>')
markup=re.sub(r'<div class="shots">.*?</div><small>','<div class="shots">'+''.join(shots)+'</div><small>',markup,flags=re.S)
page.write_text(markup,encoding='utf-8')
print(f'{len(cards)} product photos; {len(files)} interaction photos; {len(shots)} gallery frames')
