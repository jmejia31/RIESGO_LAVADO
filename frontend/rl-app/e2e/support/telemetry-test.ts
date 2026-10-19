import { expect, test as base, type Page } from '@playwright/test';

type BrowserTelemetry = {
  consoleErrors: string[];
  pageErrors: string[];
  requestFailures: string[];
  httpErrors: Array<{ status: number; method: string; url: string }>;
};

export function expectHttpStatuses(...statuses: number[]): void {
  for (const status of statuses) {
    base.info().annotations.push({ type: 'expected-http-status', description: String(status) });
  }
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
      pageErrors: [],
      requestFailures: [],
      httpErrors: [],
    };
    const observedPages = new WeakSet<Page>();

    const observePage = (page: Page): void => {
      if (observedPages.has(page)) return;
      observedPages.add(page);
      page.on('console', message => {
        if (message.type() === 'error') telemetry.consoleErrors.push(safeConsoleMessage(message.text()));
      });
      page.on('pageerror', error => telemetry.pageErrors.push(safeConsoleMessage(error.message)));
      page.on('requestfailed', request => {
        telemetry.requestFailures.push(`${request.method()} ${safePath(request.url())} (${request.failure()?.errorText ?? 'unknown'})`);
      });
      page.on('response', response => {
        if (response.status() >= 400) {
          telemetry.httpErrors.push({
            status: response.status(),
            method: response.request().method(),
            url: safePath(response.url()),
          });
        }
      });
    };

    context.pages().forEach(observePage);
    context.on('page', observePage);
    await use();
    context.off('page', observePage);

    const expectedStatuses = testInfo.annotations
      .filter(annotation => annotation.type === 'expected-http-status')
      .map(annotation => Number(annotation.description));
    const unexpectedHttpErrors = telemetry.httpErrors.filter(error => {
      const expectedIndex = expectedStatuses.indexOf(error.status);
      if (expectedIndex < 0) return true;
      expectedStatuses.splice(expectedIndex, 1);
      return false;
    });

    await testInfo.attach('browser-telemetry', {
      body: JSON.stringify({ ...telemetry, unexpectedHttpErrors }, null, 2),
      contentType: 'application/json',
    });

    expect(telemetry.consoleErrors, 'Unexpected browser console.error entries').toEqual([]);
    expect(telemetry.pageErrors, 'Unexpected uncaught browser page errors').toEqual([]);
    expect(telemetry.requestFailures, 'Unexpected browser request failures').toEqual([]);
    expect(unexpectedHttpErrors, 'Unexpected browser HTTP 4xx/5xx responses').toEqual([]);
  }, { auto: true }],
});

export { expect } from '@playwright/test';
