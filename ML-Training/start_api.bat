@echo off
echo Starting Recruitment AI ML API...
cd /d %~dp0
python -m uvicorn api.app:app --host 0.0.0.0 --port 8000 --reload
pause