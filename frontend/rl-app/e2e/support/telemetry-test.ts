import { expect, test as base, type Page, type Request, type Response } from '@playwright/test';

type BrowserTelemetry = {
  consoleErrors: string[];
  browserResourceErrors: number[];
  pageErrors: string[];
  requestFailures: string[];
  httpErrors: Array<{ status: number; method: string; url: string }>;
};

type ExpectedHttpResponse = {
  status: number;
  method?: string;
  path?: string;
  pathIncludes?: string;
  count?: number;
  minCount?: number;
  maxCount?: number;
  reason?: string;
};

export function expectHttpStatuses(...statuses: number[]): void {
  for (const status of statuses) {
    base.info().annotations.push({ type: 'expected-http-status', description: String(status) });
  }
}

export function expectHttpResponse(response: ExpectedHttpResponse): void {
  base.info().annotations.push({ type: 'expected-http-response', description: JSON.stringify(response) });
}

function safePath(rawUrl: string): string {
  try {
    return new URL(rawUrl).pathname;
  } catch {
    return '<unparseable-url>';
  }
}

function safeConsoleMessage(value: string): string {
  return value
    .replace(/Bearer\s+\S+/gi, 'Bearer [REDACTED]')
    .replace(/\beyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\b/g, '[REDACTED_JWT]')
    .replace(/(password|token|secret|credential|connection string)(\s*[:=]\s*)[^\s,;]+/gi, '$1$2[REDACTED]')
    .replace(/https?:\/\/[^\s?]+\?[^\s]+/gi, '[URL_WITH_QUERY_REDACTED]');
}

export const test = base.extend<{ browserTelemetry: void }>({
  browserTelemetry: [async ({ context }, use, testInfo) => {
    const telemetry: BrowserTelemetry = {
      consoleErrors: [],
      browserResourceErrors: [],
      pageErrors: [],
      requestFailures: [],
      httpErrors: [],
    };
    const observedPages = new WeakSet<Page>();

    const observePage = (page: Page): void => {
      if (observedPages.has(page)) return;
      observedPages.add(page);
      page.on('console', message => {
        if (message.type() !== 'error') return;
        const resourceStatus = message.text().match(/^Failed to load resource: the server responded with a status of (\d{3})/);
        if (resourceStatus) telemetry.browserResourceErrors.push(Number(resourceStatus[1]));
        else telemetry.consoleErrors.push(safeConsoleMessage(message.text()));
      });
      page.on('pageerror', error => telemetry.pageErrors.push(safeConsoleMessage(error.stack || error.message)));
    };
    const observeRequestFailure = (request: Request): void => {
      telemetry.requestFailures.push(`${request.method()} ${safePath(request.url())} (${request.failure()?.errorText ?? 'unknown'})`);
    };
    const observeResponse = (response: Response): void => {
      if (response.status() >= 400) {
        telemetry.httpErrors.push({
          status: response.status(),
          method: response.request().method(),
          url: safePath(response.url()),
        });
      }
    };

    context.pages().forEach(observePage);
    context.on('page', observePage);
    context.on('requestfailed', observeRequestFailure);
    context.on('response', observeResponse);
    await use();
    context.off('page', observePage);
    context.off('requestfailed', observeRequestFailure);
    context.off('response', observeResponse);

    const expectedResponses: Array<ExpectedHttpResponse & { remaining: number; observed: number; maximum: number }> = testInfo.annotations
      .filter(annotation => annotation.type === 'expected-http-status' || annotation.type === 'expected-http-response')
      .map(annotation => annotation.type === 'expected-http-status'
        ? { status: Number(annotation.description), remaining: 1, observed: 0, maximum: 1 }
        : (() => {
          const expected = JSON.parse(annotation.description!) as ExpectedHttpResponse;
          return {
            ...expected,
            remaining: expected.minCount ?? expected.count ?? 1,
            observed: 0,
            maximum: expected.maxCount ?? expected.count ?? 1,
          };
        })());
    const unexpectedHttpErrors = telemetry.httpErrors.filter(error => {
      const expectedIndex = expectedResponses.findIndex(expected => expected.observed < expected.maximum
        && expected.status === error.status
        && (!expected.method || expected.method === error.method)
        && (!expected.path || expected.path === error.url)
        && (!expected.pathIncludes || error.url.includes(expected.pathIncludes)));
      if (expectedIndex < 0) return true;
      const expected = expectedResponses[expectedIndex];
      expected.observed++;
      expected.remaining = Math.max(0, expected.remaining - 1);
      return false;
    });
    const unobservedExpectedResponses = expectedResponses.filter(expected => expected.remaining > 0);
    const unexpectedBrowserResourceErrors = [...telemetry.browserResourceErrors];
    for (const expected of expectedResponses) {
      if (expected.status >= 400) {
        for (let matched = 0; matched < expected.observed; matched++) {
          const resourceIndex = unexpectedBrowserResourceErrors.indexOf(expected.status);
          if (resourceIndex >= 0) unexpectedBrowserResourceErrors.splice(resourceIndex, 1);
        }
      }
    }

    await testInfo.attach('browser-telemetry', {
      body: JSON.stringify({ ...telemetry, unexpectedHttpErrors }, null, 2),
      contentType: 'application/json',
    });

    expect(unexpectedHttpErrors, 'Unexpected browser HTTP 4xx/5xx responses').toEqual([]);
    expect(unobservedExpectedResponses, 'Expected HTTP responses not observed').toEqual([]);
    expect(unexpectedBrowserResourceErrors, 'Unmatched browser resource errors').toEqual([]);
    expect(telemetry.consoleErrors, 'Unexpected browser console.error entries').toEqual([]);
    expect(telemetry.pageErrors, 'Unexpected uncaught browser page errors').toEqual([]);
    expect(telemetry.requestFailures, 'Unexpected browser request failures').toEqual([]);
  }, { auto: true }],
});

export { expect } from '@playwright/test';
