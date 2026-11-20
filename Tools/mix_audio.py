import wave
import struct
import math

TARGET_SR = 44100
DURATION = 10.0
TOTAL_FRAMES = int(TARGET_SR * DURATION)

# Buffer: left and right channels as float
left_buf = [0.0] * TOTAL_FRAMES
right_buf = [0.0] * TOTAL_FRAMES

def load_wav(path):
    w = wave.open(path, 'rb')
    nch, sampwidth, sr, nframes = w.getparams()[:4]
    raw = w.readframes(nframes)
    w.close()
    fmt = '<' + 'h' * (len(raw) // 2)
    raw_samples = struct.unpack(fmt, raw)
    if nch == 1:
        # Mono
        samples = [(s / 32768.0, s / 32768.0) for s in raw_samples]
    else:
        # Stereo
        samples = [(raw_samples[i*2] / 32768.0, raw_samples[i*2+1] / 32768.0) for i in range(len(raw_samples)//2)]
    return sr, samples

def mix_clip(path, start_sec, volume=1.0, fade_out=0.0):
    sr, samples = load_wav(path)
    start_frame = int(start_sec * TARGET_SR)
    n_src = len(samples)
    ratio = sr / TARGET_SR
    
    for i in range(TOTAL_FRAMES - start_frame):
        out_idx = start_frame + i
        if out_idx >= TOTAL_FRAMES:
            break
        src_pos = i * ratio
        src_idx = int(src_pos)
        if src_idx >= n_src - 1:
            break
        frac = src_pos - src_idx
        l0, r0 = samples[src_idx]
        l1, r1 = samples[src_idx + 1]
        l = l0 + frac * (l1 - l0)
        r = r0 + frac * (r1 - r0)
        
        # Apply fade out if near end of overall track
        time_left = (TOTAL_FRAMES - out_idx) / TARGET_SR
        v = volume
        if fade_out > 0 and time_left < fade_out:
            v *= (time_left / fade_out)
            
        left_buf[out_idx] += l * v
        right_buf[out_idx] += r * v

# 1. Background Music (The Six Faces) - starts at 0s, 35% vol, fade out in last 1.2s
print("Mixing music...")
mix_clip("Assets/_Project/Audio/music-the-six-faces.wav", start_sec=0.0, volume=0.32, fade_out=1.2)

# 2. SFX Cues
print("Mixing SFX...")
mix_clip("Assets/_Project/Audio/sfx-troops-gain.wav", start_sec=0.2, volume=0.85)
mix_clip("Assets/_Project/Audio/sfx-card-hover.wav", start_sec=1.3, volume=0.7)
mix_clip("Assets/_Project/Audio/dice-roll.wav", start_sec=2.4, volume=0.95)
mix_clip("Assets/_Project/Audio/sacred-chime.wav", start_sec=3.6, volume=1.0)
mix_clip("Assets/_Project/Audio/sfx-card-hover.wav", start_sec=5.2, volume=0.7)
mix_clip("Assets/_Project/Audio/sfx-wrath-zeus.wav", start_sec=6.3, volume=1.0)
mix_clip("Assets/_Project/Audio/sfx-soft-good.wav", start_sec=7.7, volume=0.8)
mix_clip("Assets/_Project/Audio/victory-fanfare.wav", start_sec=8.6, volume=0.95)

# Master limiter / clipping guard
max_amp = 0.0
for i in range(TOTAL_FRAMES):
    max_amp = max(max_amp, abs(left_buf[i]), abs(right_buf[i]))

norm = 1.0
if max_amp > 0.95:
    norm = 0.95 / max_amp
print(f"Max amplitude: {max_amp:.3f}, normalizing factor: {norm:.3f}")

out_samples = []
for i in range(TOTAL_FRAMES):
    l = int(max(-32767, min(32767, left_buf[i] * norm * 32767)))
    r = int(max(-32767, min(32767, right_buf[i] * norm * 32767)))
    out_samples.append(l)
    out_samples.append(r)

out_wav = wave.open("Tools/soundtrack_10s.wav", "wb")
out_wav.setnchannels(2)
out_wav.setsampwidth(2)
out_wav.setframerate(TARGET_SR)
out_wav.writeframes(struct.pack('<' + 'h' * len(out_samples), *out_samples))
out_wav.close()
print("Saved Tools/soundtrack_10s.wav successfully!")
