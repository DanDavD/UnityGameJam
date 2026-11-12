"""Slow, medieval, tragic-and-epic soundtrack plus small UI sounds for GodBoard (standard library only).

Warm, human-sounding voices (lute, bowed strings, flute, soft frame drum, drone) over a lament bass
(Dm - C - Bb - A), humanized timing and a long reverb. Usage: python3 generate_medieval.py <music_dir> <sfx_dir>
"""
import math, random, sys
from generate_music import Track, SR, hz, chord, render
from generate_epic import save_sfx

TAU = 2 * math.pi


def sample(kind, t, dur, f, noise):
    if kind == "lute":
        a = math.exp(-t * 3.2) * min(1.0, t / 0.004)
        return a * (math.sin(TAU * f * t) + 0.55 * math.sin(TAU * 2.003 * f * t) * math.exp(-t * 3) + 0.3 * math.sin(TAU * 3.01 * f * t) * math.exp(-t * 5) + 0.12 * math.sin(TAU * 4 * f * t) * math.exp(-t * 7))
    if kind == "bow":
        a = min(1.0, t / 0.9) * min(1.0, max(0.0, (dur - t) / 1.2))
        vib = 1 + 0.0035 * math.sin(TAU * 5.0 * t) * min(1.0, t / 1.5)
        return a * 0.5 * sum(math.sin(TAU * f * vib * h * t + 0.3 * h) / (h ** 1.4) for h in range(1, 7)) * (1 + 0.05 * math.sin(TAU * f * 1.006 * t))
    if kind == "flute":
        a = min(1.0, t / 0.12) * min(1.0, max(0.0, (dur - t) / 0.3))
        vib = 1 + 0.006 * math.sin(TAU * 4.6 * t) * min(1.0, t / 0.6)
        return a * (math.sin(TAU * f * vib * t) + 0.18 * math.sin(TAU * 2 * f * vib * t) + 0.25 * noise() * min(1.0, 0.4 + t))
    if kind == "frame":
        fr = 78 + 40 * math.exp(-t * 25)
        return math.exp(-t * 8) * (math.sin(TAU * fr * t) + 0.5 * noise() * math.exp(-t * 60))
    if kind == "drone":
        a = min(1.0, t / 3.0) * min(1.0, max(0.0, (dur - t) / 3.0))
        return a * (math.sin(TAU * f * t) + 0.5 * math.sin(TAU * f * 1.5 * t) + 0.25 * math.sin(TAU * f * 2.01 * t)) * (0.85 + 0.15 * math.sin(TAU * 0.1 * t))
    if kind == "bell":
        return math.exp(-t * 2.2) * (math.sin(TAU * f * t) + 0.4 * math.sin(TAU * f * 2.4 * t) * math.exp(-t * 2.5) + 0.2 * math.sin(TAU * f * 3.9 * t) * math.exp(-t * 4))
    if kind == "knock":
        return math.exp(-t * 28) * (math.sin(TAU * f * t) + 0.4 * noise() * math.exp(-t * 80))
    return 0.0


def add(track, start, dur, f, amp, kind, rng):
    s0 = int(start * SR)
    end = min(len(track.buf), s0 + int(dur * SR))
    if s0 >= len(track.buf) or s0 < 0:
        return
    lp = [0.0]

    def noise():
        lp[0] += 0.3 * ((rng.random() * 2 - 1) - lp[0])
        return lp[0] * 2.4

    buf = track.buf
    for i in range(end - s0):
        buf[s0 + i] += amp * sample(kind, i / SR, dur, f, noise)


def human(rng, amount=0.03):
    return rng.uniform(-amount, amount)


LAMENT = [(["D3", "F3", "A3"], "D2"), (["C3", "E3", "G3"], "C2"), (["Bb2", "D3", "F3"], "Bb1"), (["A2", "C#3", "E3"], "A1")]
MELODY = [(0, 4, "A4"), (4, 2, "D5"), (6, 2, "C5"), (8, 3, "G4"), (11, 1, "A4"), (12, 4, "Bb4"),
          (16, 2, "D5"), (18, 2, "E5"), (20, 3, "F5"), (23, 1, "E5"), (24, 4, "D5"), (28, 4, "A4")]


def song(bpm, bars, seed, plucks, drums, melody=True):
    beat = 60 / bpm
    seconds = bars * 4 * beat

    def sched(tr, off, rng):
        for bar in range(bars):
            tones, root = LAMENT[bar % 4]
            t0 = off + bar * 4 * beat
            add(tr, t0 - 0.4, 4 * beat + 0.8, hz(root), 0.22, "drone", rng)
            for f in chord(tones):
                add(tr, t0 - 0.3, 4 * beat + 0.6, f, 0.07, "bow", rng)
                add(tr, t0 - 0.3, 4 * beat + 0.6, f * 2, 0.04, "bow", rng)
            if plucks:
                pattern = [0, 2, 1, 2, 0, 2, 1, 2][: plucks]
                step = 4 * beat / plucks
                for e in range(plucks):
                    add(tr, t0 + e * step + human(rng), 1.6, chord(tones)[pattern[e % len(pattern)]] * 2, 0.10 * (0.7 + 0.3 * rng.random()), "lute", rng)
            if drums:
                add(tr, t0 + human(rng, 0.02), 0.6, 0, 0.45, "frame", rng)
                add(tr, t0 + 2 * beat + human(rng, 0.02), 0.6, 0, 0.30, "frame", rng)
                if drums > 1:
                    add(tr, t0 + 3 * beat + human(rng, 0.02), 0.6, 0, 0.22, "frame", rng)
        if melody:
            for k in range(bars // 8):
                base = off + k * 32 * beat
                for b, d, n in MELODY:
                    add(tr, base + b * beat + human(rng, 0.05), d * beat, hz(n), 0.20, "flute", rng)

    return render(seconds, sched, seed)


def ui_sfx(out):
    jobs = {
        "sfx-card-hover": (0.9, [(0.0, 0.8, hz("A5"), 0.35, "bell"), (0.07, 0.8, hz("D6"), 0.22, "bell")]),
        "sfx-bot-result": (0.9, [(0.0, 0.2, hz("D3"), 0.8, "knock"), (0.09, 0.8, hz("A3"), 0.25, "bell")]),
        "sfx-soft-good": (1.1, [(0.0, 0.2, hz("D3"), 0.5, "knock"), (0.06, 0.9, hz("A4"), 0.28, "bell"), (0.2, 0.9, hz("D5"), 0.24, "bell")]),
        "sfx-soft-bad": (1.2, [(0.0, 0.2, hz("D3"), 0.6, "knock"), (0.06, 1.0, hz("F4"), 0.26, "bell"), (0.22, 1.0, hz("Bb3"), 0.26, "bell")]),
    }
    for i, (name, (secs, ev)) in enumerate(jobs.items()):
        tr = Track(secs)
        rng = random.Random(40 + i)
        for s, d, f, a, k in ev:
            add(tr, s, d, f, a, k, rng)
        tr.length = int(secs * SR)
        tr.reverb(0.2)
        save_sfx(tr, f"{out}/{name}.wav")
        print("sfx", name, flush=True)


if __name__ == "__main__":
    music_dir, sfx_dir = sys.argv[1], sys.argv[2]
    print("menu", flush=True); song(52, 16, 3, plucks=0, drums=0).save(f"{music_dir}/music-menu.wav")
    print("match", flush=True); song(60, 16, 5, plucks=8, drums=1).save(f"{music_dir}/music-match.wav")
    print("tension", flush=True); song(78, 16, 8, plucks=8, drums=2).save(f"{music_dir}/music-tension.wav")
    ui_sfx(sfx_dir)
    print("done")
