"""Writes 'The Six Faces' (GodBoard main theme) as a format-1 multitrack MIDI file.

Pure Python, no dependencies. Usage: python3 compose_theme.py <output.mid>
"""

import random
import struct
import sys

PPQ = 480
BAR = 4 * PPQ
rng = random.Random(7)  # seeded so the "human" feel is reproducible

N = {}
for octave in range(1, 7):
    for name, semi in (("C", 0), ("C#", 1), ("D", 2), ("Eb", 3), ("E", 4), ("F", 5),
                       ("F#", 6), ("G", 7), ("Bb", 10), ("A", 9), ("B", 11)):
        N[f"{name}{octave}"] = 12 * (octave + 1) + semi

CHORDS = {
    "Dm": ("D", "F", "A"), "C": ("C", "E", "G"), "Bb": ("Bb", "D", "F"),
    "G": ("G", "B", "D"), "F": ("F", "A", "C"), "Gm": ("G", "Bb", "D"),
    "A": ("A", "C#", "E"),
}

# Form (bars): intro 6, A 10, A' 10, B 10, coda 7 = 43 bars, ~2:27 at 70 BPM.
INTRO, A, A2, B, CODA = 0, 6, 16, 26, 36
TOTAL_BARS = 43
PROG = (
    ["Dm"] * 4 + ["C", "Dm"]                                  # intro
    + ["Dm", "C"] + ["Dm", "C", "Bb", "C"] * 2                # A
    + ["Dm", "G", "Dm", "C"] * 2 + ["Dm", "C"]                # A'
    + ["F", "C", "Gm", "A"] * 2 + ["Gm", "A"]                 # B
    + ["Dm"] * 5 + ["A", "A"]                                 # coda, ends on A to loop
)
assert len(PROG) == TOTAL_BARS


class Track:
    def __init__(self, name, channel, program):
        self.name, self.ch, self.events = name, channel, []
        if program is not None:
            self.events.append((0, bytes([0xC0 | channel, program])))

    def note(self, tick, pitch, dur, vel, human=True):
        if human:
            tick = max(0, tick + rng.randint(-12, 12))
            vel += rng.randint(-7, 7)
        vel = max(1, min(127, vel))
        self.events.append((tick, bytes([0x90 | self.ch, pitch, vel])))
        self.events.append((tick + dur, bytes([0x80 | self.ch, pitch, 0])))

    def line(self, start_bar, notes, vel, octave_shift=0):
        """notes: list of (name or None, beats)."""
        t = start_bar * BAR
        for name, beats in notes:
            d = int(beats * PPQ)
            if name:
                self.note(t, N[name] + 12 * octave_shift, int(d * 0.97), vel)
            t += d

    def cc(self, tick, num, val):
        self.events.append((tick, bytes([0xB0 | self.ch, num, val])))


def vlq(n):
    out = [n & 0x7F]
    n >>= 7
    while n:
        out.append((n & 0x7F) | 0x80)
        n >>= 7
    return bytes(reversed(out))


def chunk(events, name=None):
    body = b""
    if name:
        b = name.encode()
        body += b"\x00\xff\x03" + vlq(len(b)) + b
    last = 0
    # note-offs before note-ons at the same tick
    for tick, data in sorted(events, key=lambda e: (e[0], 0 if e[1][0] & 0xF0 == 0x80 else 1)):
        body += vlq(tick - last) + data
        last = tick
    body += b"\x00\xff\x2f\x00"
    return b"MTrk" + struct.pack(">I", len(body)) + body


def tempo_event(tick, bpm):
    us = int(60_000_000 / bpm)
    return (tick, b"\xff\x51\x03" + us.to_bytes(3, "big"))


def chord_tones(chord, octave):
    root, third, fifth = CHORDS[chord]
    r = N[f"{root}{octave}"]
    t = N[f"{third}{octave}"]
    f = N[f"{fifth}{octave}"]
    if t < r:
        t += 12
    if f < r:
        f += 12
    return r, t, f


# --- Tracks -------------------------------------------------------------------
bouzouki = Track("Bouzouki", 0, 25)   # acoustic steel guitar
flute = Track("Low flute", 1, 73)     # flute
cello = Track("Cello", 2, 42)         # cello
choir = Track("Choir", 3, 52)         # choir aahs
drone = Track("Hurdy-gurdy drone", 4, 109)  # bagpipe: closest GM drone
drum = Track("Frame drum", 9, None)   # GM percussion channel

for t in (bouzouki, flute, cello, choir, drone):
    t.cc(0, 91, 90)  # reverb send: stone hall
cello.cc(0, 7, 100)
choir.cc(0, 7, 70)
drone.cc(0, 7, 55)


def arpeggio(bar, chord, vel):
    """Fingerpicked eighths: root, fifth, octave, tenth, octave, fifth, root+8, fifth."""
    r, t, f = chord_tones(chord, 3)
    pattern = [r, f, r + 12, t + 12, r + 12, f, r + 12, f + 12]
    for i, p in enumerate(pattern):
        accent = 8 if i in (0, 4) else 0
        bouzouki.note(bar * BAR + i * PPQ // 2, p, PPQ, vel + accent)


# Drone: D (root + fifth) all piece long, switching to A on the last two bars.
drone.note(0, N["D2"], 41 * BAR, 50, human=False)
drone.note(0, N["A2"], 41 * BAR, 42, human=False)
drone.note(41 * BAR, N["A2"], 2 * BAR, 45, human=False)
drone.note(41 * BAR, N["E3"], 2 * BAR, 38, human=False)

# Intro: the "destiny" motif D-A-G-F-E-D, alone and free, then arpeggios.
bouzouki.line(INTRO, [("D5", 2), ("A4", 2), ("G4", 1), ("F4", 1), ("E4", 2), ("D4", 4)], 72)
for b in range(INTRO + 3, A):
    arpeggio(b, PROG[b], 56)

# A: bouzouki arpeggios, low flute melody from bar 3 of the section.
for b in range(A, A2):
    arpeggio(b, PROG[b], 58)
flute_a = [
    ("D4", 2), ("A4", 1), ("G4", 1),
    ("F4", 1.5), ("E4", 0.5), ("D4", 1), ("E4", 1),
    ("F4", 2), ("G4", 1), ("A4", 1),
    ("G4", 3), (None, 1),
    ("A4", 2), ("C5", 1), ("D5", 1),
    ("C5", 1.5), ("A4", 0.5), ("G4", 1), ("E4", 1),
    ("F4", 1), ("G4", 1), ("F4", 1), ("E4", 1),
    ("E4", 2), ("D4", 2),
]
flute.line(A + 2, flute_a, 70)

# A': cello answers with the "rival" motif (inversion, Dorian B natural).
for b in range(A2, B):
    arpeggio(b, PROG[b], 54)
cello_a2 = [
    ("D3", 2), ("G3", 2),
    ("A3", 1), ("B3", 1), ("C4", 1), ("D4", 1),
    ("D4", 2), ("C4", 1), ("A3", 1),
    ("G3", 3), ("E3", 1),
    ("F3", 2), ("A3", 2),
    ("B3", 2), ("D4", 2),
    ("A3", 2), ("F3", 1), ("E3", 1),
    ("E3", 2), ("C3", 2),
    ("D3", 4),
    ("E3", 4),
]
cello.line(A2, cello_a2, 78)
# Flute: soft long-tone counterline.
flute.line(A2, [(n, 4) for n in ("A4", "B4", "A4", "G4", "F4", "G4", "A4", "E4", "D4", "E4")], 52)
# Choir enters on bar 3 of A', very soft.
for b in range(A2 + 2, B):
    for p in chord_tones(PROG[b], 4):
        choir.note(b * BAR, p, BAR, 40 + 2 * (b - A2))

# B: glorious and tragic. Flute and cello in octave unison, frame drum, choir swells.
melody_b = [
    ("F4", 1), ("A4", 1), ("C5", 2),
    ("E5", 2), ("D5", 1), ("C5", 1),
    ("D5", 2), ("Bb4", 1), ("G4", 1),
    ("A4", 3), ("C#5", 1),
    ("D5", 1), ("C5", 1), ("A4", 2),
    ("G4", 1), ("A4", 1), ("C5", 2),
    ("Bb4", 2), ("A4", 1), ("G4", 1),
    ("A4", 4),
    ("G4", 2), ("F4", 1), ("E4", 1),
    ("A4", 4),
]
flute.line(B, melody_b, 84)
cello.line(B, melody_b, 86, octave_shift=-1)
for b in range(B, CODA):
    arpeggio(b, PROG[b], 62)
    for p in chord_tones(PROG[b], 4):
        choir.note(b * BAR, p, BAR, 62 if b < B + 8 else 54)
    bass = chord_tones(PROG[b], 2)[0]
    cello.note(b * BAR, bass, 2 * PPQ - 20, 60)  # cello double-stop feel on the downbeat
    # Frame drum: soft hand strokes on 1 and 3, a ghost on the "and" of 4.
    drum.note(b * BAR, 41, PPQ // 2, 58)
    drum.note(b * BAR + 2 * PPQ, 41, PPQ // 2, 48)
    drum.note(b * BAR + 3 * PPQ + PPQ // 2, 45, PPQ // 4, 30)

# Coda: everything falls away; destiny motif on the bouzouki, ending on A to loop.
bouzouki.line(CODA, [("D5", 2), ("A4", 2), ("G4", 1), ("F4", 1), ("E4", 2), ("D4", 4)], 64)
arpeggio(CODA + 3, "Dm", 48)
arpeggio(CODA + 4, "Dm", 42)
arpeggio(CODA + 5, "A", 38)
bouzouki.line(CODA + 6, [("A3", 1), ("E4", 1), ("A4", 2)], 34)

# --- Conductor track: tempo with a little rubato in the intro and coda ---------
conductor = [
    (0, b"\xff\x58\x04\x04\x02\x18\x08"),  # 4/4
    tempo_event(0, 64),
    tempo_event(2 * BAR, 67),
    tempo_event(4 * BAR, 70),
    tempo_event((CODA + 4) * BAR, 66),
    tempo_event((CODA + 6) * BAR, 62),
]

tracks = [chunk(conductor, "The Six Faces - GodBoard main theme")]
for t in (bouzouki, flute, cello, choir, drone, drum):
    tracks.append(chunk(t.events, t.name))

header = b"MThd" + struct.pack(">IHHH", 6, 1, len(tracks), PPQ)
with open(sys.argv[1], "wb") as f:
    f.write(header + b"".join(tracks))
print(f"wrote {sys.argv[1]}: {len(tracks)} tracks, {TOTAL_BARS} bars")
