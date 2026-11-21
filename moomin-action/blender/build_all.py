"""모든 모델을 한 번에 다시 만들어 exports/ 에 저장. 사용: python build_all.py [--preview]
(필요: pip install bpy==5.3.0, Python 3.13)"""
import subprocess, sys, os
here = os.path.dirname(os.path.abspath(__file__))
for s in ("build_moomin.py", "build_sword.py", "build_enemy.py"):
    subprocess.check_call([sys.executable, os.path.join(here, s)] + sys.argv[1:])
