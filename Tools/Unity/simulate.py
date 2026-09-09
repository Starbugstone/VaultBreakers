"""Five labelled virtual-controller policies; never substitute results for human preference."""
from pathlib import Path
import json, subprocess, sys
root = Path(__file__).resolve().parents[2]
rows = []
for policy, name in enumerate(['MeleeFirst', 'RangedPressure', 'GuardApproach', 'Evasive', 'Mixed']):
    label = 'Dock9_Controller_' + name
    result = subprocess.run([sys.executable, str(root/'Tools/Unity/review.py'), 'journey',
                             '1920', '1080', label, '120', '--poc-policy', str(policy)])
    folder = root/'Docs/Images'/label
    session = json.loads((folder/'session.json').read_text()) if (folder/'session.json').exists() else {}
    metrics = json.loads((folder/'metrics.json').read_text()) if (folder/'metrics.json').exists() else {}
    passed = result.returncode == 0 and session.get('completed') and session.get('replayReset') and session.get('shotsWhileGuardRaised') == 0
    rows.append({'name': name, 'evidence': str(folder.relative_to(root)), 'passed': bool(passed),
                 'session': session, 'metrics': metrics})
    print(name, json.dumps(session), flush=True)
report = {'method': 'Five programmed policies using a virtual Gamepad with normal health/damage. '
          'World-state targeting is privileged. No human comprehension, preference or physical-device claims.', 'runs': rows}
(root/'Docs/Validation/ControllerPolicies.json').write_text(json.dumps(report, indent=2)+'\n')
sys.exit(0 if all(r['passed'] for r in rows) else 1)
