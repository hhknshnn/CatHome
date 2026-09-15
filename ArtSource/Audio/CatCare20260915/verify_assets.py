from pathlib import Path
import hashlib,json,wave
import numpy as np
from prepare_audio import PROJECT,QA,SR,wav_read,wav_write,edge

manifest=json.loads((PROJECT/'Assets/Resources/GameAudio/manifest.json').read_text())
rows=[]
for row in manifest['assets']:
    path=PROJECT/'Assets/Resources/GameAudio'/row['folder']/(row['name']+'.wav')
    assert hashlib.sha256(path.read_bytes()).hexdigest()==row['sha256'],row['name']
    data,sr=wav_read(path);assert sr==SR and np.isfinite(data).all()
    assert np.max(np.abs(data))<.98
    rows.append({'name':row['name'],'frames':len(data),'rms':float(np.sqrt(np.mean(data**2)))})
before,_=wav_read(QA/'title-before.wav');after,_=wav_read(PROJECT/'Assets/Resources/Music/CatHomeMenu.wav')
changed=np.any(before!=after,axis=1);permitted=np.zeros(len(changed),dtype=bool)
for a,b in [(11.55,12.33),(41.55,42.33)]:permitted[round(a*SR):round(b*SR)]=True
assert not np.any(changed&~permitted),'Other title samples must remain bit-identical'
for kind,spacing in [('Yuruyus',.33),('Kosma',.19)]:
    mix=np.zeros(round(5*SR))
    for i,at in enumerate(np.arange(.2,4.5,spacing)):
        sample,_=wav_read(PROJECT/f'Assets/Resources/GameAudio/Sfx/PawWood_{i%3+1}.wav')
        start=round(at*SR);mix[start:start+len(sample)]+=sample[:,0]*(.45 if kind=='Yuruyus' else .65)
    wav_write(QA/(kind+'.wav'),mix)
links=[('Mama — gerçek kuru mama kıtırtısı','../../../Assets/Resources/GameAudio/Sfx/Eat_1.wav'),
       ('Su — gerçek dil ve sıvı teması','../../../Assets/Resources/GameAudio/Sfx/Drink_1.wav'),
       ('Yürüyüş — pati sesinin dinleme örneği','Yuruyus.wav'),
       ('Koşma — pati sesinin dinleme örneği','Kosma.wav')]
# QA root is Docs/QA/<run>, so game assets are three levels up.
html='''<!doctype html><html lang="tr"><meta charset="utf-8"><title>Cat Home — Ses iyileştirmesi</title>
<style>body{font:18px system-ui;background:#f5f0e8;color:#322c31;max-width:820px;margin:48px auto;padding:0 20px}article{background:white;padding:24px;border-radius:18px;margin:18px 0}audio{width:100%}p{line-height:1.6}</style>
<h1>Cat Home · Ses iyileştirmesi</h1><p>Gerçek kedi kayıtlarından mama ve içme; yumuşak pati teması. Adım örnekleri ses karakterini gösterir; oyunda zamanlama kedinin gerçek patilerini takip eder.</p>'''
for title,url in links:html+=f'<article><h2>{title}</h2><audio controls preload="none" src="{url}"></audio></article>'
html+='<article><h2>Menü müziği — geçişleri düzeltilmiş 60 saniye</h2><audio controls preload="none" src="../../../Assets/Resources/Music/CatHomeMenu.wav"></audio></article>'
html+='<script>document.querySelectorAll("audio").forEach(a=>a.onplay=()=>document.querySelectorAll("audio").forEach(b=>{if(a!==b)b.pause()}))</script></html>'
(QA/'index.html').write_text(html,encoding='utf-8')
report={'wav_files_verified':len(rows),'all_manifest_hashes_match':True,'no_clipping':True,
        'title_changed_only_in_two_phrase_release_regions':True,'title_frames_changed':int(changed.sum()),
        'title_samples':len(after),'new_care_and_paw_files':14}
(QA/'assets-verified.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print(json.dumps(report))
