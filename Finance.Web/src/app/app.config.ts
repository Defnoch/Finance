import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { API_BASE_URL } from './core/api/api-tokens';
import { environment } from '../environments/environment';
import { ApiClient } from './core/api/api-client';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    { provide: API_BASE_URL, useValue: environment.apiBaseUrl },
    ApiClient
  ]
};
