from pathlib import Path
from html import escape
from PIL import Image, ImageOps, ImageDraw, ImageFont

base = Path(__file__).resolve().parent
names = ['Canlı kedi açılışı','Yapımcılar','Yeni oyun onayı','Hesap seçimi','Ev','Ev menüsü','Mobilya mağazası','Kedi eşyaları','Oda mağazası','Kedim','Odalar','Ayarlar','Gizlilik','Veri silme onayı','Ev görevleri','Günlük görevler','Satın alma','Gerekli ürün','Elmas harcama onayı','Elmas paketleri','Oyunlar','Sıralama: boş durum','Sıralama: örnek liste','Geri dönüş','Yuva seviyesi','Koleksiyon tamamlandı','İlk gün kutlaması','Kediyle tanışma','İsim seçimi','Runner girişi','Runner oyunu','Runner öğreticisi','Runner duraklatma','Runner sonucu','Runner: can yok','Catch girişi','Catch oyunu','Catch öğreticisi','Catch duraklatma','Catch sonucu','Catch: can yok','Yakındaki bakım eylemi','Kedi konuşması','Aktivite ilerlemesi','Başarım bildirimi']
files=sorted((base/'screens').glob('*_Final.png'))
cards=[]
for p in files:
    prefix=p.stem.split('_')[0]
    title=names[int(prefix)-1] if prefix.isdigit() and 0<int(prefix)<=len(names) else p.stem.replace('_Final','').replace('_',' · ')
    example=any(k in p.name for k in ['Example','DiamondConfirmation','Purchase','Prerequisite','NoEnergy','NoLives','Tutorial'])
    group='Oran ve dil' if not prefix.isdigit() else 'Mini oyunlar' if 30<=int(prefix)<=41 else 'Ev ve menüler'
    with Image.open(p) as im: size=f'{im.width} × {im.height}'
    rel='screens/'+p.name
    label='Örnek durum / veri' if example else 'Canlı oyun görünümü'
    cards.append(f'<article data-group="{group}" data-title="{escape(title)}"><a href="{rel}" target="_blank"><img src="{rel}" loading="lazy" alt="{escape(title)}"></a><div><h2>{escape(title)}</h2><p>{size} · {label}</p><a href="{rel}" download>HD görüntüyü indir</a></div></article>')
html='''<!doctype html><html lang="tr"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Cat Home · Uygulanan UI/UX</title><style>
*{box-sizing:border-box}body{margin:0;background:#fff9ef;color:#243536;font:16px/1.6 system-ui,sans-serif}header,main,footer{max-width:1560px;margin:auto;padding:32px}header{padding-top:54px}h1{font-size:42px;letter-spacing:-1px;margin:0 0 12px}header p{max-width:970px}a{color:#14776f}nav{display:flex;gap:12px;flex-wrap:wrap;margin:25px 0}button,input{border:1px solid #d2d8cc;border-radius:14px;background:#ddede3;color:#243536;padding:12px 20px;font:inherit}button{cursor:pointer}button.active{background:#218f87;color:white}input{background:white;min-width:260px}.grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:28px}article{overflow:hidden;border:1px solid #d8d4c9;border-radius:22px;background:#fffdf8}article img{display:block;width:100%;height:auto}article div{padding:16px 22px 23px}h2{font-size:21px;margin:0}article p{margin:6px 0;color:#61756f;font-size:14px}small{display:block;color:#61756f}.note{padding:20px 24px;border-radius:18px;background:#ddede3}footer{font-size:14px;color:#61756f}@media(max-width:850px){.grid{grid-template-columns:1fr}header,main,footer{padding:22px}h1{font-size:32px}}</style>
<header><small>CAT HOME · 6 EYLÜL 2026 · UNITY OYUN GÖRÜNTÜLERİ</small><h1>Daha net yazılar, daha özenli yüzeyler.</h1><p>Sıcak krem ve mint yüzeyler, belirgin turkuaz seçimler, mercan ana eylemler. Oyunun gerçek kedileri, eşyaları ve odalarıyla uygulanan arayüz. Aşağıdaki kareler tasarım çizimi değil, çalışan Unity ekranlarından alınmıştır.</p>
<p><a href="../UIUX_2026-09-06/index.html">Önceki arayüz</a> · <a href="../../UIUX_References_2026-09-06/index.html">Onaylanan tasarım referansları</a> · <a href="../../UIUX_REFINEMENT_2026-09-06.md">Uygulama ve doğrulama kaydı</a></p>
<div class="note">Görüntülere tıklayarak gerçek çözünürlükte açabilirsiniz. Ödül, sıralama, satın alma ve can bitmesi gibi koşullu ekranlar ayrı bir yerel QA kaydı üzerinde gösterildi. Örnek skorlar ve satın almalar oyuncunun kaydına uygulanmadı; ödeme, reklam ve hesap sağlayıcıları çağrılmadı.</div>
<nav><button class="active" data-filter="">Hepsi</button><button data-filter="Ev ve menüler">Ev ve menüler</button><button data-filter="Mini oyunlar">Mini oyunlar</button><button data-filter="Oran ve dil">Oran ve dil</button><input id="search" placeholder="Ekran ara…" aria-label="Ekran ara"></nav></header>
<main class="grid">'''+''.join(cards)+'''</main><footer>HD kareler yeniden büyütülmedi. Masaüstü Unity kontrolü fiziksel Android cihaz performansı, ekran klavyesi veya platform mağazası doğrulaması yerine geçmez.</footer><script>let filter='';const search=document.querySelector('#search');function update(){document.querySelectorAll('article').forEach(a=>a.hidden=!!((filter&&a.dataset.group!==filter)||!a.dataset.title.toLocaleLowerCase('tr').includes(search.value.toLocaleLowerCase('tr'))))}document.querySelectorAll('button').forEach(b=>b.onclick=()=>{filter=b.dataset.filter;document.querySelectorAll('button').forEach(x=>x.classList.toggle('active',x===b));update()});search.oninput=update;</script></html>'''
(base/'index.html').write_text(html,encoding='utf-8')
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',18)
for page in range((len(files)+11)//12):
    sheet=Image.new('RGB',(1440,1232),'#fff9ef');draw=ImageDraw.Draw(sheet)
    for i,p in enumerate(files[page*12:page*12+12]):
        x=(i%3)*480;y=(i//3)*308
        with Image.open(p) as im:
            thumb=ImageOps.contain(im.convert('RGB'),(472,270));sheet.paste(thumb,(x+(480-thumb.width)//2,y))
        draw.text((x+10,y+275),p.stem.replace('_Final','')[:46],font=font,fill='#243536')
    sheet.save(base/f'contact_{page+1}.jpg',quality=92)
print(f'{len(files)} native screenshots; gallery and contact sheets ready.')
