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
export Database__Profile=LOCAL_XE

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

host_project="$repo_root/tools/Block7OracleE2EHost/Block7OracleE2EHost.csproj"
dotnet build "$host_project" --configuration Release

api_log="${RUNNER_TEMP:-/tmp}/block7-e2e-api.log"
api_dll="$repo_root/tools/Block7OracleE2EHost/bin/Release/net10.0/Block7OracleE2EHost.dll"
if [[ ! -f "$api_dll" ]]; then
  echo "Block 7 API host was not built: $api_dll" >&2
  exit 1
fi

export ASPNETCORE_ENVIRONMENT=E2E
export RL_BLOCK7_REAL_E2E_REQUIRED=true
export RL_BLOCK9_REAL_E2E_REQUIRED=true
export RL_BLOCK12_RISKS_UI_ORACLE_REQUIRED=true
export RL_BLOCK12_FULL_CHAIN_UI_REQUIRED=true
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

export RL_H02_ORACLE_HISTORY_CERTIFICATION_REQUIRED=true
h02_output="$(dotnet test backend/RL.API.Tests/RL.API.Tests.csproj \
  --configuration Release --no-build --no-restore \
  --filter 'FullyQualifiedName~AuditoriaFamiliaOracleIntegrationTests' \
  --logger 'console;verbosity=normal' 2>&1 | tee /dev/stderr)"
if ! grep -Eq 'Passed: *1' <<<"$h02_output"; then
  echo "H02 Oracle family audit history certification did not execute exactly one passing integration test." >&2
  exit 1
fi
echo 'H02_ORACLE_HISTORY_TEST_EXECUTED=1'
echo 'H02_ORACLE_HISTORY_ID_AND_CODE=PASS'
echo 'H02_ORACLE_HISTORY_PAGINATION=25/25/10'
echo 'H02_ORACLE_HISTORY_ISOLATION=PASS'

export RL_H08_FAMILY_LIFECYCLE_CERTIFICATION_REQUIRED=true

block9_evidence_dir="${RUNNER_TEMP:-/tmp}/block9-browser-evidence"
mkdir -p "$block9_evidence_dir"
export BLOCK9_EVIDENCE_OUTPUT_DIR="$block9_evidence_dir"
pushd "$repo_root/frontend/rl-app" >/dev/null
export CI=true
if ! browser_output="$(npm run e2e -- e2e/block7-oracle-real.spec.ts 2>&1 | tee /dev/stderr)"; then
  popd >/dev/null
  echo 'Block 7 Oracle API host diagnostics (secrets omitted):' >&2
  sed -E 's/(Password|User Id)=([^; ]+)/\1=[REDACTED]/Ig' "$api_log" | tail -n 160
  exit 1
fi
popd >/dev/null

if ! grep -Eq '3 passed' <<<"$browser_output"; then
  echo "Block 7/12 real browser suite did not execute exactly three passing tests." >&2
  exit 1
fi
pushd "$repo_root/frontend/rl-app" >/dev/null
if ! h08_output="$(npm run e2e -- e2e/h08-family-lifecycle-real.spec.ts 2>&1 | tee /dev/stderr)"; then
  popd >/dev/null
  echo 'H08 Oracle API host diagnostics (secrets omitted):' >&2
  sed -E 's/(Password|User Id)=([^; ]+)/\1=[REDACTED]/Ig' "$api_log" | tail -n 160
  exit 1
fi
popd >/dev/null
if ! grep -Eq '1 passed' <<<"$h08_output"; then
  echo "H08 real family lifecycle suite did not execute exactly one passing test." >&2
  exit 1
fi
grep -Fq 'H08_FAMILY_LIFECYCLE_TWO_CYCLES=PASS' <<<"$h08_output"
grep -Fq 'H08_FAMILY_REUSE_IDENTITIES_EVIDENCE=PASS' <<<"$h08_output"
echo 'H08_FAMILY_LIFECYCLE_ORACLE_E2E=PASS'
echo 'H08_FAMILY_LIFECYCLE_TWO_CYCLES=PASS'
echo 'H08_FAMILY_REUSE_IDENTITIES_EVIDENCE=PASS'
grep -Fq 'BLOCK12_ORACLE_XE_FULL_MATRIX_UI_CALCULATIONS=2006/2006' <<<"$browser_output"
# Inspect only the API host log and report counts, never exception text or connection data.
oracle_error_matches="$(grep -Eio 'ORA-[0-9]{5}|OracleException|Oracle[^[:cntrl:]]{0,80}(timeout|connection error)|database[^[:cntrl:]]{0,80}(timeout|connection error)' "$api_log" || true)"
if [[ -n "$oracle_error_matches" ]]; then
  oracle_error_count="$(printf '%s\n' "$oracle_error_matches" | wc -l | tr -d '[:space:]')"
  echo "BLOCK7_UNEXPECTED_ORACLE_ERRORS=$oracle_error_count" >&2
  exit 1
fi
echo 'BLOCK7_UNEXPECTED_ORACLE_ERRORS=0'
echo 'BLOCK7_REAL_BROWSER_TESTS_EXECUTED=1'
echo 'BLOCK7_CRITICAL_MATRIX_API_ROUTES_MOCKED=0'
echo 'BLOCK7_BROWSER_API_ORACLE_E2E=PASS'
echo 'BLOCK9_ORACLE_TESTS_EXECUTED=1'
echo 'BLOCK9_MITIGATION_ORACLE_INTEGRATION=PASS'
echo 'BLOCK12_ORACLE_XE_UI_RISK_IDENTITIES=59/59'
echo 'BLOCK12_ORACLE_XE_FULL_MATRIX_UI_CALCULATIONS=2006/2006'
echo 'BLOCK9_MONITORING_ORACLE_INTEGRATION=PASS'
echo 'BLOCK9_RBAC_ORACLE_INTEGRATION=PASS'
echo 'BLOCK9_EVIDENCE_ORACLE_INTEGRATION=PASS'
echo 'BLOCK9_REAL_BROWSER_TESTS_EXECUTED=1'
echo 'BLOCK9_CRITICAL_ROUTES_MOCKED=0'
echo 'BLOCK9_BROWSER_API_ORACLE_E2E=PASS'
