#!/usr/bin/env python3
"""Перевіряє ізольований Development API/БД; створює лише синтетичні demo-виміри."""
import datetime
import http.client
import json
import sys
import time
import urllib.error
import urllib.request

base = (sys.argv[1] if len(sys.argv) > 1 else "http://127.0.0.1:5195").rstrip("/")
if not base.startswith(("http://127.0.0.1:", "http://localhost:")):
    raise SystemExit("Smoke призначений лише для локального Development API.")


def request(path, method="GET", data=None, token=None, device=False):
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = "Bearer " + token
    if device:
        headers["X-Device-Key"] = "test-device-key-12345"
    body = None if data is None else json.dumps(data).encode()
    try:
        response = urllib.request.urlopen(urllib.request.Request(base + path, body, headers, method=method), timeout=10)
    except urllib.error.HTTPError as error:
        raise RuntimeError(f"{method} {path}: HTTP {error.code}: {error.read().decode()}") from None
    with response:
        raw = response.read()
        return json.loads(raw) if raw else None


for attempt in range(60):
    try:
        with urllib.request.urlopen(base + "/health", timeout=2) as response:
            if response.status == 200:
                break
    except (OSError, http.client.HTTPException):
        # Поки контейнер/міграції стартують, порт може reset-нути вже прийняте з'єднання.
        pass
    time.sleep(1)
else:
    raise SystemExit("API не став готовим за 60 перевірок.")


token = request("/api/v1/auth/login", "POST", {"email": "admin@microclimate.local", "password": "AdminPassword123!"})["accessToken"]
rooms = request("/api/v1/rooms", token=token)["items"]
assert rooms, "Демонстраційна кімната відсутня"
room = rooms[0]["id"]
root = "/api/v1/rooms/" + room
sensor = request(root + "/sensors", token=token)[0]["id"]
thresholds = request(root + "/thresholds", token=token)
assert len(thresholds) == 6
saved = request(root + "/thresholds/temperature", "PUT", {"warningMin": 20, "warningMax": 25, "criticalMin": 16, "criticalMax": 28}, token)
assert saved["warningMax"] == 25
now = datetime.datetime.now(datetime.timezone.utc)
measured_at = (now - datetime.timedelta(seconds=1)).isoformat()
request("/api/v1/measurements", "POST", {"sensorId": sensor, "items": [
    {"metric": 1, "value": 22.5, "measuredAt": measured_at},
    {"metric": 3, "value": 1900, "measuredAt": measured_at},
]}, device=True)
latest = request(root + "/measurements/latest", token=token)
assert any(item["value"] == 22.5 for item in latest)
comfort = request(root + "/analytics/comfort", token=token)
assert comfort["index"] is not None
alerts = request("/api/v1/alerts?status=open&level=critical", token=token)["items"]
assert alerts, "Критичне сповіщення не створено"
request("/api/v1/alerts/" + alerts[0]["id"] + "/acknowledge", "POST", token=token)
assert request("/api/v1/alerts?status=acknowledged", token=token)["items"]
print("PASS: health, JWT, rooms, sensors, thresholds, ingestion, latest values, comfort, alerts, acknowledge")
