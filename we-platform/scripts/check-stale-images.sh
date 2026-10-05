#!/usr/bin/env bash
# Warn when a running compose service image was built before the latest commit
# that touches that service. Shared libraries are included because service
# Dockerfiles copy we-platform/shared into the image.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

if ! command -v docker >/dev/null 2>&1 || ! docker info >/dev/null 2>&1; then
  echo "WARN stale-image check skipped: docker is not available"
  exit 0
fi

python3 - "$ROOT" <<'PY'
import json
import subprocess
import sys
from datetime import datetime, timezone

root = sys.argv[1]

def run(args):
    return subprocess.check_output(args, cwd=root, text=True).strip()

def parse_ts(value):
    value = value.strip()
    if value.endswith("Z"):
        value = value[:-1] + "+00:00"
    if "." in value:
        head, tail = value.split(".", 1)
        digits = []
        rest = ""
        for index, char in enumerate(tail):
            if char.isdigit():
                digits.append(char)
                continue
            rest = tail[index:]
            break
        fraction = "".join(digits[:6])
        value = head + (("." + fraction) if fraction else "") + rest
    parsed = datetime.fromisoformat(value)
    if parsed.tzinfo is None:
        parsed = parsed.replace(tzinfo=timezone.utc)
    return parsed

config = json.loads(run(["docker", "compose", "config", "--format", "json"]))
warnings = 0

for name, spec in sorted(config.get("services", {}).items()):
    if "build" not in spec:
        continue

    ps = subprocess.run(
        ["docker", "compose", "ps", "-q", name],
        cwd=root,
        text=True,
        capture_output=True,
    )
    container_ids = [line for line in ps.stdout.splitlines() if line.strip()]
    if not container_ids:
        print(f"WARN {name} is not running; cannot compare its image to git")
        warnings += 1
        continue

    image_id = run(["docker", "inspect", "-f", "{{.Image}}", container_ids[0]])
    created_raw = run(["docker", "image", "inspect", "-f", "{{.Created}}", image_id])
    image_created = parse_ts(created_raw)

    commit = subprocess.run(
        ["git", "log", "-1", "--format=%cI", "--", f"services/{name}", "shared"],
        cwd=root,
        text=True,
        capture_output=True,
    ).stdout.strip()
    if not commit:
        print(f"OK   {name} has no commits under services/{name} or shared")
        continue

    commit_time = parse_ts(commit)
    if image_created < commit_time:
        print(
            f"WARN {name} image ({created_raw}) is older than the latest commit "
            f"touching services/{name} or shared ({commit})"
        )
        warnings += 1
    else:
        print(f"OK   {name} image is current")

if warnings:
    print(
        f"{warnings} service image(s) look stale. "
        "Rebuild with: docker compose up -d --build"
    )
else:
    print("No stale service images detected.")
PY
