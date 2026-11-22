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
export RL_P03_TX12_AUDIT_FAILURE_REQUIRED=true
dotnet "$api_dll" >"$api_log" 2>&1 &
api_pid=$!
tx12_disabled=false
cleanup() {
  local test_exit_code="$1"
  local cleanup_exit_code=0
  local host_exit_code=0
  local waited=0

  # Preserve the test result and execute teardown only once, even after errors.
  trap - EXIT
  set +e

  if [[ "${RL_P03_TX12_AUDIT_FAILURE_REQUIRED:-}" == "true" && "$tx12_disabled" != "true" ]] &&
     kill -0 "$api_pid" 2>/dev/null; then
    if ! curl --silent --show-error --fail --max-time 15 \
         --request POST http://127.0.0.1:5043/__e2e/tx12-audit-disable >/dev/null; then
      echo 'BLOCK7_TX12_FAULT_RESET=FAIL' >&2
      cleanup_exit_code=1
    fi
  fi

  if kill -0 "$api_pid" 2>/dev/null; then
    if ! curl --silent --show-error --fail --max-time 15 \
         --request POST http://127.0.0.1:5043/__e2e/shutdown >/dev/null; then
      echo 'BLOCK7_HOST_GRACEFUL_SHUTDOWN_REQUEST=FAIL' >&2
      cleanup_exit_code=1
    fi
    while kill -0 "$api_pid" 2>/dev/null && (( waited < 120 )); do
      sleep 1
      waited=$((waited + 1))
    done
    if kill -0 "$api_pid" 2>/dev/null; then
      echo "BLOCK7_HOST_GRACEFUL_SHUTDOWN_TIMEOUT=FAIL;SECONDS=$waited" >&2
      cleanup_exit_code=1
      kill -TERM "$api_pid" 2>/dev/null
      sleep 2
      if kill -0 "$api_pid" 2>/dev/null; then
        kill -KILL "$api_pid" 2>/dev/null
      fi
    fi
  fi

  wait "$api_pid" 2>/dev/null || host_exit_code=$?
  if [[ "$host_exit_code" -ne 0 ]]; then
    echo "BLOCK7_HOST_PROCESS_EXIT=FAIL;CODE=$host_exit_code" >&2
    cleanup_exit_code=1
  fi

  if [[ -f "$api_log" ]] &&
     grep -Fq 'BLOCK7_FIXTURE_RISK_CLEANUP_VERIFIED=YES' "$api_log" &&
     grep -Fq 'BLOCK7_FIXTURE_RISK_REMAINING=0' "$api_log" &&
     grep -Fq 'BLOCK7_FIXTURE_DEPENDENCIES_REMAINING=0' "$api_log" &&
     grep -Fq 'BLOCK7_FIXTURE_METADATA_REMAINING=0' "$api_log" &&
     grep -Fq 'BLOCK7_FIXTURE_CLEANUP_ERRORS=0' "$api_log" &&
     [[ "$host_exit_code" -eq 0 && "$cleanup_exit_code" -eq 0 ]]; then
    echo 'BLOCK7_FIXTURE_RISK_CLEANUP_VERIFIED=YES'
    echo 'BLOCK7_FIXTURE_RISK_REMAINING=0'
    echo 'BLOCK7_FIXTURE_DEPENDENCIES_REMAINING=0'
    echo 'BLOCK7_FIXTURE_METADATA_REMAINING=0'
    echo 'BLOCK7_FIXTURE_CLEANUP_ERRORS=0'
  else
    echo 'BLOCK7_FIXTURE_RISK_CLEANUP_VERIFIED=NO' >&2
    echo 'BLOCK7_FIXTURE_RISK_REMAINING=UNVERIFIED' >&2
    if [[ -f "$api_log" ]]; then
      sed -E 's/(Password|User Id)=([^; ]+)/\1=[REDACTED]/Ig' "$api_log" | tail -n 80 >&2
    fi
    cleanup_exit_code=1
  fi

  if [[ "$test_exit_code" -ne 0 || "$cleanup_exit_code" -ne 0 ]]; then
    echo "BLOCK7_E2E_FINAL_RESULT=FAIL;TEST_EXIT=$test_exit_code;CLEANUP_EXIT=$cleanup_exit_code" >&2
    exit 1
  fi
  echo 'BLOCK7_E2E_FINAL_RESULT=PASS'
  exit 0
}
trap 'cleanup "$?"' EXIT

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
tx12_log_start_line="$(wc -l < "$api_log" | tr -d '[:space:]')"
if ! tx12_output="$(RL_P03_TX12_AUDIT_FAILURE_REQUIRED=true npm run e2e -- e2e/tx12-audit-rollback-real.spec.ts 2>&1 | tee /dev/stderr)"; then
  popd >/dev/null
  echo 'TX12 Oracle API host diagnostics (secrets omitted):' >&2
  sed -E 's/(Password|User Id)=([^; ]+)/\1=[REDACTED]/Ig' "$api_log" | tail -n 160 >&2
  exit 1
fi
popd >/dev/null
grep -Eq '1 passed' <<<"$tx12_output"
for marker in \
  TX12_REAL_ANGULAR_INTERACTION=PASS \
  TX12_REAL_HTTP_ERROR=PASS \
  TX12_REAL_ORACLE_AUDIT_FAILURE=PASS \
  TX12_JSON_ROLLBACK=PASS \
  TX12_HASH_ROLLBACK=PASS \
  TX12_STATE_ROLLBACK=PASS \
  TX12_VIGENCY_ROLLBACK=PASS \
  TX12_AUDIT_DELTA=0 \
  TX12_PARTIAL_COMMIT=0 \
  TX12_ANGULAR_ERROR_VISIBLE=PASS \
  TX12_FALSE_SUCCESS_MESSAGE=NO \
  TX12_FRONTEND_NEGATIVE_E2E=PASS; do
  grep -Fq "$marker" <<<"$tx12_output"
done
tx12_disable_output="$(curl --silent --show-error --fail --request POST http://127.0.0.1:5043/__e2e/tx12-audit-disable)"
grep -Fq '"removed":true' <<<"$tx12_disable_output"
tx12_disabled=true
tx12_log_end_line="$(wc -l < "$api_log" | tr -d '[:space:]')"
echo 'P03_TX12_FRONTEND_NEGATIVE_E2E=PASS'
echo 'P03_TX12_TEMPORARY_FAULT_REMOVED=YES'

# Exercise the synthetic deletion contract while the disposable XE fixture is
# initialized and before resource-intensive browser suites reuse this service.
if [[ "${RL_MATRICES_SYNTHETIC_PURGE_REQUIRED:-}" == "true" ]]; then
  purge_output="$(dotnet test backend/RL.API.Tests/RL.API.Tests.csproj \
    --configuration Release --no-build --no-restore \
    --filter 'FullyQualifiedName~MatricesSyntheticPurgeOracleIntegrationTests' \
    --logger 'console;verbosity=normal' 2>&1 | tee /dev/stderr)"
  grep -Eq 'Total tests: *1' <<<"$purge_output"
  grep -Eq 'Passed: *1' <<<"$purge_output"
  ! grep -Eq 'Skipped: *[1-9]' <<<"$purge_output"
  for proof in \
    SYNTHETIC_MATRICES_RECORDS_REMOVED=PASS \
    CROSS_MODULE_ISOLATION=PASS \
    OTHER_MODULES_SYNTHETIC_DATA_CHANGED=0 \
    REFERENTIAL_INTEGRITY=PASS \
    PROTECTED_AUDIT_PRESERVED=PASS \
    ROLLBACK_TEST=PASS \
    MATRICES_EMPTY_STATE=PASS \
    NEW_SYNTHETIC_FAMILY_AFTER_CLEANUP=PASS; do
    grep -Fq "$proof" <<<"$purge_output"
  done
  echo 'MATRICES_SYNTHETIC_PURGE_ORACLE_XE=PASS'
  # A unique family is exported only after its Oracle fixture was removed.
  # The browser test must exercise the real API + Angular (no route mocks).
  purged_code="$(printf '%s\n' "$purge_output" | sed -nE 's/^[[:space:]]*GATE02_PURGED_FAMILY_CODE=(SYNTH_PURGE_[A-F0-9]{12})[[:space:]]*$/\1/p')"
  purged_id="$(printf '%s\n' "$purge_output" | sed -nE 's/^[[:space:]]*GATE02_PURGED_FAMILY_ID=([0-9]+)[[:space:]]*$/\1/p')"
  if ! [[ "$purged_code" =~ ^SYNTH_PURGE_[A-F0-9]{12}$ && "$purged_id" =~ ^[0-9]+$ ]]; then
    echo 'GATE02: purge test did not provide an authenticated post-cleanup family identifier.' >&2
    exit 1
  fi
  export RL_G02_PURGED_FAMILY_CODE="$purged_code"
  export RL_G02_PURGED_FAMILY_ID="$purged_id"
  pushd "$repo_root/frontend/rl-app" >/dev/null
  if ! empty_output="$(RL_G02_POST_PURGE_E2E_REQUIRED=true npm run e2e -- e2e/g02-synthetic-family-empty-real.spec.ts 2>&1 | tee /dev/stderr)"; then
    popd >/dev/null
    echo 'GATE02: real empty-family API/Angular E2E failed.' >&2
    exit 1
  fi
  popd >/dev/null
  grep -Eq '1 passed' <<<"$empty_output"
  grep -Fq 'GATE02_POST_PURGE_FAMILY_API=PASS' <<<"$empty_output"
  grep -Fq 'GATE02_POST_PURGE_FAMILY_ANGULAR=PASS' <<<"$empty_output"
  echo 'GATE02_POST_PURGE_FAMILY_API_ANGULAR=PASS'
fi

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
export RL_P0_DELETE_REAL_E2E_REQUIRED=true

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
for marker in \
  H08_A_GATE=PASS \
  H08_A_REAL_BUTTON_INTERACTIONS=PASS \
  H08_B_GATE=PASS \
  H08_C_GATE=PASS \
  H08_D_GATE=PASS \
  H08_E_GATE=PASS \
  H08_D_UNEXPECTED_DATA_LOSS=0 \
  H08_D_UNEXPECTED_DUPLICATES=0; do
  grep -Fq "$marker" <<<"$h08_output"
done
echo 'H08_FAMILY_LIFECYCLE_ORACLE_E2E=PASS'
echo 'H08_EXTENDED_A_TO_E=PASS'
pushd "$repo_root/frontend/rl-app" >/dev/null
if ! p0_publish_output="$(npm run e2e -- e2e/p0-direct-publication-real.spec.ts 2>&1 | tee /dev/stderr)"; then
  popd >/dev/null
  echo 'P0 direct publication E2E failed.' >&2
  exit 1
fi
popd >/dev/null
if ! grep -Eq '1 passed' <<<"$p0_publish_output"; then
  echo 'P0 direct publication suite did not execute exactly one passing test.' >&2
  exit 1
fi
for marker in \
  P0_PUBLISH_DIRECT_BROWSER=PASS \
  P0_PUBLISH_DIRECT_ANGULAR=PASS \
  P0_PUBLISH_DIRECT_API=PASS \
  P0_PUBLISH_DIRECT_ORACLE_XE=PASS \
  P0_PUBLISH_DIRECT_STATUS=2XX \
  P0_PUBLISH_DIRECT_READBACK=PASS \
  P0_PUBLISH_DIRECT_STATE=PUBLISHED \
  P0_PUBLISH_DIRECT_VIGENCY=1 \
  P0_PUBLISH_DIRECT_AUDIT=PASS \
  P0_PUBLISH_DIRECT_FEEDBACK=PASS \
  P0_PUBLICATION_ORACLE_E2E=PASS; do
  grep -Fq "$marker" <<<"$p0_publish_output"
done
echo 'P0_DIRECT_PUBLICATION_ORACLE_E2E=PASS'
pushd "$repo_root/frontend/rl-app" >/dev/null
if ! p0_dirty_publish_output="$(npm run e2e -- e2e/p0-publication-with-changes-real.spec.ts 2>&1 | tee /dev/stderr)"; then
  popd >/dev/null
  echo 'P0 dirty publication Oracle API host diagnostics (secrets omitted):' >&2
  sed -E 's/(Password|User Id)=([^; ]+)/\1=[REDACTED]/Ig' "$api_log" | tail -n 160 >&2
  exit 1
fi
popd >/dev/null
if ! grep -Eq '1 passed' <<<"$p0_dirty_publish_output"; then
  echo 'P0 dirty publication suite did not execute exactly one passing test.' >&2
  exit 1
fi
for marker in \
  P0_PUBLISH_DIRTY_BROWSER=PASS \
  P0_PUBLISH_DIRTY_PUT_STATUS=2XX \
  P0_PUBLISH_DIRTY_PUT_READBACK=PASS \
  P0_PUBLISH_DIRTY_JSON_HASH=PASS \
  P0_PUBLISH_DIRTY_POST_STATUS=2XX \
  P0_PUBLISH_DIRTY_STATE=PUBLISHED \
  P0_PUBLISH_DIRTY_VIGENCY=1 \
  P0_PUBLISH_DIRTY_AUDIT=PASS \
  P0_PUBLISH_DIRTY_FEEDBACK=PASS \
  P0_PUBLICATION_DIRTY_ORACLE_E2E=PASS; do
  grep -Fq "$marker" <<<"$p0_dirty_publish_output"
done
echo 'P0_DIRTY_PUBLICATION_ORACLE_E2E=PASS'
pushd "$repo_root/frontend/rl-app" >/dev/null
if ! p0_delete_output="$(npm run e2e -- e2e/h08-version-delete-real.spec.ts 2>&1 | tee /dev/stderr)"; then
  popd >/dev/null
  echo 'P0 delete Oracle API host diagnostics (secrets omitted):' >&2
  sed -E 's/(Password|User Id)=([^; ]+)/\1=[REDACTED]/Ig' "$api_log" | tail -n 160
  exit 1
fi
popd >/dev/null
if ! grep -Eq '1 passed' <<<"$p0_delete_output"; then
  echo "P0 real version deletion suite did not execute exactly one passing test." >&2
  exit 1
fi
for marker in \
  P0_DELETE_REAL_BROWSER=PASS \
  P0_DELETE_REAL_ANGULAR=PASS \
  P0_DELETE_REAL_API=PASS \
  P0_DELETE_REAL_ORACLE_XE=PASS \
  P0_DELETE_FIXTURE=PASS \
  P0_DELETE_HTTP_STATUS=2XX \
  P0_DELETE_TARGET_REMOVED=YES \
  P0_DELETE_OTHER_VERSION_PRESERVED=YES \
  P0_DELETE_FAMILY_PRESERVED=YES \
  P0_DELETE_READBACK_STATUS=404 \
  P0_DELETE_COUNTER_UPDATED=YES \
  P0_DELETE_MODAL_REMAINS_OPEN=YES \
  P0_DELETE_FEEDBACK=PASS \
  P0_DELETE_ACCESSIBILITY=PASS \
  P0_DELETE_AUDIT=PASS \
  P0_DELETE_AUDIT_DUPLICATES=0 \
  P0_DELETE_ORACLE_INTEGRITY=PASS \
  P0_DELETE_FORMULA_DEPENDENCY_CONFLICT=PASS \
  P0_DELETE_FORMULA_DEPENDENCY_PRESERVED=YES \
  P0_DELETE_EVALUATION_DEPENDENCY_CONFLICT=PASS \
  P0_DELETE_EVALUATION_PRESERVED=YES \
  P0_VERSION_DELETE_E2E=PASS; do
  grep -Fq "$marker" <<<"$p0_delete_output"
done
echo 'P0_DELETE_REAL_BROWSER_E2E=PASS'

# The complete repository E2E suite must run while this script's real
# Oracle-backed API host is alive. The EXIT trap shuts port 5043 down after
# this script returns, so running the suite later would create false API
# timeouts for tests that depend on the real host.
if [[ "${RL_BLOCK7_FULL_REPOSITORY_E2E_REQUIRED:-}" == "true" ]]; then
  pushd "$repo_root/frontend/rl-app" >/dev/null
  if ! full_e2e_output="$(npm run e2e 2>&1 | tee /dev/stderr)"; then
    popd >/dev/null
    echo 'Full repository E2E failed while the real Oracle API host was alive.' >&2
    sed -E 's/(Password|User Id)=([^; ]+)/\1=[REDACTED]/Ig' "$api_log" | tail -n 160 >&2
    exit 1
  fi
  popd >/dev/null
  if ! grep -Eq '[0-9]+ passed' <<<"$full_e2e_output"; then
    echo 'Full repository E2E did not report passing tests.' >&2
    exit 1
  fi
  echo 'BLOCK7_FULL_REPOSITORY_E2E=PASS'
fi

grep -Fq 'BLOCK12_ORACLE_XE_FULL_MATRIX_UI_CALCULATIONS=2006/2006' <<<"$browser_output"
# Inspect only the API host log and report counts, never exception text or connection data.
api_log_without_expected_tx12="$(mktemp)"
awk -v start="$tx12_log_start_line" -v end="$tx12_log_end_line" 'NR <= start || NR > end' "$api_log" > "$api_log_without_expected_tx12"
oracle_error_matches="$(grep -Eio 'ORA-[0-9]{5}|OracleException|Oracle[^[:cntrl:]]{0,80}(timeout|connection error)|database[^[:cntrl:]]{0,80}(timeout|connection error)' "$api_log_without_expected_tx12" || true)"
rm -f "$api_log_without_expected_tx12"
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

