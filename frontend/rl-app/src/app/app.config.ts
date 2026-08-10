import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { confirmacionCambiosInterceptor } from './core/interceptors/confirmacion-cambios.interceptor';
import { httpResilienceInterceptor } from './core/interceptors/http-resilience.interceptor';

import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([confirmacionCambiosInterceptor, httpResilienceInterceptor, authInterceptor]))
  ]
};

