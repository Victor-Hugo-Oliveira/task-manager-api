#!/usr/bin/env bash
# End-to-end smoke test against a running API (e.g. `docker compose up`).
# Usage: scripts/smoke-test.sh [base-url]     (default http://localhost:8080)
# Requires: curl, jq
set -euo pipefail

BASE_URL="${1:-http://localhost:8080}"

fail() { echo "FAIL: $*" >&2; exit 1; }

expect_status() { # expected actual step
  [[ "$2" == "$1" ]] || fail "$3: expected HTTP $1, got $2"
  echo "ok  $3 ($2)"
}

echo "Waiting for $BASE_URL/health ..."
for _ in $(seq 1 60); do
  if [[ "$(curl -s -o /dev/null -w '%{http_code}' "$BASE_URL/health" || true)" == "200" ]]; then break; fi
  sleep 2
done
expect_status 200 "$(curl -s -o /dev/null -w '%{http_code}' "$BASE_URL/health")" "health check"

EMAIL="smoke-$(date +%s)-$RANDOM@example.com"
BODY=$(jq -n --arg e "$EMAIL" '{name:"Smoke Test", email:$e, password:"Password123"}')

RESPONSE=$(curl -s -w '\n%{http_code}' -X POST "$BASE_URL/api/auth/register" -H 'Content-Type: application/json' -d "$BODY")
expect_status 201 "$(tail -n1 <<<"$RESPONSE")" "register"
TOKEN=$(head -n -1 <<<"$RESPONSE" | jq -r .accessToken)
[[ -n "$TOKEN" && "$TOKEN" != "null" ]] || fail "register: no access token"
AUTH=(-H "Authorization: Bearer $TOKEN")

expect_status 401 "$(curl -s -o /dev/null -w '%{http_code}' "$BASE_URL/api/tasks")" "tasks without token"

DUE=$(date -u -d '+7 days' +%Y-%m-%dT%H:%M:%SZ)
# Title "Relatório de integração", written with JSON escapes so the script works in any terminal encoding.
TASK_BODY=$(jq -n --arg due "$DUE" '{title:"Relatório de integração", priority:"High", dueDate:$due}')
RESPONSE=$(curl -s -w '\n%{http_code}' -X POST "$BASE_URL/api/tasks" "${AUTH[@]}" -H 'Content-Type: application/json' \
  -d "$TASK_BODY")
expect_status 201 "$(tail -n1 <<<"$RESPONSE")" "create task"
TASK_ID=$(head -n -1 <<<"$RESPONSE" | jq -r .id)

RESPONSE=$(curl -s -w '\n%{http_code}' -X PATCH "$BASE_URL/api/tasks/$TASK_ID/status" "${AUTH[@]}" \
  -H 'Content-Type: application/json' -d '{"status":"Done"}')
expect_status 200 "$(tail -n1 <<<"$RESPONSE")" "complete task"

# Case-insensitive search with accents: "RELATÓRIO" (URL-encoded) finds "Relatório".
LIST=$(curl -s "$BASE_URL/api/tasks?search=RELAT%C3%93RIO" "${AUTH[@]}")
[[ "$(jq .totalCount <<<"$LIST")" == "1" ]] || fail "search: expected 1 result, got $(jq -c . <<<"$LIST")"
echo "ok  search with accents"

SUMMARY=$(curl -s "$BASE_URL/api/tasks/summary" "${AUTH[@]}")
[[ "$(jq .done <<<"$SUMMARY")" == "1" && "$(jq .completionRate <<<"$SUMMARY")" == "100" ]] \
  || fail "summary: unexpected $(jq -c . <<<"$SUMMARY")"
echo "ok  summary"

expect_status 204 "$(curl -s -o /dev/null -w '%{http_code}' -X DELETE "$BASE_URL/api/tasks/$TASK_ID" "${AUTH[@]}")" "delete task"

echo "Smoke test passed."
