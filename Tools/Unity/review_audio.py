"""Inspect the exported original cue signals and assemble a listenable review sequence."""
from pathlib import Path
import wave, struct, math, json
folder = Path(__file__).resolve().parents[2]/'Docs/Audio'
rows, sequence = [], []
for path in sorted(folder.glob('*.wav')):
    if path.name == 'ReviewSequence.wav': continue
    with wave.open(str(path), 'rb') as sound:
        assert sound.getparams()[:3] == (1, 2, 22050), path
        frames = sound.readframes(sound.getnframes())
        values = struct.unpack('<'+'h'*(len(frames)//2), frames)
    rows.append({'cue': path.name, 'seconds': len(values)/22050,
                 'peak': max(abs(x) for x in values)/32768,
                 'rms': math.sqrt(sum((x/32768)**2 for x in values)/len(values)),
                 'silentEndpoints': values[0] == 0 and values[-1] == 0})
    sequence.extend(values); sequence.extend([0]*6615)
(folder/'SignalReview.json').write_text(json.dumps({
    'method': 'Exported source cues, PCM signal inspection; does not measure speaker output or in-game mix',
    'cues': rows}, indent=2)+'\n')
with wave.open(str(folder/'ReviewSequence.wav'), 'wb') as sound:
    sound.setparams((1, 2, 22050, 0, 'NONE', 'not compressed'))
    sound.writeframes(struct.pack('<'+'h'*len(sequence), *sequence))
assert rows and all(r['silentEndpoints'] and r['peak'] < 1 and r['rms'] > 0 for r in rows)
print({'cues': len(rows), 'maximumPeak': max(r['peak'] for r in rows), 'allTaperToSilence': True})
