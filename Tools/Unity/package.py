"""Package the tested Windows folder, notices and controls; verify ZIP integrity."""
from pathlib import Path
import argparse, hashlib, json, subprocess, zipfile
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--alpha', action='store_true', help='Package the gem/loot alpha without replacing the historical combat POC ZIP.')
args = parser.parse_args()
root = Path(__file__).resolve().parents[2]
build = root/'Builds/Vaultbreakers_POC'
assert (build/'Vaultbreakers.exe').stat().st_size > 0
output = root/'Builds'/('Vaultbreakers_Alpha_0.0.1_Windows.zip' if args.alpha else 'Vaultbreakers_Dock9_POC_Windows.zip')
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
if args.alpha:
    readme = '''VAULTBREAKERS - ALPHA 0.0.1 / DOCK 9
Extract this entire archive before running Vaultbreakers.exe. Windows x64.
Play_720p.bat and Play_1080p.bat select a window size.
Three connected rooms: defeat the guardians, gather gems, cross the gate, claim the final core.

Controller: left stick move, right stick optional aim, hold X melee, RT fire, LT guard, A dodge.
Start pauses or replays after the final clear. The pause menu includes Quit Game.
Keyboard/mouse: WASD move, mouse/arrows aim, hold left mouse melee, E fire, right mouse guard,
Space dodge, Escape pause, R retry/replay. Guard prevents firing; melee lowers guard.

Enemies drop multiple gems. Break the orange crates and teal chests marked with gold diamonds
using your hammer or gun. Walk near gems to attract and collect them; no button is needed.
Broken lids, panels and frame pieces shrink and fade away within one second.
The exit unlocks after a short collection window. Gold gives salvage score; energy gems are
counted for this mechanics test. No gauges, powers, upgrades or persistent inventory yet.
Gems bank when entering the next room. Death/retry resets the current room's loot and caches;
replay starts from zero. Grey industrial scenery and the final objective cache are separate.

This is a development build for mechanics testing. F1 opens the combat lab.
Known limits: stable 60 FPS and physical controller feel are not certified. A buffer-disposal
warning can appear at shutdown. Test evidence and exact scope: Docs/ALPHA_0_0_1.md in the source.
Original materials: Copyright (c) 2026 StarbugStone. All rights reserved.
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
    payload_hashes = {}
    for relative in ('Vaultbreakers.exe', 'Vaultbreakers_Data/Managed/Vaultbreakers.Runtime.dll'):
        tested = (build/relative).read_bytes()
        assert archive.read('Vaultbreakers_POC/'+relative) == tested, 'Packaged player differs from the tested build'
        payload_hashes[relative] = hashlib.sha256(tested).hexdigest()
sha = hashlib.sha256(output.read_bytes()).hexdigest()
output.with_suffix('.sha256').write_text(sha+'  '+output.name+'\n')
report = {'sourceCommit': subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root, text=True).strip(),
          'sourceState': 'Build inputs match sourceCommit',
          'archive': output.name, 'bytes': output.stat().st_size, 'sha256': sha, 'entries': count,
          'zipCrcVerified': True, 'payloadMatchesBuild': True, 'payloadSha256': payload_hashes,
          'excluded': 'Generated Burst debug information marked DoNotShip'}
input_changes = subprocess.check_output(
    ['git', 'status', '--porcelain', '--untracked-files=all', '--',
     'Assets', 'ArtSource', 'Packages', 'ProjectSettings', 'Tools'], cwd=root, text=True).splitlines()
if input_changes:
    report['sourceState'] = 'Working tree build; build inputs differ from sourceCommit'
    report['sourceInputChanges'] = input_changes
(root/'Docs/Validation'/('Alpha001_BuildPackage.json' if args.alpha else 'BuildPackage.json')).write_text(json.dumps(report, indent=2)+'\n')
print(json.dumps(report), flush=True)
