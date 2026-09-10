"""Keep original SFX sources outside Unity; prepare small mono, click-free care loops."""
from pathlib import Path
import wave,json,shutil
import numpy as np

root=Path(__file__).resolve().parents[2]
assets=root/'Assets/Resources/CatAudio'
source=Path(__file__).resolve().parent/'GeneratedOriginals'
source.mkdir(parents=True,exist_ok=True)
report={}
for name in ['CatMeow','CatPurr','CatEat','CatDrink']:
    original=source/(name+'.wav')
    if not original.exists():shutil.copy2(assets/(name+'.wav'),original)
    with wave.open(str(original),'rb') as wav:
        rate=wav.getframerate();channels=wav.getnchannels();width=wav.getsampwidth()
        if width!=2:raise ValueError('Expected the generated PCM16 master')
        samples=np.frombuffer(wav.readframes(wav.getnframes()),dtype='<i2').astype(np.float64).reshape(-1,channels).mean(axis=1)/32768
    # The source remains untouched and every rerun starts from that master.
    samples=np.interp(np.arange(int(len(samples)*24000/rate))*rate/24000,np.arange(len(samples)),samples)
    if name!='CatMeow':
        n=int(24000*(.24 if name=='CatPurr' else .18));t=np.linspace(0,1,n,endpoint=False)
        join=samples[-n:]*(1-t)+samples[:n]*t
        samples=np.concatenate([samples[n:-n],join])
    else:
        n=int(24000*.035);samples[:n]*=np.linspace(0,1,n);samples[-n:]*=np.linspace(1,0,n)
    peak=float(np.max(np.abs(samples)))
    if peak>.90:samples*=.90/peak
    result=(np.clip(samples,-1,1)*32767).astype('<i2')
    with wave.open(str(assets/(name+'.wav')),'wb') as wav:
        wav.setnchannels(1);wav.setsampwidth(2);wav.setframerate(24000);wav.writeframes(result.tobytes())
    report[name]={'seconds':len(samples)/24000,'sampleRate':24000,'channels':1,'peak':float(np.max(np.abs(samples))),
        'loopBoundaryDelta':float(abs(samples[-1]-samples[0])) if name!='CatMeow' else None,
        'bytes':(assets/(name+'.wav')).stat().st_size}
(Path(__file__).parent/'prepared_audio_metrics.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))
