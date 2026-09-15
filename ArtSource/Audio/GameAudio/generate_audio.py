"""Cat Home original audio. Python + numpy only; no recordings or external service.
Run from any directory. Writes authored production WAVs and an audit manifest.
The previously accepted title master is never changed.
"""
from pathlib import Path
import ast, hashlib, json, math, wave
import numpy as np

PROJECT = Path(__file__).resolve().parents[3]
OUT = PROJECT/'Assets/Resources/GameAudio'
QA = PROJECT/'Docs/QA/FULL_AUDIO_2026-09-14'
SR=48000
rng=np.random.default_rng(9142026)
manifest=[]
# Preserve the later recorded care / padded-paw masters when rebuilding other cues.
# Their dedicated, reproducible preparation lives in ../CatCare20260915/prepare_audio.py.
existing_manifest=OUT/'manifest.json'
overrides={}
if existing_manifest.exists():
    overrides={row['name']:row for row in json.loads(existing_manifest.read_text(encoding='utf-8'))['assets']
               if row.get('revision')=='2026-09-15'}
# Reuse only the accepted theme's instrument definitions, without executing its render.
old=ast.parse((PROJECT/'ArtSource/Audio/CatHomeMenu/generate_menu_music.py').read_text(encoding='utf-8'))
defs=ast.Module(body=[n for n in old.body if isinstance(n,ast.FunctionDef) and n.name in ('instrument','room')],type_ignores=[])
exec(compile(defs,'accepted_theme_instruments','exec'),globals())

def fade(s, attack=.005, release=.025):
    s=s.copy();a=min(len(s)//2,round(attack*SR));r=min(len(s)//2,round(release*SR))
    if a:s[:a]*=np.linspace(0,1,a)**2
    if r:s[-r:]*=np.linspace(1,0,r)**2
    return s

def noise(sec, low=120, high=6500):
    n=round(sec*SR);f=np.fft.rfftfreq(n,1/SR)
    gain=(1-np.exp(-(f/max(1,low))**4))*np.exp(-(f/high)**4)
    return np.fft.irfft(np.fft.rfft(rng.normal(0,1,n))*gain,n=n)

def place(dst, src, at, gain=1):
    start=round(at*SR);end=min(len(dst),start+len(src))
    if end>start:dst[start:end]+=src[:end-start]*gain

def chime(notes, spacing=.09, held=.12):
    s=np.zeros(round((len(notes)*spacing+held+.65)*SR))
    for i,n in enumerate(notes):place(s,instrument('mallet',n,held,.9),i*spacing)
    return s

def thud(sec=.16, hz=170, grain=.3):
    t=np.arange(round(sec*SR))/SR
    body=np.sin(2*np.pi*(hz*t+hz*.1*(1-np.exp(-t*40))/40))*np.exp(-t*28)
    return fade(body+noise(sec,100,3500)*np.exp(-t*60)*grain,.003,.025)

def rustle(sec=.38, material='cloth'):
    t=np.arange(round(sec*SR))/SR
    band={'cloth':(240,3300),'sand':(900,6800),'paper':(500,8200),'rope':(240,5000),'wood':(170,3600),'soil':(180,4700),'water':(400,7000)}[material]
    env=np.sin(np.pi*np.clip(t/sec,0,1))**1.4
    flutter=.6+.4*np.sin(2*np.pi*(14 if material=='rope' else 21)*t)**2
    s=noise(sec,*band)*env*flutter
    if material in ('sand','soil','paper'):
        for _ in range(14):place(s,thud(.025,rng.uniform(300,900),.3),float(rng.uniform(.015,sec-.04)),.12)
    return fade(s,.006,.025)

def drops(sec=1.8, count=11):
    s=np.zeros(round(sec*SR))
    for j in range(count):
        at=(j+.3)*sec/(count+1);t=np.arange(round(.13*SR))/SR
        f=rng.uniform(850,1700);phase=2*np.pi*(f*t+f*.35*.028*(1-np.exp(-t/.028)))
        d=np.sin(phase)*np.exp(-t/.028)*(1-np.exp(-t/.002))
        place(s,d,at,.34+rng.uniform(0,.1))
    return fade(s+noise(sec,800,6600)*.028,.02,.06)

def meow(variant):
    sec=[.62,.82,.52][variant];t=np.arange(round(sec*SR))/SR;u=t/sec
    freq=np.interp(u,[0,.12,.36,.7,1],[520,650,710,510,400])*(1+variant*.035)
    phase=2*np.pi*np.cumsum(freq)/SR
    s=np.zeros_like(t)
    # Nasal opening, round open vowel, then closed hum; no human voice sample.
    f1=np.interp(u,[0,.3,.7,1],[1500,950,1050,650]);f2=np.interp(u,[0,.3,.7,1],[2600,1900,1750,1200])
    for h in range(1,17):
        form=.23*np.exp(-h*.16)+np.exp(-((freq*h-f1)/430)**2)+.35*np.exp(-((freq*h-f2)/550)**2)
        s+=np.sin(phase*h)*form/h**.7
    env=np.sin(np.pi*u)**1.2*(.78+.22*np.sin(2*np.pi*u))
    return fade(s*env,.012,.06)

def purr():
    sec=4;t=np.arange(round(sec*SR))/SR
    grain=noise(sec,30,1600)
    pulse=(.45+.55*np.sin(2*np.pi*27*t))**2
    breath=.74+.26*np.cos(2*np.pi*t/.8)
    s=(grain*.36+np.sin(2*np.pi*108*t)*.20+np.sin(2*np.pi*216*t)*.055)*pulse*breath
    return s

def write(name,s,folder='Sfx',loop=False,peak=.68,description=''):
    if name in overrides:
        path=OUT/folder/(name+'.wav')
        assert hashlib.sha256(path.read_bytes()).hexdigest()==overrides[name]['sha256'], 'Regenerate revised care foley with prepare_audio.py first'
        manifest.append(overrides[name])
        with wave.open(str(path)) as f:
            return np.frombuffer(f.readframes(f.getnframes()),'<i2').astype(float)/32767
    s=np.asarray(s,dtype=float);s-=np.mean(s,axis=0)
    if not loop:s=fade(s) if s.ndim==1 else np.stack([fade(s[:,i]) for i in range(s.shape[1])],axis=1)
    if loop:
        # Circular spectral filtering preserves a smooth boundary.
        f=np.fft.rfftfreq(len(s),1/SR);g=(1-np.exp(-(f/28)**4))*np.exp(-(f/11500)**6)
        s=np.fft.irfft(np.fft.rfft(s,axis=0)*(g if s.ndim==1 else g[:,None]),n=len(s),axis=0)
    s*=peak/max(np.max(np.abs(s)),1e-9)
    pcm=np.rint(s*32767).astype('<i2');path=OUT/folder/(name+'.wav');path.parent.mkdir(parents=True,exist_ok=True)
    with wave.open(str(path),'wb') as f:
        f.setnchannels(1 if s.ndim==1 else s.shape[1]);f.setsampwidth(2);f.setframerate(SR);f.writeframes(pcm.tobytes())
    record={'name':name,'folder':folder,'seconds':len(s)/SR,'channels':1 if s.ndim==1 else s.shape[1],
            'loop':loop,'peak_dbfs':round(20*np.log10(np.max(np.abs(s))),3),'rms_dbfs':round(20*np.log10(np.sqrt(np.mean(s*s))),3),
            'clipped_samples':int(np.sum(np.abs(pcm.astype(float))>=32767)),
            'boundary_step':float(np.max(np.abs(s[0]-s[-1]))),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'description':description}
    manifest.append(record)
    return s

def prop_land(take):
    """Dry solid-object thock; fixed local seed leaves every existing cue unchanged."""
    t=np.arange(round(.22*SR))/SR
    local=np.random.default_rng(9152026+take)
    f=np.fft.rfftfreq(len(t),1/SR)
    grain=np.fft.irfft(np.fft.rfft(local.normal(size=len(t)))*(1-np.exp(-(f/140)**4))*np.exp(-(f/3700)**4),n=len(t))
    grain/=max(np.sqrt(np.mean(grain*grain)),1e-9)
    hz=[166,176,157][take]
    body=.80*np.sin(2*np.pi*hz*t)*np.exp(-t/.032)
    body+=.28*np.sin(2*np.pi*hz*1.89*t)*np.exp(-t/.021)
    knock=.23*np.sin(2*np.pi*hz*3.65*t)*np.exp(-t/.012)
    return fade(body+knock+grain*.32*np.exp(-t/.009),.0015,.025)

def effects():
    for take in range(3):
        write('PropLand_'+str(take+1),prop_land(take),peak=.72,description='Dry solid object landing on the floor: short low thock and a muted tap; no bell or ball bounce.')
    chords={'UIClick':([76],.035,.025),'UIToggle':([72,79],.045,.04),'UIOpen':([67,72],.06,.06),
            'UIClose':([72,67],.05,.04),'UIError':([60,60],.12,.04),'Purchase':([67,72,76],.075,.10),
            'Coin':([84],.02,.04),'Diamond':([79,84,88],.055,.12),'Success':([72,76,79],.10,.12),
            'LevelUp':([60,64,67,72,76,79,84],.11,.24),'RoomChange':([67,72],.10,.06),
            'NewGame':([60,67,72,76,79,84],.14,.20),'Countdown':([72],.03,.055),
            'Start':([72,76,79,84],.065,.14),'Shield':([72,79,84],.03,.17),
            'PowerUp':([67,72,76,79,84],.05,.15),'Result':([60,64,67,72,76,79,72],.11,.20),
            'Catch':([79,84],.035,.05),'Combo':([84,88,91],.045,.07)}
    for name,(notes,spacing,held) in chords.items():write(name+'_1',chime(notes,spacing,held),description='Short tuned wooden UI/reward motif in C major.')
    for name,hz,grain in [('PawWood',180,.32),('PawTile',290,.38),('PawGrass',130,.80),('PawFabric',110,.50),
                          ('LandWood',125,.40),('LandTile',210,.45),('LandSoft',85,.55),('BallTap',390,.14),
                          ('FruitLand',100,.38),('BookLand',125,.72),('WoodTap',290,.22),('Hit',85,.65),('Miss',150,.6)]:
        for i in range(3):write(name+'_'+str(i+1),thud(.13 if name.startswith('Paw') else .26,hz*(1+(i-1)*.045),grain),description='Rounded physical impact; three authored takes.')
    for name,mat,sec in [('ScratchWood','wood',.24),('ScratchRope','rope',.30),('DigSand','sand',.34),('DigSoil','soil',.38),
                         ('Cloth','cloth',.36),('PaperTear','paper',.28),('PaperRoll','paper',.65),('Ribbon','cloth',.22),
                         ('Groom','cloth',.24),('Sniff','cloth',.14),('Slide','cloth',.36),('Leaves','paper',.55)]:
        for i in range(2):write(name+'_'+str(i+1),rustle(sec,mat),description='Filtered granular material texture.')
    for name,hz,decay in [('GlassTap',1900,.11),('GlassLand',1450,.25),('CeramicTap',1250,.065),('CeramicLand',980,.17),('Bell',1568,.22),('MetalRattle',920,.10)]:
        for i in range(2):
            sec=.55;t=np.arange(round(sec*SR))/SR;s=np.zeros_like(t)
            for ratio,amp in [(1,1),(2.71,.18),(4.08,.08)]:s+=amp*np.sin(2*np.pi*hz*(1+(i-.5)*.025)*ratio*t)*np.exp(-t/decay/(ratio**.5))
            s+=noise(sec,350,5200)*np.exp(-t/.018)*.3
            write(name+'_'+str(i+1),fade(s,.002,.025),description='Compact material resonance, kept below music.')
    for name in ['Jump','ToySqueak','Bird','Creak']:
        for i in range(2):
            sec={'Jump':.17,'ToySqueak':.22,'Bird':.35,'Creak':.48}[name];t=np.arange(round(sec*SR))/SR
            lo,hi={'Jump':(340,570),'ToySqueak':(1000,1700),'Bird':(1900,3100),'Creak':(180,280)}[name]
            freq=lo+(hi-lo)*np.sin(np.pi*t/sec)
            s=np.sin(2*np.pi*np.cumsum(freq)/SR)*np.sin(np.pi*t/sec)**2
            if name=='Creak':s+=noise(sec,200,2200)*.22
            write(name+'_'+str(i+1),s,description='Brief stylized gesture.')
    write('Shake_1',rustle(.65,'water')+drops(.65,13)*.36)
    write('WaterDrop_1',drops(.34,2))
    write('Feeder_1',rustle(.65,'sand')+thud(.65,230,.3)*.35)
    write('BallRoll_1',rustle(.7,'wood')*.7+noise(.7,80,1100)*.10)
    write('WheelRoll_1',rustle(.7,'rope')*.5+noise(.7,50,1100)*.2)
    write('Record_1',noise(.55,400,5200)*np.sin(np.pi*np.arange(round(.55*SR))/(.55*SR))**2)
    for i in range(3):write('Meow_'+str(i+1),meow(i),description='Original formant-synthesized playful cat voice.')
    write('Purr_1',purr(),loop=True,description='Four-second circular breath/purr synthesis.')
    eat=np.zeros(2*SR)
    for j in range(7):place(eat,rustle(.10,'sand'),.08+j*.26,.7 if j%2 else 1)
    write('Eat_1',eat,loop=True,description='Quiet rhythmic crunching.')
    write('Drink_1',drops(2,10),loop=True,description='Quiet lapping and droplets.')
    shower=noise(4,550,6800);tt=np.arange(len(shower))/SR
    shower*=.8+.12*np.sin(2*np.pi*tt)+.08*np.cos(2*np.pi*2*tt)
    write('Shower_1',shower,loop=True,description='Soft continuous rinse noise.')
    wind=noise(8,100,2200);tt=np.arange(len(wind))/SR
    wind*=.55+.18*np.sin(2*np.pi*tt/8)+.18*np.cos(2*np.pi*tt/4)
    write('GardenAir_1',wind,loop=True,description='Very quiet outdoor air; no automatic bird chorus.')

def music(name,bpm,bars,mode):
    beat=60/bpm;sec=bars*4*beat;n=round(sec*SR);mix=np.zeros((n+2*SR,2));notes=[]
    # Plain diatonic triads. Every sustained melody and bass note belongs to its bar.
    progression=[(48,[60,64,67]),(48,[60,64,67]),(41,[60,65,69]),(43,[59,62,67]),
                 (45,[60,64,69]),(41,[60,65,69]),(43,[59,62,67]),(48,[60,64,67])]
    def add(kind,pitch,at,length,vel,pan=0):
        sig=instrument(kind,pitch,length*beat,vel);start=round((at*beat+.04)*SR);end=min(len(mix),start+len(sig));ang=(pan+1)*np.pi/4
        mix[start:end,0]+=sig[:end-start]*np.cos(ang);mix[start:end,1]+=sig[:end-start]*np.sin(ang)
        notes.append({'kind':kind,'midi':pitch,'beat':at,'length_beats':length})
    for b in range(bars):
        root,chord=progression[b%8];off=b*4;rest=mode=='rest';fast=mode=='runner';outdoor=mode=='outdoor'
        add('bass',root,off,.55,.30 if rest else .43)
        if not rest and b%4!=3:add('bass',chord[2]-24,off+2,.36,.30)
        timings=[1.25,3.0] if fast else [1.5,3.25] if outdoor else [1.5]
        if rest:timings=[2]
        for at in timings:
            for i,p in enumerate(chord[:2]):add('pluck',p,off+at+i*.028,.35,.20 if rest else .29,-.22+i*.09)
        # Spacious phrases in the home, clear playful answers in each following four bars.
        if b%8 in ([2,3,6] if rest else [3,7]) or mode=='home' and b%8==5:continue
        pattern=[0,1,2,1] if b%4<2 else [2,1,0,1]
        if b>=16:pattern=pattern[::-1]
        times=[0,.75,2,2.75] if fast else [.25,1.25,2.75] if mode=='catch' else [.25,2,3] if outdoor else [.25,2.5]
        if rest:times=[.5,2.5]
        for j,at in enumerate(times):
            pitch=chord[pattern[j%4]]+(12 if not rest else 0)
            kind='mallet' if b%4<2 or fast else 'reed'
            add(kind,pitch,off+at,.32 if fast else .45 if not rest else .75,.52 if fast else .40 if not rest else .31,.10 if kind=='reed' else -.06)
        if fast or outdoor:
            for at in ([1,3] if fast else [3]):add('wood',60,off+at,.12,.40,.18)
    mix=room(mix);mix[:len(mix)-n]+=mix[n:];mix=mix[:n]
    write(name,mix,'Music',True,.72,description=f'{bpm} BPM, {bars} bars, original C-major {mode} theme; varied second half.')
    (QA/(name+'-score.json')).write_text(json.dumps({'bpm':bpm,'bars':bars,'notes':notes},indent=2),encoding='utf-8')

if __name__=='__main__':
    QA.mkdir(parents=True,exist_ok=True)
    effects()
    for args in [('Home',96,32,'home'),('Outdoor',100,32,'outdoor'),('Rest',88,32,'rest'),('Runner',128,32,'runner'),('Catch',112,32,'catch')]:
        music(*args);print('Rendered',args[0],flush=True)
    write('Fountain_1',drops(4,27)+noise(4,300,4600)*.06,loop=True,description='Gentle running water during a sip.')
    fire=noise(4,60,2300)*.05
    for j in range(15):place(fire,thud(.04,rng.uniform(120,500),.6),float(rng.uniform(0,3.9)),.09)
    write('FireCrackle_1',fire,loop=True,description='Subtle warm crackles beside an active stove or fire pit.')
    tv=np.zeros(4*SR)
    for j,pitch in enumerate([72,79,76,72,67,72]):place(tv,instrument('mallet',pitch,.055,.35),j*.59)
    write('Television_1',tv,loop=True,description='Quiet original cartoon-like TV phrase while watching.')
    (OUT/'manifest.json').write_text(json.dumps({'sample_rate':SR,'seed':9142026,'assets':manifest},indent=2),encoding='utf-8')
    (QA/'audio-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
    assert all(x['clipped_samples']==0 for x in manifest)
    print(json.dumps({'assets':len(manifest),'music':5,'effects':len(manifest)-5,'seconds':sum(x['seconds'] for x in manifest),'clipping':0}),flush=True)
