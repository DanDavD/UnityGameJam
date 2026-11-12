"""Epic soundtrack and god-day / wrath sound effects for GodBoard (standard library only).

Reuses the Track/reverb machinery of generate_music.py and adds brass, choir, taiko and noise voices.
Usage: python3 generate_epic.py <music_dir> <sfx_dir>
"""
import math, random, sys
from generate_music import Track, SR, hz, chord, render

TAU = 2 * math.pi


def voice(kind, t, dur, f, rng_noise):
    """One sample of a voice at time t (seconds since note start) -> (envelope*value)."""
    if kind == "brass":
        a = min(1.0, t / 0.18) * min(1.0, max(0.0, (dur - t) / 0.35))
        vib = 1 + 0.004 * math.sin(TAU * 5.2 * t)
        v = sum(math.sin(TAU * f * vib * h * t) / h for h in range(1, 9)) * 0.35
        return a * v
    if kind == "choir":
        a = min(1.0, t / 1.4) * min(1.0, max(0.0, (dur - t) / 1.6))
        v = 0.0
        for h in range(1, 14):
            fh = f * h
            form = math.exp(-((fh - 750) / 260) ** 2) + 0.7 * math.exp(-((fh - 1250) / 300) ** 2) + 0.05
            v += form * math.sin(TAU * fh * (1 + 0.003 * math.sin(TAU * 4.5 * t + h)) * t) / h
        return a * v * 0.5
    if kind == "string":
        a = math.exp(-t * 7) * min(1.0, t / 0.006)
        return a * sum(math.sin(TAU * f * h * t) / h for h in range(1, 6)) * 0.5
    if kind == "taiko":
        fr = 52 + 70 * math.exp(-t * 14)
        return math.exp(-t * 5.5) * (math.sin(TAU * fr * t) + 0.35 * rng_noise() * math.exp(-t * 40))
    if kind == "noise":  # rumble / crack: f = lowpass strength 0..1
        return math.exp(-t * (6.0 if f < 0.5 else 3.0)) * rng_noise() * (1.0 if t < 0.02 else 0.6)
    if kind == "caw":
        sweep = 700 + 500 * math.exp(-((t - 0.1) / 0.12) ** 2)
        a = min(1.0, t / 0.02) * math.exp(-t * 5)
        return a * (math.sin(TAU * sweep * t) * 0.5 + rng_noise() * 0.35 * (0.6 + 0.4 * math.sin(TAU * 60 * t)))
    return 0.0


def add(track, start, dur, freq, amp, kind, rng):
    s0 = int(start * SR)
    n = int(dur * SR)
    end = min(len(track.buf), s0 + n)
    if s0 >= len(track.buf):
        return
    lp = [0.0]

    def noise():
        lp[0] += 0.35 * ((rng.random() * 2 - 1) - lp[0])
        return lp[0] * 2.2

    buf = track.buf
    for i in range(end - s0):
        buf[s0 + i] += amp * voice(kind, i / SR, dur, freq, noise)


def epic_match(bpm=76, bars=16, seed=5, drive=1.0, minor_tail=("A", "C#", "E")):
    beat = 60 / bpm
    seconds = bars * 4 * beat
    prog = [(["D3", "F3", "A3"], "D2"), (["Bb2", "D3", "F3"], "Bb1"), (["F3", "A3", "C4"], "F2"), (["C3", "E3", "G3"], "C2"),
            (["D3", "F3", "A3"], "D2"), (["Bb2", "D3", "F3"], "Bb1"), (["G2", "Bb2", "D3"], "G1"), (["A2", "C#3", "E3"], "A1")]
    melody = [(0, 3, "A4"), (3, 1, "D5"), (4, 2, "F5"), (6, 2, "E5"), (8, 3, "D5"), (11, 1, "C5"), (12, 4, "Bb4"),
              (16, 3, "C5"), (19, 1, "D5"), (20, 2, "F5"), (22, 2, "G5"), (24, 4, "A5"), (28, 4, "E5")]

    def sched(tr, off, rng):
        for bar in range(bars):
            tones, root = prog[(bar // 2) % 8]
            t0 = off + bar * 4 * beat
            for f in chord(tones):
                add(tr, t0 - 0.2, 4 * beat + 0.4, f, 0.10, "brass", rng)
            add(tr, t0, 4 * beat, hz(root), 0.22, "brass", rng)
            if bar % 2 == 0:
                for f in chord(tones):
                    add(tr, t0 - 0.3, 8 * beat + 0.6, f * 2, 0.09, "choir", rng)
            add(tr, t0, 1.2, 0, 0.55, "taiko", rng)
            add(tr, t0 + 2 * beat, 1.2, 0, 0.40, "taiko", rng)
            if bar % 4 == 3:
                for s in range(8):
                    add(tr, t0 + 3 * beat + s * beat / 8, 0.5, 0, 0.18 + 0.03 * s, "taiko", rng)
            for e in range(8):
                f = chord(tones)[[0, 2, 0, 2, 1, 2, 0, 2][e]] * 2
                add(tr, t0 + e * beat / 2, 0.4, f, 0.06 * drive, "string", rng)
        for k in range(2):  # melody in two 16-beat phrases per 32 beats, repeated over the loop
            base = off + k * 32 * beat
            for b, d, n in melody:
                add(tr, base + b * beat, d * beat, hz(n), 0.16, "brass", rng)
                add(tr, base + b * beat, d * beat, hz(n) * 2, 0.04, "choir", rng)

    return render(seconds, sched, seed)


def epic_menu():
    beat = 60 / 58
    bars = 16
    seconds = bars * 4 * beat
    prog = [["D3", "F3", "A3", "D4"], ["Bb2", "D3", "F3", "Bb3"], ["G2", "Bb2", "D3", "G3"], ["A2", "C#3", "E3", "A3"]]

    def sched(tr, off, rng):
        for c in range(4):
            for f in chord(prog[c]):
                add(tr, off + c * 16 * beat - 1.0, 16 * beat + 2.0, f * 2, 0.11, "choir", rng)
                add(tr, off + c * 16 * beat - 0.5, 16 * beat + 1.0, f, 0.07, "brass", rng)
            add(tr, off + c * 16 * beat, 2.5, 0, 0.5, "taiko", rng)
        for bar in range(bars):
            add(tr, off + bar * 4 * beat, 1.5, 0, 0.30, "taiko", rng)
        for b in (0, 8, 16, 24, 32, 40, 48, 56):
            add(tr, off + b * beat, 6.0, hz(["A4", "D5", "F5", "E5", "A4", "C5", "D5", "A4"][b // 8]), 0.10, "choir", rng)

    return render(seconds, sched, 7)


def sfx(name, events, seconds, seed, reverb=0.25):
    tr = Track(seconds)
    rng = random.Random(seed)
    for start, dur, f, amp, kind in events:
        add(tr, start, dur, f, amp, kind, rng)
    tr.length = int(seconds * SR)
    tr.reverb(reverb)
    tr.buf = [0.0] * 0 or tr.buf
    return tr


def save_sfx(tr, path):
    # SFX are one-shots: keep the first half (reverb tail included), not a loop.
    import wave, struct
    data = tr.buf[: tr.length]
    peak = max(abs(x) for x in data) or 1.0
    with wave.open(path, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, x * 0.85 / peak)) * 32767)) for x in data))


def build_sfx(out):
    n = hz
    sets = {
        "sfx-troops-gain": (0.9, [(0.0, 0.5, n("E5"), 0.5, "string"), (0.12, 0.5, n("A5"), 0.5, "string"), (0.24, 0.7, n("E6"), 0.35, "string")]),
        "sfx-day-zeus": (1.8, [(0.0, 0.6, 0.2, 1.0, "noise"), (0.05, 1.6, 0.9, 0.7, "noise"), (0.0, 0.9, n("D2"), 0.8, "taiko"), (0.0, 1.2, n("A5"), 0.12, "brass")]),
        "sfx-day-ra": (2.0, [(0.0, 1.8, n("D4"), 0.12, "choir"), (0.0, 1.8, n("A4"), 0.10, "choir"), (0.2, 0.6, n("D5"), 0.4, "string"), (0.35, 0.6, n("F#5"), 0.4, "string"), (0.5, 0.6, n("A5"), 0.4, "string"), (0.65, 0.9, n("D6"), 0.4, "string")]),
        "sfx-day-thor": (1.8, [(0.0, 1.2, 0, 1.0, "taiko"), (0.0, 0.5, 0.2, 0.9, "noise"), (0.02, 1.4, n("D2"), 0.35, "brass"), (0.0, 1.0, n("A3"), 0.5, "string")]),
        "sfx-day-morrigan": (2.0, [(0.0, 0.45, 0, 0.8, "caw"), (0.5, 0.45, 0, 0.7, "caw"), (0.9, 0.4, 0, 0.6, "caw"), (0.0, 1.9, n("D2"), 0.25, "brass"), (0.0, 1.9, n("A2"), 0.18, "brass")]),
        "sfx-bad-omen": (2.4, [(0.0, 2.2, n("D2"), 0.4, "brass"), (0.0, 2.2, n("Eb2"), 0.3, "brass"), (0.0, 0.8, 0, 0.9, "taiko"), (0.9, 1.4, n("A2"), 0.25, "brass")]),
        "sfx-good-fortune": (1.6, [(i * 0.1, 0.9, n(x), 0.4, "string") for i, x in enumerate(["D5", "F#5", "A5", "D6", "F#6"])] + [(0.0, 1.5, n("D4"), 0.10, "choir")]),
        "sfx-wrath-zeus": (1.8, [(0.0, 0.8, 0.1, 1.0, "noise"), (0.0, 1.6, 0.9, 0.9, "noise"), (0.0, 1.0, 0, 1.0, "taiko"), (0.0, 1.4, n("A6"), 0.10, "brass")]),
        "sfx-wrath-thor": (2.0, [(0.0, 1.5, 0, 1.0, "taiko"), (0.12, 1.5, 0, 0.9, "taiko"), (0.0, 0.6, 0.1, 1.0, "noise"), (0.0, 1.8, n("D2"), 0.5, "brass")]),
        "sfx-wrath-ra": (2.2, [(0.0, 2.0, n("D4"), 0.14, "choir"), (0.0, 2.0, n("A4"), 0.14, "choir"), (0.0, 2.0, n("D5"), 0.12, "choir"), (0.3, 1.0, n("D6"), 0.5, "string"), (0.6, 1.0, n("A6"), 0.4, "string"), (0.0, 1.0, 0, 0.5, "taiko")]),
        "sfx-wrath-morrigan": (2.2, [(i * 0.28, 0.45, 0, 0.8, "caw") for i in range(5)] + [(0.0, 2.1, n("D2"), 0.35, "brass"), (0.0, 1.0, 0.2, 0.6, "noise")]),
    }
    for i, (name, (secs, ev)) in enumerate(sets.items()):
        print("sfx", name, flush=True)
        save_sfx(sfx(name, ev, secs, 100 + i), f"{out}/{name}.wav")


if __name__ == "__main__":
    music_dir, sfx_dir = sys.argv[1], sys.argv[2]
    print("music-match", flush=True); epic_match().save(f"{music_dir}/music-match.wav")
    print("music-tension", flush=True); epic_match(bpm=104, seed=9, drive=1.6).save(f"{music_dir}/music-tension.wav")
    print("music-menu", flush=True); epic_menu().save(f"{music_dir}/music-menu.wav")
    build_sfx(sfx_dir)
    print("done")
