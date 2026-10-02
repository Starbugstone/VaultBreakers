"""Capture actual development-player frames, then encode a silent gameplay review MP4.

Performance validation must run separately: frame recording changes simulation timing.
Usage: python3 Tools/Unity/video.py AA_Overhaul
"""
from pathlib import Path
import argparse
import shutil
import subprocess
import sys

root = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('label', nargs='?', default='AA_Overhaul')
parser.add_argument('--encode-only', action='store_true', help='Encode an existing recording without rerunning Unity.')
parser.add_argument('--end-frame', type=int, help='Exclusive final frame; trim diagnostic teardown from a reviewed capture.')
args = parser.parse_args()
label = args.label
frames = root / 'Logs' / label / 'VideoFrames'
if not args.encode_only and frames.exists() and any(frames.iterdir()):
    raise SystemExit('Choose a new label; an existing recording will not be overwritten.')
encoder = shutil.which('ffmpeg')
if not encoder:
    raise SystemExit('FFmpeg is required to encode the captured image sequence.')
frames.mkdir(parents=True, exist_ok=True)
win = subprocess.check_output(['wslpath', '-w', str(frames)], text=True).strip()
if not args.encode_only:
    subprocess.run([sys.executable, str(root/'Tools/Unity/review.py'), 'journey', '1280', '720',
                    label+'_Video', '120', '--poc-policy', '4', '--poc-video', win], check=True)
filters = []
if args.end_frame is not None:
    if args.end_frame <= 0 or not (frames/f'{args.end_frame-1:06}.png').exists():
        raise SystemExit('The requested final frame is absent or invalid.')
    filters = ['-vf', f'trim=end_frame={args.end_frame},setpts=PTS-STARTPTS,fps=30,tpad=stop_mode=clone:stop_duration=1,fade=t=out:st={args.end_frame/30+.6}:d=0.4']
output = root / 'Docs' / 'Videos'
output.mkdir(parents=True, exist_ok=True)
subprocess.run([encoder, '-y', '-framerate', '30', '-i', str(frames/'%06d.png'), *filters,
                '-r', '30', '-c:v', 'libx264', '-preset', 'medium', '-threads', '4', '-crf', '19', '-pix_fmt', 'yuv420p',
                '-movflags', '+faststart', str(output/(label+'.mp4'))], check=True)
print(output/(label+'.mp4'))
