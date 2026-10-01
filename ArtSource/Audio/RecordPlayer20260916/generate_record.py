"""Original Cat Home record miniature: deterministic synthesis, no recordings/soundfonts.
Render beside this source; the production integration copies Record.wav and its provenance.
"""
from pathlib import Path
import hashlib, json, wave
import numpy as np

SR=48000
BPM=100
BEAT=60/BPM
SECONDS=16*4*BEAT
N=round(SECONDS*SR)
rng=np.random.default_rng(16092026)
mix=np.zeros(N)
events=[]

def add(note, beat, held, gain, voice):
    seconds=(held+.35)*BEAT
    t=np.arange(round(seconds*SR))/SR
    f=440*2**((note-69)/12)
    # Small circular wow on each note, not sample detuning of an existing song.
    p=2*np.pi*(f*t+.0012*f/(2*np.pi*.75)*np.sin(2*np.pi*.75*t))
    if voice=='reed':
        s=np.sin(p)+.24*np.sin(3*p)+.055*np.sin(5*p)
        env=(1-np.exp(-t/.026))*np.minimum(1,np.exp(-(t-held*BEAT)/.075))
        env*=.85+.15*np.exp(-t/.15)
    elif voice=='keys':
        s=np.sin(p)+.30*np.sin(2*p+.003*np.sin(p))+.12*np.sin(3*p)
        env=(1-np.exp(-t/.003))*np.exp(-t/.21)
    else:
        s=np.sin(p)+.22*np.sin(2*p)
        env=(1-np.exp(-t/.008))*np.exp(-t/.22)
    s*=env*gain
    # Circular placement preserves the last bar's natural tail into bar one.
    start=round(beat*BEAT*SR)%N
    ix=(start+np.arange(len(s)))%N
    np.add.at(mix,ix,s)
    events.append(dict(note=note,beat=beat,held=held,voice=voice))

chords=[(48,[64,67,71]),(48,[64,67,69]),(45,[61,64,67]),(45,[61,64,67]),
        (50,[65,69,72]),(50,[65,69,72]),(43,[62,65,69]),(43,[62,65,68]),
        (48,[64,67,71]),(52,[62,67,71]),(53,[64,69,72]),(54,[63,69,72]),
        (48,[64,67,69]),(45,[61,64,67]),(50,[65,69,72]),(43,[62,65,71])]
# Authored small phrases, deliberately not an arrangement of a named melody.
phrases=[[(.0,76,.6),(.75,79,.35),(1.5,74,.45),(2.5,76,.3),(3.25,72,.4)],
         [(.25,74,.35),(1,76,.5),(2,79,.5),(3,81,.6)],
         [(0,76,.4),(.75,73,.5),(1.75,76,.3),(2.5,79,.65)],
         [(.5,78,.45),(1.5,76,.45),(2.5,73,.35),(3.25,69,.45)],
         [(0,77,.6),(1,81,.35),(1.75,79,.45),(2.75,77,.65)],
         [(.25,74,.45),(1,77,.45),(2,76,.3),(2.75,74,.6)],
         [(0,71,.4),(.75,74,.4),(1.5,77,.5),(2.5,76,.35),(3.25,74,.4)],
         [(.25,71,.45),(1.25,68,.5),(2.5,67,.65)]]
for bar,(bass,chord) in enumerate(chords):
    base=bar*4
    for beat,note in [(0,bass),(2,bass+7)]: add(note,base+beat,.65,.19,'bass')
    for beat in [1,3]:
        for note in chord:add(note,base+beat,.20,.075,'keys')
    notes=phrases[bar%8]
    for onset,note,duration in notes:
        if bar>=8 and bar<14: note += 12 if bar%3==0 else 0
        if bar==15: note={71:72,68:71,67:74}.get(note,note)
        add(note,base+onset,duration,.12,'reed')
    # Soft brushes with swung offbeat accents; entirely generated noise.
    for onset in [0, .66,1,1.66,2,2.66,3,3.66]:
        count=round(.075*SR);t=np.arange(count)/SR
        noise=rng.normal(0,1,count)
        s=np.diff(noise,prepend=noise[0])*np.exp(-t/.017)*.004
        ix=(round((base+onset)*BEAT*SR)+np.arange(count))%N
        np.add.at(mix,ix,s)

# Periodic band limitation and very quiet vinyl texture support the gramophone colour.
texture=rng.normal(0,1,N)*.00065
mix=np.tanh((mix+texture)*1.1)
freq=np.fft.rfftfreq(N,1/SR)
band=(1-np.exp(-(freq/120)**4))*np.exp(-(freq/5100)**4)
mix=np.fft.irfft(np.fft.rfft(mix)*band,n=N)
mix-=mix.mean();mix*=.62/max(abs(mix).max(),1e-8)
pcm=np.rint(mix*32767).astype('<i2')
out=Path(__file__).resolve().parent
path=out/'Record.wav'
with wave.open(str(path),'wb') as f:
    f.setnchannels(1);f.setsampwidth(2);f.setframerate(SR);f.writeframes(pcm.tobytes())
info=dict(title='Sunny Little Record',bpm=BPM,seconds=SECONDS,sample_rate=SR,channels=1,
          notes=len(events),peak_dbfs=float(20*np.log10(abs(mix).max())),
          rms_dbfs=float(20*np.log10(np.sqrt(np.mean(mix*mix)))),
          clipped_samples=int(np.sum(abs(pcm.astype(np.int32))>=32767)),
          loop_boundary_step=float(abs(mix[0]-mix[-1])),
          sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
          provenance='Original authored note sequence and mathematical synthesis created for Cat Home. No third-party recording, sample library, SoundFont, named-song arrangement or external generation service.',
          events=events)
(out/'record-manifest.json').write_text(json.dumps(info,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in info.items() if k!='events'},indent=2))
