"""Reproducible cat-care foley: CC0 recordings for care, unpitched padded paws."""
from pathlib import Path
import argparse, hashlib, json, subprocess, wave
import numpy as np

ROOT = Path(__file__).resolve().parent
PROJECT = ROOT.parents[2]
QA = PROJECT / 'Docs/QA/AUDIO_REFINEMENT_2026-09-15'
OUT = PROJECT / 'Assets/Resources/GameAudio/Sfx'
SR = 48000
FFMPEG = str(Path.home() / 'AppData/Local/Microsoft/WinGet/Packages/Gyan.FFmpeg.Shared_Microsoft.Winget.Source_8wekyb3d8bbwe/ffmpeg-9.0-full_build-shared/bin/ffmpeg.exe')

def decode(name):
    data = subprocess.run([FFMPEG, '-v', 'error', '-i', str(ROOT/(name+'.mp3')),
        '-ac', '1', '-ar', str(SR), '-f', 'f32le', 'pipe:1'], check=True, capture_output=True).stdout
    return np.frombuffer(data, dtype='<f4').astype(float)

def wav_read(path):
    with wave.open(str(path)) as f:
        assert f.getsampwidth() == 2
        return np.frombuffer(f.readframes(f.getnframes()), '<i2').reshape(-1, f.getnchannels()).astype(float)/32768, f.getframerate()

def wav_write(path, data):
    data = np.asarray(data); assert np.isfinite(data).all() and np.max(np.abs(data)) < .98
    pcm = np.rint(data*32767).astype('<i2')
    with wave.open(str(path), 'wb') as f:
        f.setnchannels(1 if data.ndim == 1 else data.shape[1]); f.setsampwidth(2); f.setframerate(SR); f.writeframes(pcm.tobytes())

def band(signal, lo, hi):
    f = np.fft.rfftfreq(len(signal), 1/SR)
    gain = (1-np.exp(-(f/lo)**4))*np.exp(-(f/hi)**6)
    return np.fft.irfft(np.fft.rfft(signal)*gain, n=len(signal))

def db(x): return float(20*np.log10(max(float(x), 1e-12)))

def edge(signal, attack=.008, release=.06):
    s=signal.copy(); a=round(attack*SR); r=round(release*SR)
    s[:a]*=np.sin(np.linspace(0,np.pi/2,a))**2
    s[-r:]*=np.cos(np.linspace(0,np.pi/2,r))**2
    return s

def denoise(s, reference):
    # Fixed, conservative spectral subtraction; no adaptive pumping or pitch shift.
    n=1024; hop=256; win=np.sqrt(np.hanning(n)); floor=[]
    for at in range(0,len(reference)-n,hop):
        floor.append(np.abs(np.fft.rfft(reference[at:at+n]*win)))
    floor=np.median(floor,axis=0)
    padded=np.pad(s,(n,n)); output=np.zeros_like(padded); weight=np.zeros_like(padded)
    for at in range(0,len(padded)-n,hop):
        spec=np.fft.rfft(padded[at:at+n]*win); mag=np.abs(spec)
        gain=np.maximum(.30,1-.8*floor/(mag+1e-9))
        output[at:at+n]+=np.fft.irfft(spec*gain,n=n)*win
        weight[at:at+n]+=win*win
    return (output/np.maximum(weight,1e-9))[n:n+len(s)]

def record(name,s,description,source=None):
    # SFX retain headroom; short fade on naturally quiet endpoints avoids loop clicks.
    s-=s.mean(); s=edge(s); s*=.72/max(np.max(np.abs(s)),1e-9)
    path=OUT/(name+'.wav'); wav_write(path,s)
    row={'name':name,'folder':'Sfx','seconds':len(s)/SR,'channels':1,
        'loop':name in ('Eat_1','Drink_1'),'peak_dbfs':round(db(np.max(np.abs(s))),3),
        'rms_dbfs':round(db(np.sqrt(np.mean(s*s))),3),'clipped_samples':0,
        'boundary_step':float(abs(s[0]-s[-1])),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),
        'description':description,'revision':'2026-09-15'}
    if source:row['source']=source
    return row

def care():
    rows=[]
    for name,src,start,end,noise_range in [('Eat_1','eat',10.2,24.5,(38.4,40.3)),
            ('Drink_1','drink_alt',18.7,24.7,(28.0,33.0))]:
        # Keep recorded chewing / tongue-contact timing; remove only room rumble and hiss.
        raw=decode(src); clean=band(raw,230 if src=='eat' else 330,7600)
        part=clean[round(start*SR):round(end*SR)]
        reference=clean[round(noise_range[0]*SR):round(noise_range[1]*SR)]
        part=denoise(part,reference)
        # Smooth compression of isolated crunchy peaks, with no block-based gain changes.
        knee=np.quantile(np.abs(part),.997)
        part=np.tanh(part/max(knee,1e-8))*knee
        # A short breathing space is deliberate, inside a longer natural phrase.
        part=np.concatenate([edge(part,.018,.07),np.zeros(round(.10*SR))])
        source=json.loads((ROOT/(src+'-source.json')).read_text())
        source.update({'trim_seconds':[start,end],'processing':'mono 48k; rumble/hiss reduction; soft peak compression; quiet loop seam; original timing/pitch'})
        rows.append(record(name,part,'Real cat dry-kibble crunches.' if src=='eat' else 'Real cat tongue lapping with short wet contacts.',source))
    update_manifest(rows)
    print(json.dumps(rows))

def paws():
    rows=[]; rng=np.random.default_rng(15092026)
    for name,lo,hi,duration in [('PawWood',95,2200,.105),('PawTile',140,3900,.09),
            ('PawGrass',180,2900,.14),('PawFabric',140,1700,.12)]:
        for i in range(3):
            t=np.arange(round(duration*SR))/SR
            # Broad damped noise bodies, no sine oscillators / tuned drum fundamental.
            grain=band(rng.normal(size=len(t)),lo,hi)
            pad=band(rng.normal(size=len(t)),65,680)
            env=(1-np.exp(-t/(.004+i*.0004)))**2*np.exp(-t/(.013+i*.001))
            s=grain*env+.38*pad*(1-np.exp(-t/.006))**2*np.exp(-t/.022)
            if name in ('PawGrass','PawFabric'):
                s+=grain*.13*np.sin(np.pi*t/duration)**2
            rows.append(record(name+'_'+str(i+1),s,'Unpitched, soft padded paw contact; independently authored take; '+name))
    update_manifest(rows);print(json.dumps(rows))

def update_manifest(rows):
    path=PROJECT/'Assets/Resources/GameAudio/manifest.json';data=json.loads(path.read_text())
    updated={r['name']:r for r in rows}
    data['assets']=[updated.get(r['name'],r) for r in data['assets']]
    data['care_foley_revision']='2026-09-15'
    path.write_text(json.dumps(data,indent=2),encoding='utf-8')
    (QA/('care-assets.json' if rows[0]['name']=='Eat_1' else 'paw-assets.json')).write_text(json.dumps(rows,indent=2),encoding='utf-8')

def fill_title_tails(mix):
    """A quiet middle-C release connects C to F in the two matching phrase gaps."""
    mix=mix.copy(); t=np.arange(round(.78*SR))/SR
    note=(np.sin(2*np.pi*261.625565*t)+.16*np.sin(4*np.pi*261.625565*t))
    note*=.045*(1-np.exp(-t/.018))**2*np.exp(-t/.25)
    note=edge(note,.005,.14)
    for at in [11.55,41.55]:
        start=round(at*SR);mix[start:start+len(note)]+=note[:,None]*np.array([.71,.70])
    return mix

def title():
    path=PROJECT/'Assets/Resources/Music/CatHomeMenu.wav'
    backup=QA/'title-before.wav'
    if not backup.exists():
        assert hashlib.sha256(path.read_bytes()).hexdigest()=='63f264b0eea4fde5c3f29384b4969c3d9c073682e9e0fa706bf4240955e43434'
        backup.write_bytes(path.read_bytes())
    with wave.open(str(backup)) as f:
        original=np.frombuffer(f.readframes(f.getnframes()),'<i2').reshape(-1,2).astype(float)/32767
    fixed=fill_title_tails(original);wav_write(path,fixed)
    rows=[]
    for signal in [original,fixed]:
        rms=np.sqrt(np.mean(signal[:len(signal)//960*960].reshape(-1,960,2)**2,axis=(1,2)))
        rows.append({'silent_20ms_frames':np.where(rms<10**(-65/20))[0].tolist(),
            'peak_db':db(np.max(np.abs(signal))),'boundary_step':float(np.max(np.abs(signal[0]-signal[-1])))})
    result={'before':rows[0],'after':rows[1],'seconds':60,'changed_regions_seconds':[[11.55,12.33],[41.55,42.33]],
        'melody_tempo_key_unchanged':True,'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}
    (QA/'title-repair.json').write_text(json.dumps(result,indent=2),encoding='utf-8');print(json.dumps(result))

def analyze():
    report = {}
    for name in ['eat', 'drink', 'drink_alt']:
        s = decode(name); wav_write(QA/(name+'-original.wav'), s*.95/max(1, np.max(np.abs(s))))
        rows = []
        for k in range(len(s)//SR):
            frame = s[k*SR:(k+1)*SR]; clean = band(frame, 500, 7500)
            rows.append([k, round(db(np.sqrt(np.mean(frame**2))),1), round(db(np.sqrt(np.mean(clean**2))),1), round(db(np.max(np.abs(frame))),1)])
        report[name] = {'seconds':len(s)/SR, 'one_second_time_rms_highband_peak_db':rows}
    menu, sr = wav_read(PROJECT/'Assets/Resources/Music/CatHomeMenu.wav')
    mono = np.mean(menu**2, axis=1)
    frames = mono[:len(mono)//960*960].reshape(-1,960)
    rms = np.sqrt(np.mean(frames,axis=1))
    quiet = np.where(rms < 10**(-65/20))[0]
    report['title'] = {'seconds':len(menu)/sr, 'peak_db':db(np.max(np.abs(menu))),
        'boundary_step':float(np.max(np.abs(menu[0]-menu[-1]))),
        'quiet_20ms_frames':quiet.tolist(), 'max_adjacent_sample_step':float(np.max(np.abs(np.diff(menu,axis=0))))}
    (QA/'source-analysis.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(json.dumps(report))

if __name__ == '__main__':
    QA.mkdir(parents=True,exist_ok=True)
    parser=argparse.ArgumentParser();parser.add_argument('step',choices=['analyze','care','paws','title'],default='analyze',nargs='?')
    globals()[parser.parse_args().step]()
