"""Renders the GodBoard theme MIDI to a seamless-loop stereo WAV (pure Python, stdlib only).

Instruments are modeled, not sampled: Karplus-Strong double courses for the bouzouki,
wavetables with breath noise and delayed vibrato for flute and cello, formant-shaped
"ah" voices for the choir, a buzzing drone and a pitched-down frame drum.
Usage: python3 render_theme.py <in.mid> <out.wav>
"""

import math
import random
import struct
import sys
import wave

SR = 24000
TWO_PI = 2 * math.pi
rng = random.Random(3)


# --- MIDI parsing ------------------------------------------------------------
def read_midi(path):
    d = open(path, "rb").read()
    ntracks, ppq = struct.unpack(">H", d[10:12])[0], struct.unpack(">H", d[12:14])[0]
    p, tracks, tempos = 14, [], []
    for _ in range(ntracks):
        ln = struct.unpack(">I", d[p + 4 : p + 8])[0]
        q, end, t, name, on, notes = p + 8, p + 8 + ln, 0, "", {}, []
        while q < end:
            v = 0
            while True:
                b = d[q]
                q += 1
                v = (v << 7) | (b & 0x7F)
                if b < 0x80:
                    break
            t += v
            s = d[q]
            if s == 0xFF:
                typ, l = d[q + 1], d[q + 2]
                if typ == 3:
                    name = d[q + 3 : q + 3 + l].decode()
                if typ == 0x51:
                    tempos.append((t, int.from_bytes(d[q + 3 : q + 6], "big")))
                q += 3 + l
                continue
            hi = s & 0xF0
            if hi in (0xC0, 0xD0):
                q += 2
                continue
            pitch, vel = d[q + 1], d[q + 2]
            q += 3
            if hi == 0x90 and vel > 0:
                on.setdefault(pitch, []).append((t, vel))
            elif hi in (0x80, 0x90) and on.get(pitch):
                t0, v0 = on[pitch].pop(0)
                notes.append((t0, t, pitch, v0))
        tracks.append((name, notes))
        p = end
    return ppq, sorted(tempos), tracks, t


def tick_to_sec_fn(ppq, tempos):
    tempos = tempos or [(0, 500000)]

    def conv(tick):
        sec, last_t, last_us = 0.0, 0, tempos[0][1]
        for t, us in tempos:
            if t >= tick:
                break
            sec += (t - last_t) * last_us / ppq / 1e6
            last_t, last_us = t, us
        return sec + (tick - last_t) * last_us / ppq / 1e6

    return conv


def hz(pitch):
    return 440.0 * 2 ** ((pitch - 69) / 12)


# --- Instruments (each returns a mono list) ---------------------------------------
def table(weights, size=2048):
    tab = [0.0] * size
    for k, w in enumerate(weights, start=1):
        ph = rng.random() * TWO_PI
        for i in range(size):
            tab[i] += w * math.sin(TWO_PI * k * i / size + ph)
    peak = max(abs(x) for x in tab)
    return [x / peak for x in tab]


FLUTE_TAB = table([1, 0.42, 0.14, 0.06, 0.025])
CELLO_TAB = table([1, 0.85, 0.62, 0.55, 0.42, 0.30, 0.26, 0.18, 0.14, 0.10, 0.08, 0.06, 0.04, 0.03])
DRONE_TAB = table([1 / k ** 0.8 for k in range(1, 18)])


def osc(tab, f, n, vib_depth, vib_rate, vib_delay, drift=0.0):
    size = len(tab)
    out = [0.0] * n
    ph = rng.random() * size
    vph = rng.random() * TWO_PI
    for i in range(n):
        t = i / SR
        ramp = min(1.0, max(0.0, (t - vib_delay) / 0.5))
        fi = f * (1 + vib_depth * ramp * math.sin(vph + TWO_PI * vib_rate * t) + drift * math.sin(0.7 * t))
        ph += fi * size / SR
        if ph >= size:
            ph -= size * int(ph / size)
        out[i] = tab[int(ph)]
    return out


def envelope(n, attack, release, swell=0.0):
    out = [0.0] * n
    a, r = max(1, int(attack * SR)), max(1, int(release * SR))
    for i in range(n):
        e = min(1.0, i / a) * min(1.0, (n - i) / r)
        out[i] = e * (1 + swell * math.sin(math.pi * i / n))
    return out


def lowpass(x, a):
    y, out = 0.0, [0.0] * len(x)
    for i, v in enumerate(x):
        y += a * (v - y)
        out[i] = y
    return out


def flute(f, dur, amp):
    n = int((dur + 0.15) * SR)
    tone = osc(FLUTE_TAB, f, n, 0.0045, 5.0, 0.35)
    env = envelope(n, 0.07, 0.15, swell=0.12)
    breath = lowpass([rng.uniform(-1, 1) for _ in range(n)], 0.25)
    chiff = int(0.06 * SR)
    return [amp * env[i] * (tone[i] + breath[i] * (0.55 if i < chiff else 0.06)) for i in range(n)]


def cello(f, dur, amp):
    n = int((dur + 0.25) * SR)
    tone = osc(CELLO_TAB, f, n, 0.006, 5.4, 0.22)
    tone = lowpass(tone, 0.35)
    env = envelope(n, 0.13, 0.25, swell=0.15)
    bow = lowpass([rng.uniform(-1, 1) for _ in range(n)], 0.12)
    return [amp * env[i] * (tone[i] + 0.10 * bow[i]) for i in range(n)]


def drone(f, dur, amp):
    n = int(dur * SR)
    a = osc(DRONE_TAB, f, n, 0.0, 0.0, 0.0, drift=0.0015)
    b = osc(DRONE_TAB, f * 1.003, n, 0.0, 0.0, 0.0, drift=0.0015)
    env = envelope(n, 2.0, 2.0)
    wheel = [1 + 0.08 * math.sin(TWO_PI * 3.1 * i / SR) for i in range(n)]
    return lowpass([amp * env[i] * wheel[i] * (a[i] + b[i]) * 0.5 for i in range(n)], 0.3)


def choir_table(f):
    formants = ((700, 110, 1.0), (1150, 130, 0.55), (2800, 250, 0.25))
    weights = []
    k = 1
    while k * f < SR / 2.5 and k <= 40:
        h = k * f
        weights.append(sum(g * math.exp(-((h - c) / w) ** 2 / 2) for c, w, g in formants) + 0.15 / k)
        k += 1
    return table(weights, 1024)


def choir(f, dur, amp):
    n = int((dur + 0.9) * SR)
    tab = choir_table(f)
    out = [0.0] * n
    for cents in (-7, 0, 6):
        v = osc(tab, f * 2 ** (cents / 1200), n, 0.004, 4.6 + rng.random(), 0.1)
        for i in range(n):
            out[i] += v[i]
    env = envelope(n, 0.9, 1.0)
    return [amp * env[i] * out[i] / 3 for i in range(n)]


def pluck_string(f, n, bright):
    period = max(2, int(SR / f))
    line = lowpass([rng.uniform(-1, 1) for _ in range(period)], bright)
    m = sum(line) / period
    line = [x - m for x in line]
    out, idx, prev = [0.0] * n, 0, 0.0
    decay = 0.996 if f < 300 else 0.993
    for i in range(n):
        cur = line[idx]
        out[i] = cur
        line[idx] = decay * 0.5 * (cur + prev)
        prev = cur
        idx += 1
        if idx == period:
            idx = 0
    return out


def bouzouki(f, dur, amp, vel):
    n = int((dur + 0.9) * SR)
    bright = 0.35 + 0.5 * vel / 127
    a = pluck_string(f, n, bright)
    b = pluck_string(f * 1.0025, n, bright)
    octave = pluck_string(f * 2, n, bright) if f < 220 else None
    fade = envelope(n, 0.001, 0.4)
    out = [0.0] * n
    for i in range(n):
        s = a[i] + 0.8 * b[i] + (0.45 * octave[i] if octave else 0.0)
        out[i] = amp * fade[i] * s
    return out


def frame_drum(pitch, amp):
    n = int(0.6 * SR)
    f0, f1 = (95, 62) if pitch == 41 else (180, 120)
    out, ph = [0.0] * n, 0.0
    noise = lowpass([rng.uniform(-1, 1) for _ in range(n)], 0.2)
    for i in range(n):
        t = i / SR
        ph += TWO_PI * (f1 + (f0 - f1) * math.exp(-t * 25)) / SR
        out[i] = amp * (math.sin(ph) * math.exp(-t * 7) + noise[i] * 0.6 * math.exp(-t * 60))
    return out


# --- Mix -----------------------------------------------------------------------
MIX = {  # track name: (gain, pan -1..1)
    "Bouzouki": (0.40, -0.30),
    "Low flute": (0.30, 0.25),
    "Cello": (0.42, -0.10),
    "Choir": (0.10, 0.0),
    "Hurdy-gurdy drone": (0.12, 0.05),
    "Frame drum": (0.55, 0.15),
}


def reverb_send(src, combs, allpasses, tail):
    n = len(src)
    total = n + tail
    out = [0.0] * total
    for delay, fb in combs:
        line, idx, lp = [0.0] * delay, 0, 0.0
        for i in range(total):
            y = line[idx]
            lp += 0.45 * (y - lp)  # damping: darker stone-hall tail
            line[idx] = (src[i] if i < n else 0.0) + lp * fb
            out[i] += y * 0.25
            idx += 1
            if idx == delay:
                idx = 0
    for delay, g in allpasses:
        line, idx = [0.0] * delay, 0
        for i in range(total):
            x = out[i]
            y = line[idx]
            line[idx] = x + y * g
            out[i] = y - g * x
            idx += 1
            if idx == delay:
                idx = 0
    # wrap the tail onto the start so the loop point is seamless
    wet = out[:n]
    for i in range(tail):
        wet[i % n] += out[n + i]
    return wet


def main(src, dst):
    ppq, tempos, tracks, _ = read_midi(src)
    sec = tick_to_sec_fn(ppq, tempos)
    length = int(sec(43 * 4 * ppq) * SR)
    left, right = [0.0] * length, [0.0] * length
    send = [0.0] * length

    for name, notes in tracks:
        if name not in MIX:
            continue
        gain, pan = MIX[name]
        lg, rg = math.cos((pan + 1) * math.pi / 4), math.sin((pan + 1) * math.pi / 4)
        for t0, t1, pitch, vel in notes:
            start, dur = sec(t0), sec(t1) - sec(t0)
            amp = gain * (vel / 127) ** 1.6
            f = hz(pitch)
            if name == "Bouzouki":
                sig = bouzouki(f, dur, amp, vel)
            elif name == "Low flute":
                sig = flute(f, dur, amp)
            elif name == "Cello":
                sig = cello(f, dur, amp)
            elif name == "Choir":
                sig = choir(f, dur, amp)
            elif name == "Hurdy-gurdy drone":
                sig = drone(f, dur, amp)
            else:
                sig = frame_drum(pitch, amp)
            s0 = int(start * SR)
            for i, v in enumerate(sig):
                j = (s0 + i) % length  # notes ringing past the end wrap into the start
                left[j] += v * lg
                right[j] += v * rg
                send[j] += v
        print("rendered", name, len(notes), "notes", flush=True)

    tail = int(4.5 * SR)
    wet_l = reverb_send(send, [(1116, 0.86), (1188, 0.85), (1277, 0.84), (1356, 0.83)], [(225, 0.5), (556, 0.5)], tail)
    wet_r = reverb_send(send, [(1139, 0.86), (1211, 0.85), (1300, 0.84), (1379, 0.83)], [(248, 0.5), (579, 0.5)], tail)
    mix_l = [left[i] + 0.45 * wet_l[i] for i in range(length)]
    mix_r = [right[i] + 0.45 * wet_r[i] for i in range(length)]

    peak = max(max(abs(x) for x in mix_l), max(abs(x) for x in mix_r))
    scale = 0.89 / peak  # about -1 dBFS
    frames = bytearray()
    for a, b in zip(mix_l, mix_r):
        frames += struct.pack("<hh", int(a * scale * 32767), int(b * scale * 32767))
    with wave.open(dst, "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(bytes(frames))
    rms = math.sqrt(sum((x * scale) ** 2 for x in mix_l) / length)
    print(f"wrote {dst}: {length / SR:.1f}s, rms {20 * math.log10(rms):.1f} dBFS")


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
