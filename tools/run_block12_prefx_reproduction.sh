#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
prefx_sha="3325778c81dee92853044036945ae16ba0942311"
temp_root="${RUNNER_TEMP:-/tmp}"
prefx_root="$temp_root/block12-prefx-$GITHUB_RUN_ID"
api_log="$temp_root/block12-prefx-api-$GITHUB_RUN_ID.log"
prefx_user_id=$((9900000000 + GITHUB_RUN_ID % 99999998))

if [[ -z "${BLOCK12_PREFX_ORACLE_CONNECTION_STRING:-}" ]]; then
  echo 'BLOCK12_PREFX_ORACLE_CONNECTION_STRING is required.' >&2
  exit 1
fi

api_pid=''
cleanup() {
  cd "$repo_root"
  if [[ -n "$api_pid" ]]; then
    kill "$api_pid" 2>/dev/null || true
    wait "$api_pid" 2>/dev/null || true
  fi
  if git -C "$repo_root" worktree list --porcelain | grep -Fq "worktree $prefx_root"; then
    git -C "$repo_root" worktree remove --force "$prefx_root"
  fi
}
trap cleanup EXIT

git -C "$repo_root" fetch --no-tags --depth=1 origin "$prefx_sha"
git -C "$repo_root" worktree add --detach "$prefx_root" FETCH_HEAD

# Run the current diagnostic E2E against the historical defective app source.
cp "$repo_root/frontend/rl-app/e2e/block7-oracle-real.spec.ts" \
  "$prefx_root/frontend/rl-app/e2e/block7-oracle-real.spec.ts"
# The normal E2E host uses a fixed user id and leaves that row in the shared XE schema.
# Give this isolated historical run a run-scoped user id in both its host and browser token.
sed -i "s/987654/$prefx_user_id/g" \
  "$prefx_root/tools/Block7OracleE2EHost/Program.cs" \
  "$prefx_root/frontend/rl-app/e2e/block7-oracle-real.spec.ts"
ln -s "$repo_root/frontend/rl-app/node_modules" "$prefx_root/frontend/rl-app/node_modules"

export Database__Profile=LOCAL_XE
export ConnectionStrings__OracleDB_LocalXE="$BLOCK12_PREFX_ORACLE_CONNECTION_STRING"
export BLOCK7_ORACLE_CONNECTION_STRING="$BLOCK12_PREFX_ORACLE_CONNECTION_STRING"
export RL_BLOCK7_ORACLE_INTEGRATION_REQUIRED=true
export RL_BLOCK7_REAL_E2E_REQUIRED=true
export RL_BLOCK9_REAL_E2E_REQUIRED=true
export RL_BLOCK12_RISKS_UI_ORACLE_REQUIRED=true
export RL_BLOCK12_FULL_CHAIN_UI_REQUIRED=true
export ASPNETCORE_ENVIRONMENT=E2E
export BLOCK12_EXPECT_PREFX_REPRODUCTION=true

host_project="$prefx_root/tools/Block7OracleE2EHost/Block7OracleE2EHost.csproj"
dotnet build "$host_project" --configuration Release
api_dll="$prefx_root/tools/Block7OracleE2EHost/bin/Release/net10.0/Block7OracleE2EHost.dll"
dotnet "$api_dll" >"$api_log" 2>&1 &
api_pid=$!

ready=false
for attempt in $(seq 1 60); do
  if curl --silent --show-error --fail http://127.0.0.1:5043/__e2e/readyz >/dev/null; then
    ready=true
    break
  fi
  if ! kill -0 "$api_pid" 2>/dev/null; then
    sed -E 's/(Password|User Id)=([^; ]+)/\1=[REDACTED]/Ig' "$api_log" | tail -n 120 >&2
    exit 1
  fi
  sleep 2
done
if [[ "$ready" != true ]]; then
  sed -E 's/(Password|User Id)=([^; ]+)/\1=[REDACTED]/Ig' "$api_log" | tail -n 120 >&2
  exit 1
fi
echo 'BLOCK12_PREFX_ORACLE_API_READY=PASS'

pushd "$prefx_root/frontend/rl-app" >/dev/null
export CI=true
browser_output="$(npm run e2e -- e2e/block7-oracle-real.spec.ts --grep 'Block 7/9 real browser certifies draft' 2>&1 | tee /dev/stderr)"
popd >/dev/null

if grep -Fq 'Datos incompatibles para proyección institucional' "$api_log"; then
  echo 'BLOCK12_PREFX_API_PROJECTION_EXCEPTION_BEGIN'
  grep -F 'Datos incompatibles para proyección institucional' "$api_log" \
    | sed -E 's/EvaluationId=[0-9]+/EvaluationId=[fixture]/g' \
    | tail -n 2
  echo 'BLOCK12_PREFX_API_PROJECTION_EXCEPTION_END'
fi

grep -Fq '1 passed' <<<"$browser_output"
grep -Fq 'BLOCK12_PRE_FIX_FULL_MATRIX_DEFECT_REPRODUCED=YES' <<<"$browser_output"
echo 'BLOCK12_PREFX_DISPOSABLE_XE_BROWSER_API_REPRODUCTION=PASS'
