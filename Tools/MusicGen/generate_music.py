"""Procedural background music for GodBoard (pure Python, standard library only).

Three seamless loops: menu (calm, mysterious), match (steady, ritual) and tension
(faster, used when a god is close to winning). Everything is synthesized from sine
partials and a small Schroeder reverb, so the music is original and license-free.
Loops are rendered twice and the second pass is kept, so reverb tails wrap perfectly.

Usage: python3 generate_music.py <output_dir>
"""
import math, random, struct, sys, wave

SR = 22050
NOTE = {"C": 0, "C#": 1, "Db": 1, "D": 2, "Eb": 3, "E": 4, "F": 5, "F#": 6, "G": 7, "Ab": 8, "A": 9, "Bb": 10, "B": 11}


def hz(name):
    # "D3" -> frequency
    pitch, octave = name[:-1], int(name[-1])
    midi = 12 * (octave + 1) + NOTE[pitch]
    return 440.0 * 2 ** ((midi - 69) / 12)


class Track:
    def __init__(self, seconds):
        self.length = int(seconds * SR)
        self.buf = [0.0] * (self.length * 2)

    def add(self, start, dur, freq, amp, kind):
        s0 = int(start * SR)
        n = int(dur * SR)
        buf = self.buf
        end = min(len(buf), s0 + n)
        if s0 >= len(buf):
            return
        two_pi = 2 * math.pi
        for i in range(end - s0):
            t = i / SR
            if kind == "pad":
                a = min(1.0, t / 2.5) * min(1.0, (dur - t) / 2.5)
                v = (math.sin(two_pi * freq * t) + 0.6 * math.sin(two_pi * freq * 1.004 * t)
                     + 0.35 * math.sin(two_pi * freq * 0.996 * t + 1.1) + 0.15 * math.sin(two_pi * freq * 2 * t)) * 0.4
            elif kind == "bell":
                a = math.exp(-t * 1.5) * min(1.0, t / 0.005)
                v = (math.sin(two_pi * freq * t) + 0.5 * math.sin(two_pi * freq * 2.76 * t) * math.exp(-t * 2)
                     + 0.25 * math.sin(two_pi * freq * 5.4 * t) * math.exp(-t * 4))
            elif kind == "pluck":
                a = math.exp(-t * 5.0) * min(1.0, t / 0.004)
                v = math.sin(two_pi * freq * t) + 0.5 * math.sin(two_pi * freq * 2 * t) * math.exp(-t * 6) + 0.2 * math.sin(two_pi * freq * 3 * t)
            elif kind == "bass":
                a = math.exp(-t * 2.2) * min(1.0, t / 0.01)
                v = math.sin(two_pi * freq * t) + 0.3 * math.sin(two_pi * freq * 2 * t)
            elif kind == "thump":
                f = 48 + 60 * math.exp(-t * 18)
                a = math.exp(-t * 7)
                v = math.sin(two_pi * f * t)
            elif kind == "drone":
                a = min(1.0, t / 4.0) * min(1.0, (dur - t) / 4.0)
                v = math.sin(two_pi * freq * t) * (0.8 + 0.2 * math.sin(two_pi * 0.12 * t)) + 0.3 * math.sin(two_pi * freq * 1.5 * t)
            else:
                a, v = 0.0, 0.0
            buf[s0 + i] += amp * a * v

    def reverb(self, mix=0.35):
        src = self.buf
        n = len(src)
        combs = [(1117, 0.84), (1188, 0.83), (1277, 0.82), (1356, 0.81)]
        out = [0.0] * n
        for delay, fb in combs:
            line = [0.0] * delay
            idx = 0
            for i in range(n):
                y = line[idx]
                line[idx] = src[i] + y * fb
                out[i] += y * 0.25
                idx += 1
                if idx == delay:
                    idx = 0
        for delay, g in ((225, 0.5), (556, 0.5)):
            line = [0.0] * delay
            idx = 0
            for i in range(n):
                x = out[i]
                y = line[idx]
                line[idx] = x + y * g
                out[i] = y - g * x
                idx += 1
                if idx == delay:
                    idx = 0
        self.buf = [src[i] * (1 - mix) + out[i] * mix * 2.2 for i in range(n)]

    def save(self, path):
        loop = self.buf[self.length:]
        peak = max(abs(x) for x in loop) or 1.0
        gain = 0.8 / peak
        data = b"".join(struct.pack("<h", int(max(-1.0, min(1.0, x * gain)) * 32767)) for x in loop)
        with wave.open(path, "wb") as w:
            w.setnchannels(1)
            w.setsampwidth(2)
            w.setframerate(SR)
            w.writeframes(data)


def chord(names):
    return [hz(n) for n in names]


def render(seconds, schedule, seed):
    """schedule(track, offset, rng) places events for one pass; called for both passes."""
    track = Track(seconds)
    for p in range(2):
        rng = random.Random(seed)
        schedule(track, p * seconds, rng)
    track.reverb()
    return track


def menu():
    bpm, beats = 60, 64
    beat = 60 / bpm
    seconds = beats * beat
    chords = [["D3", "F3", "A3", "E4"], ["Bb2", "D3", "F3", "C4"], ["G2", "Bb2", "D3", "F3"], ["C3", "E3", "G3", "D4"]]
    scale = ["D4", "F4", "G4", "A4", "C5", "D5", "F5"]

    def sched(tr, off, rng):
        tr.add(off, seconds, hz("D2"), 0.25, "drone")
        for c, notes in enumerate(chords):
            for f in chord(notes):
                tr.add(off + c * 16 * beat - 1.0, 16 * beat + 2.0, f, 0.10, "pad")
        for b in range(beats):
            if rng.random() < 0.38:
                tr.add(off + b * beat, 5.0, hz(rng.choice(scale)), 0.10, "bell")

    return render(seconds, sched, 11)


def match():
    bpm, bars = 84, 16
    beat = 60 / bpm
    seconds = bars * 4 * beat
    prog = [(["D3", "F3", "A3"], "D2"), (["Bb2", "D3", "F3"], "Bb1"), (["F3", "A3", "C4"], "F2"), (["C3", "E3", "G3"], "C2")]

    def sched(tr, off, rng):
        for bar in range(bars):
            tones, root = prog[(bar // 4) % 4]
            t0 = off + bar * 4 * beat
            if bar % 4 == 0:
                for f in chord(tones):
                    tr.add(t0 - 0.5, 16 * beat + 1.0, f, 0.09, "pad")
            tr.add(t0, 2.0, hz(root), 0.30, "bass")
            tr.add(t0, 0.6, 0, 0.30, "thump")
            tr.add(t0 + 2 * beat, 0.6, 0, 0.22, "thump")
            for e in range(8):
                note = chord(tones)[[0, 1, 2, 1, 2, 1, 0, 1][e]] * 2
                tr.add(t0 + e * beat / 2, 0.9, note, 0.06 + 0.03 * rng.random(), "pluck")

    return render(seconds, sched, 22)


def tension():
    bpm, bars = 104, 16
    beat = 60 / bpm
    seconds = bars * 4 * beat
    prog = [(["D3", "F3", "A3"], "D2"), (["Bb2", "D3", "F3"], "Bb1"), (["G2", "Bb2", "D3"], "G1"), (["A2", "C#3", "E3"], "A1")]

    def sched(tr, off, rng):
        for bar in range(bars):
            tones, root = prog[(bar // 4) % 4]
            t0 = off + bar * 4 * beat
            if bar % 4 == 0:
                for f in chord(tones):
                    tr.add(t0 - 0.4, 16 * beat + 0.8, f, 0.09, "pad")
                tr.add(t0, 16 * beat, hz(root) * 1.059, 0.10, "drone")
            for b in range(4):
                tr.add(t0 + b * beat, 0.5, 0, 0.30, "thump")
            for e in range(8):
                tr.add(t0 + e * beat / 2, 1.0, hz(root), 0.16, "bass")
            for s in range(16):
                note = chord(tones)[[0, 2, 1, 2][s % 4]] * 2
                tr.add(t0 + s * beat / 4, 0.4, note, 0.05, "pluck")
            if bar % 4 == 0:
                tr.add(t0, 5.0, chord(tones)[2] * 4, 0.09, "bell")

    return render(seconds, sched, 33)


if __name__ == "__main__":
    out = sys.argv[1]
    for name, fn in (("music-menu", menu), ("music-match", match), ("music-tension", tension)):
        print("rendering", name, flush=True)
        fn().save(f"{out}/{name}.wav")
    print("done")
