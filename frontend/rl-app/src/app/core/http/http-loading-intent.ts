import { HttpContext, HttpContextToken } from '@angular/common/http';

export type HttpLoadingIntent =
  | 'INITIAL'
  | 'SILENT_REFRESH'
  | 'USER_ACTION'
  | 'CRITICAL_OPERATION';

/** Request accounting is separate from visual feedback in the application shell. */
export const HTTP_LOADING_INTENT = new HttpContextToken<HttpLoadingIntent>(
  () => 'SILENT_REFRESH'
);

export function createHttpLoadingContext(intent: HttpLoadingIntent): HttpContext {
  return new HttpContext().set(HTTP_LOADING_INTENT, intent);
}
