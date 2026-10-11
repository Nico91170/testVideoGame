"""Original procedural score / effects. Standard Python 3 only; no third-party samples.
Run python3 Blender/Tools/generate_audio.py. Deterministic mono PCM16 22.05 kHz.
"""
import math, random, wave, struct, json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'UnityProject/Assets/_RIVE404/Resources/Audio'
OUT.mkdir(parents=True,exist_ok=True)
RATE=22050
TAU=math.tau
REPORT=[]

def tone(hz,t): return math.sin(TAU*hz*t)
def midi(n): return 440*2**((n-69)/12)
def clamp(x): return max(-.98,min(.98,x))
def save(name,duration,fn):
    count=round(duration*RATE); rand=random.Random(404+sum(map(ord,name)))
    data=bytearray(count*2); peak=0
    for i in range(count):
        value=clamp(fn(i/RATE,rand)); peak=max(peak,abs(value))
        struct.pack_into('<h',data,i*2,round(value*32767))
    path=OUT/(name+'.wav')
    with wave.open(str(path),'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(RATE); w.writeframes(data)
    REPORT.append({'name':name,'duration':duration,'sample_rate':RATE,'peak':round(peak,4),'bytes':path.stat().st_size})

def sfx(name,duration,kind):
    def synth(t,r):
        u=t/duration; env=min(1,t/.006)*max(0,1-u)**2
        if kind=='step': return .30*(r.uniform(-1,1)*.5+tone(90-35*u,t)*.5)*env
        if kind=='impact': return .60*(r.uniform(-1,1)*.6+tone(120-65*u,t)*.4)*env
        if kind=='swing': return .18*r.uniform(-1,1)*math.sin(math.pi*u)**2
        if kind=='scan': return .22*(tone(350+900*u,t)*.6+tone(1050+500*u,t)*.3)*env
        if kind=='patch':
            hz=[midi(64),midi(67),midi(71),midi(76)][min(3,int(u*4))]
            return .20*(tone(hz,t)+.24*tone(hz*2,t))*env
        if kind=='override': return .22*(tone(600-480*u,t)*.65+tone(300-200*u,t)*.35)*env
        if kind=='ui': return .17*tone(880,t)*env
        if kind=='quest': return .18*(tone(midi(64),t)+tone(midi(67),t)+tone(midi(71),t))*.5*env
        if kind=='death': return .24*(tone(180-90*u,t)*.6+tone(90-45*u,t)*.4)*env
        return .17*(tone(660,t)*.6+tone(990,t)*.4)*env
    save(name,duration,synth)

for args in [('Footstep',.16,'step'),('Swing',.24,'swing'),('Impact',.22,'impact'),
             ('Scan',.70,'scan'),('Patch',.8,'patch'),('Override',.7,'override'),
             ('UI',.09,'ui'),('Quest',1.4,'quest'),('Death',1.5,'death'),('Energy',.5,'energy')]: sfx(*args)

# 64 seconds = eight 8-second bars. All sustained frequencies align with a short
# boundary fade, avoiding discontinuities when AudioSource.loop wraps the clip.
CHORDS=[(52,55,59),(48,52,55),(55,59,62),(50,54,57),(52,55,59),(48,52,55),(57,60,64),(50,54,57)]
MELODY=[76,74,71,67,69,71,74,71,72,71,67,64,67,69,71,74]
def theme(t,r):
    chord=CHORDS[min(7,int(t/8))]
    bar=t%8; beat=int(t/.5); note_t=t%.5
    arp_note=chord[(beat//1)%3]+12
    arp_env=math.exp(-note_t*7)*min(1,note_t/.009)
    arp=(tone(midi(arp_note),t)+.18*tone(midi(arp_note)*2,t))*.075*arp_env
    pad=sum(tone(midi(n),t) for n in chord)*.026*(.75+.25*math.sin(math.pi*bar/8))
    bass=tone(midi(chord[0]-12),t)*.050
    mel_note=MELODY[int(t/2)%len(MELODY)]; mt=t%2
    melody=tone(midi(mel_note),t)*.072*math.exp(-mt*1.5)*min(1,mt/.02)
    boundary=min(1,t/.08,(64-t)/.08)
    return (arp+pad+bass+melody)*boundary
save('Theme',64,theme)

def ambient(kind):
    def sound(t,r):
        boundary=min(1,t/.1,(20-t)/.1)
        wind=r.uniform(-1,1)*(.010+.010*(.5+.5*math.sin(t*.7)))
        if kind=='port':
            waves=tone(83,t)*.014*(.5+.5*math.sin(t*.8))+tone(129,t)*.007
            gull=.010*tone(1300+130*math.sin(t*5),t)*max(0,math.sin(t*.9))**20
            return (wind+waves+gull)*boundary
        if kind=='neon':
            return (tone(60,t)*.012+tone(120,t)*.006+tone(420+8*math.sin(t),t)*.004+wind*.3)*boundary
        return (wind*2+tone(70,t)*.013*(.5+.5*math.sin(t*.3)))*boundary
    return sound
for name,kind in [('PortAmbience','port'),('NeonAmbience','neon'),('CliffsAmbience','cliffs')]: save(name,20,ambient(kind))
(ROOT/'Blender'/'audio_report.json').write_text(json.dumps(REPORT,indent=2),encoding='utf-8')
print(json.dumps(REPORT,indent=2))
