"""Run opt-in real-graphics reviews of the Windows development build from WSL."""
from pathlib import Path
import subprocess,sys,json
root=Path(__file__).resolve().parents[2]
win=lambda p:subprocess.check_output(['wslpath','-w',str(p)],text=True).strip()
mode=sys.argv[1] if len(sys.argv)>1 else 'journey'
width=int(sys.argv[2]) if len(sys.argv)>2 else 1920
height=int(sys.argv[3]) if len(sys.argv)>3 else 1080
label=sys.argv[4] if len(sys.argv)>4 else f'Dock9_{height}p'
seconds=sys.argv[5] if len(sys.argv)>5 else '600'
output=root/'Docs/Images'/label;output.mkdir(parents=True,exist_ok=True)
logs=root/'Logs/Dock9Graphics';logs.mkdir(parents=True,exist_ok=True)
args=[str(root/'Builds/Vaultbreakers_POC/Vaultbreakers.exe'),'-screen-width',str(width),'-screen-height',str(height),'-screen-fullscreen','0','--poc-review',win(output),'-logFile',win(logs/(label+'.log'))]
args += ['--poc-journey'] if mode=='journey' else ['--poc-seconds',seconds]
args += sys.argv[6:]
result=subprocess.run(args,stdout=subprocess.DEVNULL,stderr=subprocess.STDOUT)
if result.returncode:sys.exit(result.returncode)
metrics=json.loads((output/'metrics.json').read_text());print(label,json.dumps(metrics),flush=True)
if mode=='journey' and not metrics.get('completed'):sys.exit('Scripted route did not finish')
