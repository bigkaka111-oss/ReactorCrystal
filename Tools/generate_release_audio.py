from pathlib import Path
import math, struct, wave
root=Path(__file__).resolve().parents[1]/"Assets"/"Sounds"/"Generated"
root.mkdir(parents=True,exist_ok=True)
def write(name,notes):
    data=[]
    rate=44100
    for frequency,duration in notes:
        n=int(rate*duration)
        for i in range(n):
            t=i/rate
            envelope=min(1,t/0.015)*min(1,(duration-t)/0.07)
            value=(math.sin(2*math.pi*frequency*t)+0.18*math.sin(4*math.pi*frequency*t))*0.24*max(0,envelope)
            data.append(struct.pack("<h",int(max(-1,min(1,value))*32767)))
    with wave.open(str(root/(name+".wav")),"wb") as f:
        f.setnchannels(1);f.setsampwidth(2);f.setframerate(rate);f.writeframes(b"".join(data))
write("StageComplete",[(660,.12),(880,.18)])
write("CycleComplete",[(440,.14),(554.37,.14),(659.25,.14),(880,.5)])
write("Shutdown",[(220,.2),(164.81,.24),(110,.5)])

