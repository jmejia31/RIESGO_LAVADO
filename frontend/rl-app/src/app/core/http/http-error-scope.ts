import { HttpContextToken } from '@angular/common/http';

export type HttpErrorScope = 'global' | 'local';

/** Requests default to the shell-wide error banner; operation forms can opt into local handling. */
export const HTTP_ERROR_SCOPE = new HttpContextToken<HttpErrorScope>(() => 'global');
