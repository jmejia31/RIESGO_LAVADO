import type { FullConfig, Reporter, Suite, TestCase, TestResult } from '@playwright/test/reporter';

type ScenarioId = 'AUTH_01' | 'AUTH_02' | 'AUTH_03' | 'AUTH_04' | 'AUTH_05' | 'AUTH_06A';
type ScenarioStatus = 'PASS' | 'FAIL' | 'SKIPPED' | 'NOT_RUN';
type FailureCategory =
  | 'ASSERTION_FAILURE'
  | 'HTTP_STATUS_MISMATCH'
  | 'TIMEOUT'
  | 'NAVIGATION_FAILURE'
  | 'BROWSER_FAILURE'
  | 'ENVIRONMENT_FAILURE'
  | 'SETUP_FAILURE'
  | 'UNKNOWN_FAILURE';

const scenarios: ScenarioId[] = ['AUTH_01', 'AUTH_02', 'AUTH_03', 'AUTH_04', 'AUTH_05', 'AUTH_06A'];

const stages: Record<ScenarioId, string> = {
  AUTH_01: 'login_profile_permissions',
  AUTH_02: 'session_storage',
  AUTH_03: 'csrf',
  AUTH_04: 'refresh_rotation',
  AUTH_05: 'logout_revocation',
  AUTH_06A: 'refresh_concurrency',
};

function scenarioId(test: TestCase): ScenarioId | undefined {
  const title = test.titlePath().find(value => /^(AUTH-0[1-5]|AUTH-06A)\b/.test(value));
  const match = title?.match(/^(AUTH-0[1-5]|AUTH-06A)\b/);
  return match?.[1].replace('-', '_') as ScenarioId | undefined;
}

function classifyFailure(result: TestResult): FailureCategory {
  if (result.status === 'timedOut') return 'TIMEOUT';

  const error = result.errors[0];
  const name = (error?.name ?? '').toLowerCase();
  const message = (error?.message ?? '').toLowerCase();

  if (name.includes('timeout') || message.includes('timeout') || message.includes('timed out')) return 'TIMEOUT';
  if (/status.*expected|expected.*status|tohave.*status|tobe\(\s*\d+\s*\)/.test(message)) return 'HTTP_STATUS_MISMATCH';
  if (/navigation|page\.goto|net::err/.test(message)) return 'NAVIGATION_FAILURE';
  if (/chromium|browser|browsercontext|target page|target closed/.test(message)) return 'BROWSER_FAILURE';
  if (/econn|socket|connection refused|fetch failed|network|environment/.test(message)) return 'ENVIRONMENT_FAILURE';
  if (/blocked_precondition|permission-contract|setup|credential/.test(message)) return 'SETUP_FAILURE';
  if (/expect|assert|received|expected|to be|to equal/.test(message)) return 'ASSERTION_FAILURE';
  return 'UNKNOWN_FAILURE';
}

export default class P06SafeReporter implements Reporter {
  private readonly planned = new Set<ScenarioId>();
  private readonly results = new Map<ScenarioId, { status: ScenarioStatus; category?: FailureCategory }>();

  onBegin(_config: FullConfig, suite: Suite): void {
    for (const test of suite.allTests()) {
      const id = scenarioId(test);
      if (id) this.planned.add(id);
    }
  }

  onTestEnd(test: TestCase, result: TestResult): void {
    const id = scenarioId(test);
    if (!id) return;

    if (result.status === 'skipped') {
      this.results.set(id, { status: 'SKIPPED' });
      return;
    }

    if (result.status === 'passed') {
      this.results.set(id, { status: 'PASS' });
      return;
    }

    this.results.set(id, { status: 'FAIL', category: classifyFailure(result) });
  }

  onEnd(): void {
    const outcomes = scenarios.map(id => ({
      id,
      stage: stages[id],
      ...(this.results.get(id) ?? { status: this.planned.has(id) ? 'NOT_RUN' : 'NOT_RUN' }),
    }));
    const failed = outcomes.filter(value => value.status === 'FAIL');
    const passed = outcomes.filter(value => value.status === 'PASS').length;
    const skipped = outcomes.filter(value => value.status === 'SKIPPED').length;
    const notRun = outcomes.filter(value => value.status === 'NOT_RUN').length;
    const categories = [...new Set(failed.map(value => value.category).filter(Boolean))] as FailureCategory[];
    const aggregateCategory = categories.length === 1 ? categories[0] : categories.length > 1 ? 'UNKNOWN_FAILURE' : 'NONE';

    console.log('P06_DIAGNOSTIC_REPORTER=PASS');
    console.log('P06_DIAGNOSTIC_STATUS=AVAILABLE');
    for (const outcome of outcomes) {
      console.log(`P06_${outcome.id}=${outcome.status}`);
      console.log(`P06_${outcome.id}_STAGE=${outcome.stage}`);
      if (outcome.category) console.log(`P06_${outcome.id}_FAILURE_CATEGORY=${outcome.category}`);
    }
    console.log('P06_TESTS_TOTAL=6');
    console.log(`P06_TESTS_PASSED=${passed}`);
    console.log(`P06_TESTS_FAILED=${failed.length}`);
    console.log(`P06_TESTS_SKIPPED=${skipped}`);
    console.log(`P06_TESTS_NOT_RUN=${notRun}`);
    console.log(`P06_FAILED_SCENARIOS=${failed.length > 0 ? failed.map(value => value.id).join(',') : 'NONE'}`);
    console.log(`P06_FAILURE_CATEGORY=${aggregateCategory}`);
    console.log(`P06_REAL_CERTIFICATION=${passed === 6 ? 'PASS' : 'FAIL'}`);
  }
}
