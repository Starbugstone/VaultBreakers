"""Package the tested Windows folder, notices and controls; verify ZIP integrity."""
from pathlib import Path
import hashlib, json, subprocess, zipfile
root = Path(__file__).resolve().parents[2]
build = root/'Builds/Vaultbreakers_POC'
assert (build/'Vaultbreakers.exe').stat().st_size > 0
output = root/'Builds/Vaultbreakers_Dock9_POC_Windows.zip'
readme = '''VAULTBREAKERS - DOCK 9 COMBAT POC
Extract this entire archive before running Vaultbreakers.exe. Windows x64.
Play_720p.bat and Play_1080p.bat select a window size. Use 720p when other games/editors are active.
Three connected zones: clear the guardians, cross the open gates, recover the final vault core.
Rooms are 24 x 24 metres; enemy HP is Grunt 18 / Shooter 14 / Bruiser 42.

Controller: left stick move, right stick optional aim, X melee, RT fire, LT shield, A dodge.
Start pauses during combat and starts a new run after the final clear.
Keyboard/mouse: WASD move, mouse/arrow aim, left mouse melee, E fire, right mouse shield,
Space dodge, Escape pause, R retry/replay. Hold melee/fire for repeated attacks.
Shield excludes fire. Melee lowers guard. Dodge cancels attacks/guard.
Death retries the current room; health and guard restore between rooms.
Pause offers feedback settings and checkpoint/new-run controls. F1 opens the development lab.

This is the combat POC: no persistent inventory, gems/gauges, Relic powers, scanning or pets.
Automated/controller-policy evidence and known limits: Docs/POLISH_PASSES.md in the source repo.
Original project materials: Copyright (c) 2026 StarbugStone. All rights reserved.
'''
for width, height in [(1280, 720), (1920, 1080)]:
    (build/f'Play_{height}p.bat').write_text(f'@echo off\nstart "" "%~dp0Vaultbreakers.exe" -screen-width {width} -screen-height {height} -screen-fullscreen 0\n', newline='\r\n')
with zipfile.ZipFile(output, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
    for file in sorted(build.rglob('*')):
        if file.is_file() and not any('DoNotShip' in part for part in file.parts):
            archive.write(file, 'Vaultbreakers_POC/'+str(file.relative_to(build)))
    archive.writestr('Vaultbreakers_POC/START_HERE.txt', readme)
    archive.write(root/'LICENSE', 'Vaultbreakers_POC/LICENSE')
    for file in sorted((root/'LICENSES').rglob('*')):
        if file.is_file(): archive.write(file, 'Vaultbreakers_POC/LICENSES/'+str(file.relative_to(root/'LICENSES')))
with zipfile.ZipFile(output) as archive:
    assert archive.testzip() is None
    count = len(archive.infolist())
sha = hashlib.sha256(output.read_bytes()).hexdigest()
output.with_suffix('.sha256').write_text(sha+'  '+output.name+'\n')
report = {'sourceCommit': subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root, text=True).strip(),
          'archive': output.name, 'bytes': output.stat().st_size, 'sha256': sha, 'entries': count,
          'zipCrcVerified': True, 'excluded': 'Generated Burst debug information marked DoNotShip'}
(root/'Docs/Validation/BuildPackage.json').write_text(json.dumps(report, indent=2)+'\n')
print(json.dumps(report), flush=True)
