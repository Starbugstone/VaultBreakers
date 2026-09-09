"""Run the pinned Windows Unity editor from WSL; logs stay outside source control."""
from pathlib import Path
import subprocess, sys, os, xml.etree.ElementTree as ET
root = Path(__file__).resolve().parents[2]
editor = '/mnt/d/Unity/Hub/6000.4.4f1/Editor/Unity.exe'
win = lambda p: subprocess.check_output(['wslpath', '-w', str(p)], text=True).strip()
label = sys.argv[1] if len(sys.argv) > 1 else 'POC'
logs = root / 'Logs' / label
logs.mkdir(parents=True, exist_ok=True)
steps = sys.argv[2:] or ['setup', 'EditMode', 'PlayMode', 'build']
for step in steps:
    args = [editor, '-batchmode', '-nographics', '-job-worker-count', os.environ.get('VB_UNITY_WORKERS', '2'), '-projectPath', win(root), '-logFile', win(logs / (step + '.log'))]
    if step == 'setup': args += ['-quit', '-executeMethod', 'Vaultbreakers.Editor.VaultbreakersProjectSetup.BuildAll']
    elif step == 'build':
        target = root / 'Builds' / 'Vaultbreakers_POC' / 'Vaultbreakers.exe'
        target.parent.mkdir(parents=True, exist_ok=True)
        args += ['-quit', '-executeMethod', 'Vaultbreakers.Editor.VaultbreakersPocReview.BuildWindows']
    else: args += ['-runTests', '-testPlatform', step, '-testResults', win(logs / (step + '.xml'))]
    if step in ['EditMode', 'PlayMode'] and os.environ.get('VB_TEST_FILTER'):args += ['-testFilter', os.environ['VB_TEST_FILTER']]
    print('Starting', step, flush=True)
    result = subprocess.run(args, stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT)
    print(step, 'exit', result.returncode, flush=True)
    if result.returncode: sys.exit(result.returncode)
    if step in ['EditMode', 'PlayMode']:
        data = ET.parse(logs / (step + '.xml')).getroot()
        print({key: data.get(key) for key in ['result', 'total', 'passed', 'failed', 'skipped']}, flush=True)
        if data.get('result') != 'Passed': sys.exit(1)
