#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

if [[ "${RL_BLOCK7_ORACLE_BROWSER_CERTIFICATION_REQUIRED:-}" != "true" ]]; then
  echo "Block 7 Oracle certification requires the explicit CI certification opt-in." >&2
  exit 1
fi
if [[ -z "${BLOCK7_ORACLE_CONNECTION_STRING:-}" \
  || -z "${ConnectionStrings__OracleDB_LocalXE:-}" ]]; then
  echo "Block 7 Oracle certification requires the disposable Oracle XE connection." >&2
  exit 1
fi

cd "$repo_root"
export RL_BLOCK7_ORACLE_INTEGRATION_REQUIRED=true
oracle_output="$(dotnet test backend/RL.API.Tests/RL.API.Tests.csproj \
  --configuration Release --no-build --no-restore \
  --filter 'FullyQualifiedName~DraftEndpoints_CreateReadUpdatePreviewAndRejectStaleVersionAgainstOracle' \
  --logger 'console;verbosity=normal' 2>&1 | tee /dev/stderr)"
if ! grep -Eq 'Passed: *1' <<<"$oracle_output"; then
  echo "Block 7 Oracle write suite did not execute exactly one passing integration test." >&2
  exit 1
fi
echo 'BLOCK7_ORACLE_TESTS_EXECUTED=1'
echo 'BLOCK7_ORACLE_WRITE_INTEGRATION=PASS'

api_log="${RUNNER_TEMP:-/tmp}/block7-e2e-api.log"
api_dll="$repo_root/tools/Block7OracleE2EHost/bin/Release/net10.0/Block7OracleE2EHost.dll"
if [[ ! -f "$api_dll" ]]; then
  echo "Block 7 API host was not built: $api_dll" >&2
  exit 1
fi

export ASPNETCORE_ENVIRONMENT=E2E
export RL_BLOCK7_REAL_E2E_REQUIRED=true
export DATABASE_PROFILE=LOCAL_XE
dotnet "$api_dll" >"$api_log" 2>&1 &
api_pid=$!
cleanup() {
  kill "$api_pid" 2>/dev/null || true
  wait "$api_pid" 2>/dev/null || true
}
trap cleanup EXIT

ready=false
for attempt in $(seq 1 60); do
  if curl --silent --show-error --fail http://127.0.0.1:5043/__e2e/readyz >/dev/null; then
    ready=true
    break
  fi
  if ! kill -0 "$api_pid" 2>/dev/null; then
    cat "$api_log"
    exit 1
  fi
  sleep 2
done
if [[ "$ready" != "true" ]]; then
  sed -E 's/(Password|User Id)=([^; ]+)/\1=[REDACTED]/Ig' "$api_log" | tail -n 160
  exit 1
fi
echo 'BLOCK7_REAL_API_ORACLE_READY=PASS'

pushd "$repo_root/frontend/rl-app" >/dev/null
export CI=true
if ! browser_output="$(npm run e2e -- e2e/block7-oracle-real.spec.ts 2>&1 | tee /dev/stderr)"; then
  popd >/dev/null
  echo 'Block 7 Oracle API host diagnostics (secrets omitted):' >&2
  sed -E 's/(Password|User Id)=([^; ]+)/\1=[REDACTED]/Ig' "$api_log" | tail -n 160
  exit 1
fi
popd >/dev/null

if ! grep -Eq '1 passed' <<<"$browser_output"; then
  echo "Block 7 real browser suite did not execute exactly one passing test." >&2
  exit 1
fi
echo 'BLOCK7_REAL_BROWSER_TESTS_EXECUTED=1'
echo 'BLOCK7_CRITICAL_MATRIX_API_ROUTES_MOCKED=0'
echo 'BLOCK7_BROWSER_API_ORACLE_E2E=PASS'
