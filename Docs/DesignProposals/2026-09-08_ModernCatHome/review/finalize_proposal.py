from pathlib import Path
from PIL import Image
from html.parser import HTMLParser
from urllib.parse import unquote, urlsplit
import json, re, subprocess, zipfile, hashlib

root = Path(__file__).resolve().parent.parent
builder = root/'review/build_gallery.py'
source = builder.read_text(encoding='utf-8').replace(".replace('</','<\\/')", ".replace('</', '<' + chr(92) + '/')")
builder.write_text(source, encoding='utf-8')
subprocess.run(['python', str(builder)], check=True)
subprocess.run(['node', '--check', str(root/'gallery.js')], check=True)
items = json.loads((root/'review/manifest.json').read_text(encoding='utf-8'))
assert len(items) == 23
images = []
for item in items:
    p = root/'images'/item['file']
    with Image.open(p) as im:
        size = im.size
        im.verify()
    images.append(dict(file=item['file'], width=size[0], height=size[1], bytes=p.stat().st_size, sha256=hashlib.sha256(p.read_bytes()).hexdigest()))

class Links(HTMLParser):
    def __init__(self): super().__init__(); self.links=[]; self.ids=[]
    def handle_starttag(self, tag, attrs):
        attrs=dict(attrs)
        for key in ['href','src']:
            if attrs.get(key): self.links.append(attrs[key])
        if 'id' in attrs: self.ids.append(attrs['id'])

missing=[]
for page in root.glob('*.html'):
    content=page.read_text(encoding='utf-8')
    assert '\ufffd' not in content, page.name
    parser=Links(); parser.feed(content)
    assert len(parser.ids)==len(set(parser.ids)), 'Duplicate IDs '+page.name
    for link in parser.links:
        dest=urlsplit(link)
        if dest.scheme or not dest.path or dest.path.endswith('.zip'): continue
        if not (page.parent/unquote(dest.path)).exists(): missing.append([page.name,link])
assert not missing, missing
index=(root/'index.html').read_text(encoding='utf-8')
assert index.count('<article class="board"') == 23
report=dict(png_verified=23, javascript_syntax='passed', html_references='passed', browser_runtime='Not run: local-file browser preview was security-blocked.', images=images)
(root/'review/package-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
archive=root/'CatHome-Modern-Tasarim-Paketi.zip'
include=list(root.glob('*.md'))+list(root.glob('*.html'))+list(root.glob('*.css'))+list(root.glob('*.js'))+list((root/'images').glob('*.png'))+[root/'review/current-home.png',root/'review/package-check.json']
with zipfile.ZipFile(archive,'w',compression=zipfile.ZIP_DEFLATED,compresslevel=4) as z:
    for p in include:
        if p.name=='index.html':
            document=p.read_text(encoding='utf-8').replace('href="CatHome-Modern-Tasarim-Paketi.zip" download>Tam paketi indir', 'href="#gallery">23 görseli incele')
            z.writestr(p.relative_to(root).as_posix(), document)
        else: z.write(p,p.relative_to(root).as_posix())
with zipfile.ZipFile(archive) as z: assert z.testzip() is None
print(json.dumps(dict(boards=23,archive=str(archive),bytes=archive.stat().st_size,files=len(include)),ensure_ascii=False))
