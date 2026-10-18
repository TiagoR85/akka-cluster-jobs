#!/usr/bin/env bash
set -euo pipefail

NODE1="http://localhost:18081"
NODE2="http://localhost:18082"
NODE3="http://localhost:18083"
EXPECTED=60
TIMEOUT=180

echo "== Waiting for all nodes to be healthy =="
for url in "$NODE1" "$NODE2" "$NODE3"; do
  for i in $(seq 1 $TIMEOUT); do
    if curl -sf "$url/health" > /dev/null 2>&1; then
      echo "  $url is healthy"
      break
    fi
    if [ "$i" -eq "$TIMEOUT" ]; then
      echo "FAIL: $url not healthy after ${TIMEOUT}s"
      docker compose logs
      exit 1
    fi
    sleep 1
  done
done

echo "== Seeding $EXPECTED jobs =="
resp=$(curl -sf -X POST "$NODE1/jobs/seed?count=$EXPECTED&prefix=e2e")
echo "  seed response: $resp"

echo "== Waiting for convergence (done==$EXPECTED on all nodes) =="
converged=0
for i in $(seq 1 $TIMEOUT); do
  d1=$(curl -sf "$NODE1/jobs/stats" | python3 -c "import sys,json; print(json.load(sys.stdin).get('done',0))" 2>/dev/null || echo "0")
  d2=$(curl -sf "$NODE2/jobs/stats" | python3 -c "import sys,json; print(json.load(sys.stdin).get('done',0))" 2>/dev/null || echo "0")
  d3=$(curl -sf "$NODE3/jobs/stats" | python3 -c "import sys,json; print(json.load(sys.stdin).get('done',0))" 2>/dev/null || echo "0")
  total=$((d1 + d2 + d3))
  echo "  poll $i: done=$total (n1=$d1 n2=$d2 n3=$d3)"

  if [ "$total" -ge "$EXPECTED" ]; then
    converged=$((converged + 1))
    if [ "$converged" -ge 2 ]; then
      echo "PASS: convergence achieved (done=$total >= $EXPECTED, 2 consecutive polls)"
      exit 0
    fi
  else
    converged=0
  fi
  sleep 1
done

echo "FAIL: no convergence after ${TIMEOUT}s"
docker compose logs
exit 1