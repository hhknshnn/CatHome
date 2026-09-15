"""Refined low-poly Cat Home character theme, with wholly local synthetic timbres."""
from pathlib import Path
import hashlib
import json
import math
import struct
import wave
import numpy as np

OUT = Path(__file__).resolve().parents[3] / 'Docs/QA/MENU_MUSIC_2026-09-14'
OUT.mkdir(parents=True, exist_ok=True)
SR, BPM, DURATION = 48000, 120, 60.0
EIGHTH = 30 / BPM
LOOP_N = round(DURATION * SR)
N = LOOP_N + 2 * SR
rng = np.random.default_rng(140930)
KINDS = ['pluck', 'reed', 'mallet', 'bass', 'wood']
tracks = {kind: np.zeros((N, 2)) for kind in KINDS}
score = []


def instrument(kind, midi, held, velocity):
    f = 440 * 2 ** ((midi - 69) / 12)
    tail = 0.60 if kind in ['pluck', 'mallet', 'bass'] else 0.30
    t = np.arange(round((held + tail) * SR)) / SR
    sig = np.zeros_like(t)
    if kind == 'pluck':
        # Rounded short plucks, with a softer transient and a compact wooden body.
        for h in range(1, 15):
            hf = f * h
            if hf > 17000:
                break
            body = 0.85 + 0.27 * math.exp(-((hf - 850) / 650) ** 2)
            amp = math.sin(np.pi * h * 0.23) * math.exp(-h * 0.20) / h ** 0.94
            phase = 2 * np.pi * hf * t
            strings = np.sin(phase)
            sig += amp * body * strings * np.exp(-t / (0.68 / (1 + 0.18 * h)))
        sig *= (1 - np.exp(-t / 0.006)) ** 2
        sig *= np.exp(-np.maximum(t - held, 0) / 0.16)
        for mode, strength in [(f*2, 0.020), (f*3, 0.013)]:
            sig += strength * np.sin(2*np.pi*mode*t) * np.exp(-t/0.018) * (1-np.exp(-t/0.002))
        sig *= 0.27 * velocity
    elif kind == 'mallet':
        # Soft wooden-bar modes. Upper modes die rapidly so this does not ring like a bell.
        for ratio, strength, decay in [(1, 1, .52), (2, .12, .11), (4, .11, .033), (10, .016, .013)]:
            if f*ratio < 15500:
                sig += strength*np.sin(2*np.pi*f*ratio*t)*np.exp(-t/decay)
        sig *= (1-np.exp(-t/.006))**2
        sig *= np.exp(-np.maximum(t-held, 0)/.17)
        sig *= .18*velocity
    elif kind == 'reed':
        # An intentionally stylized, rounded breath voice rather than a realistic reed.
        phase = 2*np.pi*f*t
        for h in range(1, 10):
            hf = f * h
            if hf > 15500:
                break
            weight = (1 if h % 2 else 0.26) * math.exp(-0.27*(h-1)) / h ** 1.03
            formant = 0.87 + 0.13 * math.exp(-((hf-1050)/850)**2)
            sig += weight * formant * np.sin(h*phase)
        attack = (1-np.exp(-t/0.017)) ** 2
        release = np.exp(-np.maximum(t-held, 0)/0.063)
        shape = 0.89 + 0.11*np.sin(np.pi*np.clip(t/max(held, .01), 0, 1))
        noise = rng.normal(0, 1, len(t))
        breath = np.convolve(noise, np.ones(15)/15, mode='same') * 0.005
        sig = (sig + breath) * attack * release * shape
        sig *= 0.105 * velocity
    elif kind == 'bass':
        for h, amp in [(1, .72), (2, .36), (3, .20), (4, .06), (5, .035)]:
            sig += amp*np.sin(2*np.pi*f*h*t)*np.exp(-t/(.50/(1+.17*h)))
        sig *= (1-np.exp(-t/.009)) ** 2
        sig *= np.exp(-np.maximum(t-held, 0)/.075)
        sig *= .18*velocity
    else:
        # Unpitched quiet texture cannot clash with the current harmony.
        noise = rng.normal(0, 1, len(t))
        sig = np.convolve(noise, np.ones(11)/11, mode='same')
        sig *= np.exp(-t/.019)*(1-np.exp(-t/.003))**2*.018*velocity
    end = min(len(sig), round(.015*SR))
    sig[-end:] *= np.linspace(1, 0, end)**2
    return sig


def add(kind, midi, eighth, length, velocity=.60, pan=0):
    jitter = float(rng.uniform(-.004, .004)) if eighth else 0
    start = max(0, round((.10 + eighth*EIGHTH + jitter)*SR))
    velocity *= float(rng.uniform(.97, 1.03))
    sound = instrument(kind, midi, length*EIGHTH, velocity)
    stop = min(N, start+len(sound))
    angle = (pan+1)*np.pi/4
    tracks[kind][start:stop, 0] += sound[:stop-start]*np.cos(angle)
    tracks[kind][start:stop, 1] += sound[:stop-start]*np.sin(angle)
    score.append({'instrument': kind, 'midi_note': midi, 'eighth': eighth,
                  'duration_eighths': length, 'velocity': round(velocity, 4),
                  'render_start_seconds': start/SR, 'pan': pan})


# Twenty 6/8 bars: main identity (1-8), exploration (9-12), and developed return (13-20).
# The low-poly brief is expressed with concise gestures and a small, rounded palette.
harmony = [
    (48, [55, 60, 64], 'C'), (43, [55, 59, 62], 'G'),
    (48, [55, 60, 64], 'C'), (48, [55, 60, 64], 'C'),
    (41, [57, 60, 65], 'F6'), (50, [57, 60, 66], 'D7'),
    (43, [55, 59, 65], 'G7'), (48, [55, 60, 64], 'C'),
    (41, [57, 60, 65], 'F6'), (48, [55, 60, 64], 'C'),
    (50, [57, 60, 65], 'Dm7'), (43, [55, 59, 65], 'G7'),
    (48, [55, 60, 64], 'C'), (45, [57, 60, 64], 'Am7'),
    (41, [57, 60, 65], 'F6'), (40, [55, 60, 64], 'C/E'),
    (50, [57, 60, 66], 'D7'), (43, [55, 59, 65], 'G7'),
    (48, [55, 60, 64], 'C'), (48, [55, 60, 64], 'C6'),
]
# Resolve secondary dominants to plain diatonic triads, then extend the form.
harmony = [(root, [57,62,65], 'Dm') if name=='D7' or name=='Dm7'
           else (root, [55,59,62], 'G') if name=='G7'
           else (root, [57,60,64], 'Am') if name=='Am7'
           else (root, chord, name) for root, chord, name in harmony]
harmony = harmony + harmony[:]
for bar, (root, chord, _) in enumerate(harmony):
    off = bar*6
    exploring = 8 <= bar % 20 <= 11
    add('bass', root, off, .90, .43 if exploring else .48)
    if bar % 20 not in [3, 7, 11, 19]:
        # Pick an actual chord tone; the old root+7 formula made B under C/E.
        bass_answer = min(chord, key=lambda pitch: abs(pitch-48)) - 12
        add('bass', bass_answer, off+3.0, .65, .32)
    timing = [1.5, 4.5] if bar % 4 in [0, 2] else [2.0]
    if exploring:
        timing = [2.0, 5.0]
    if bar % 20 in [7, 19]:
        timing = [0]
    for where in timing:
        for i, pitch in enumerate(chord[1:]):
            add('pluck', pitch, off+where+.026*i, .67, .29, -.20+.07*i)
    if bar % 20 in [4, 6, 12, 14, 16, 18]:
        add('wood', 65, off+4.5, .15, .30, .20)


# A recurring four-note identity, rounded mallet replies, and one exploratory phrase.
phrases = [
    ('mallet', [(0,72,.72), (1.0,76,.65), (2.5,74,.62), (4,67,.65)]),
    ('reed', [(0.5,76,.86), (2,79,.86), (3.5,76,.67), (4.5,74,.72)]),
    ('mallet', [(0,72,.62), (1.5,72,.55), (3,76,.73), (4.5,67,.75)]),
    ('pluck', [(0,64,.77), (1.5,67,.76), (3,72,1.10)]),
    ('reed', [(0,77,.85), (1.5,76,.65), (2.5,74,.68), (4,77,1.14)]),
    ('mallet', [(0,74,.72), (1.0,78,.64), (2.5,69,.65), (4,72,.85)]),
    ('reed', [(0,71,.77), (1.0,74,.72), (2.5,77,.77), (4,74,1.0)]),
    ('mallet', [(0,72,.75), (1.0,76,.70), (2.0,79,.85)]),
    # Middle section opens the register and gives the melody longer breaths.
    ('mallet', [(0,69,1.05), (1.5,72,.72), (3,77,1.60)]),
    ('reed', [(0,76,1.2), (2,74,.70), (3,72,1.60)]),
    ('mallet', [(0,74,.75), (1.5,77,.77), (3,76,.70), (4.5,74,.86)]),
    ('reed', [(0,71,1.1), (2,74,.86), (3.5,67,1.00)]),
    # Return to the original identity with a little extra movement.
    ('mallet', [(0,72,.72), (1.0,76,.65), (2.5,74,.62), (4,67,.65)]),
    ('reed', [(0.5,76,.87), (2,79,.74), (3.0,76,.72), (4.5,72,.84)]),
    ('mallet', [(0,77,.65), (1.0,76,.65), (2.5,74,.77), (4,69,.96)]),
    ('pluck', [(0,64,.74), (1,67,.79), (2.5,72,.83), (4,67,.88)]),
    ('reed', [(0,74,.86), (1.5,78,.84), (3,76,.78), (4.5,74,.77)]),
    ('mallet', [(0,71,.70), (1,74,.69), (2.5,77,.70), (4,71,.90)]),
    ('reed', [(0,72,.77), (1,76,.78), (2.5,74,.73), (4,72,1.18)]),
    ('mallet', [(0,64,.78), (1,67,.75), (2.5,72,2.05)]),
]
# Make held notes agree with the supporting triad. Brief D passing tones in
# the C motif stay, but no F-sharp, exposed sevenths, or long semitone rubs.
replacements = {
    1: {76:74}, 4:{76:72,74:69}, 5:{78:77,72:74}, 6:{77:79},
    10:{76:69}, 13:{79:72}, 14:{76:72,74:69}, 16:{78:77,76:69}, 17:{77:79}
}
phrases = [(kind, [(t,replacements.get(bar,{}).get(p,p),d) for t,p,d in notes])
           for bar,(kind,notes) in enumerate(phrases)]
second = [(kind, list(notes)) for kind,notes in phrases]
second[3] = ('mallet', [(0,67,.72),(1.5,72,.74),(3,76,.8),(4.5,72,.64)])
second[5] = ('mallet', [(0,69,.70),(1,74,.64),(2.5,77,.76),(4,74,.82)])
second[8] = ('reed', [(0,77,1.05),(1.5,72,.72),(3,69,1.6)])
second[9] = ('mallet', [(0,72,.76),(1.5,76,.72),(3,79,1.5)])
second[10] = ('reed', [(0,77,.78),(1.5,74,.80),(3,69,.68),(4.5,74,.82)])
second[15] = ('mallet', [(0,67,.74),(1,72,.79),(2.5,76,.83),(4,72,.88)])
second[19] = ('mallet', [(0,76,.78),(1,72,.75),(3.5,67,.70),(5,67,.44)])
phrases += second
melody_audit = []
for bar, (kind, notes) in enumerate(phrases):
    pan = {'mallet': -.06, 'pluck': -.12, 'reed': .13}[kind]
    for idx, (when, pitch, length) in enumerate(notes):
        velocity = .67 if idx == 0 else .58
        if 8 <= bar % 20 <= 11:
            velocity *= .91
        add(kind, pitch, bar*6+when, length, velocity, pan)
        pcs = {n%12 for n in harmony[bar][1]} | {harmony[bar][0]%12}
        if pitch%12 not in pcs:
            melody_audit.append({'bar':bar+1,'note':pitch,'duration_seconds':length*EIGHTH,
                                 'brief_passing_note': length<=1.0})
    if bar % 20 in [1, 5, 13, 17]:
        add('pluck', harmony[bar][1][0], bar*6+5.5, .42, .34, -.17)
    if bar in [19, 39]:
        add('reed', 64, bar*6+2.5, 1.60, .24, .14)
        add('bass', 48, bar*6+2.5, 1.5, .38)


def room(dry):
    result = dry.copy()
    # Short room emphasizes gesture and articulation, with no long pad-like wash.
    for c in [0, 1]:
        for j in range(15):
            seconds = .019 + .0267*j + .004*c
            shift = round(seconds*SR)
            source = dry[:, c if j%3 else 1-c]
            result[shift:, c] += source[:-shift]*(.034*math.exp(-seconds/.15))
    return result


mix = room(tracks['pluck']+tracks['reed']+tracks['mallet'])+tracks['bass']+tracks['wood']
# Fold release/reverb tails into the beginning instead of fading or truncating.
mix[:N-LOOP_N] += mix[LOOP_N:]
mix = mix[:LOOP_N]
freq = np.fft.rfftfreq(LOOP_N, 1/SR)
spectrum = np.fft.rfft(mix, axis=0)
spectrum *= ((1-np.exp(-(freq/28)**4))*np.exp(-(freq/11000)**4))[:, None]
mix = np.fft.irfft(spectrum, n=LOOP_N, axis=0)
mix *= 10**(-2/20)/np.max(np.abs(mix))
# Keep the accepted notes and rhythm, with the two phrase-release repairs.
import runpy
mix = runpy.run_path(str(Path(__file__).resolve().parents[1]/'CatCare20260915/prepare_audio.py'))['fill_title_tails'](mix)
pcm = np.rint(mix*32767).astype('<i2')
wav_path = OUT/'CatHome_Minik_Kasif_Menu_Loop.wav'
with wave.open(str(wav_path), 'wb') as wav:
    wav.setnchannels(2)
    wav.setsampwidth(2)
    wav.setframerate(SR)
    wav.writeframes(pcm.tobytes())


def vlq(n):
    chunks = [n&127]
    while n>>7:
        n >>= 7
        chunks.insert(0, (n&127)|128)
    return bytes(chunks)


events = [(0, b'\xff\x51\x03'+round(60_000_000/BPM).to_bytes(3,'big')),
          (0, b'\xff\x58\x04\x06\x03\x24\x08')]
channels = {kind: idx for idx, kind in enumerate(KINDS)}
for kind, program in [('pluck',45), ('reed',71), ('mallet',12), ('bass',43), ('wood',115)]:
    events.append((0,bytes([0xC0+channels[kind],program])))
for note in score:
    channel = channels[note['instrument']]
    start = round(note['eighth']*240)
    end = round((note['eighth']+note['duration_eighths'])*240)
    events += [(start, bytes([0x90+channel,note['midi_note'],round(note['velocity']*110)])),
               (end, bytes([0x80+channel,note['midi_note'],0]))]
events.sort(key=lambda x:(x[0],0 if x[1][0]&0xF0==0x80 else 1))
body = bytearray()
previous = 0
for tick, message in events:
    body.extend(vlq(tick-previous))
    body.extend(message)
    previous = tick
body.extend(b'\x00\xff\x2f\x00')
(OUT/'CatHome_Minik_Kasif_Menu_Score.mid').write_bytes(
    b'MThd'+struct.pack('>IHHH',6,0,1,480)+b'MTrk'+struct.pack('>I',len(body))+body)
(OUT/'score.json').write_text(json.dumps({'title':'Minik Kaşif', 'bpm_quarter':BPM,
    'meter':'6/8', 'key':'C major', 'harmony':[row[2] for row in harmony],
    'events':score}, ensure_ascii=False, indent=2),encoding='utf-8')
report = {
    'title':'Cat Home — Minik Kaşif / 60-second tuned menu loop',
    'duration_seconds':len(pcm)/SR, 'sample_rate':SR, 'channels':2, 'pcm_bits':16,
    'bpm_quarter':BPM, 'meter':'6/8', 'sample_peak_dbfs':float(20*np.log10(np.max(np.abs(pcm.astype(float)))/32768)),
    'rms_dbfs':float(20*np.log10(np.sqrt(np.mean((pcm.astype(float)/32768)**2)))),
    'clipped_samples':int(np.count_nonzero(np.abs(pcm.astype(np.int32))>=32767)),
    'first_frame':pcm[0].tolist(), 'last_frame':pcm[-1].tolist(),
    'finite':bool(np.isfinite(mix).all()), 'notes':len(score),
    'sha256':hashlib.sha256(wav_path.read_bytes()).hexdigest(),
    'composition':'40 bars, diatonic triads, corrected bass answers and held melody notes, new second-half variations, cyclic release tails.',
    'sound_sources':'Locally synthesized plucks, wooden-bar modes, rounded breath voice, bass, and short room taps; no recorded instruments.',
    'external_audio_samples':[], 'external_music_generation_services':[],
    'additional_music_generation_fee':0, 'listened_by_assistant':False,
    'game_integration':'separate integration step', 'not_a_seamless_loop':False,
    'loop_boundary_step':float(np.max(np.abs(mix[0]-mix[-1]))),
    'melody_nonchord_notes':melody_audit,
    'chromatic_pitched_notes':[n for n in score if n['instrument']!='wood' and n['midi_note']%12 not in {0,2,4,5,7,9,11}],
}
assert not report['chromatic_pitched_notes']
assert all(n['brief_passing_note'] for n in melody_audit), melody_audit
(OUT/'verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(report,ensure_ascii=True,indent=2))
