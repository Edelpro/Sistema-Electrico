import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from '../services/auth';

export const authGuard: CanActivateFn = (_route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.estaAutenticado()) {
    return true;
  }

  return router.createUrlTree(['/login'], {
    queryParams: { returnUrl: state.url },
  });
};

export const roleGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const usuario = authService.getUsuario();
  const roles = (route.data['roles'] as string[] | undefined) ?? [];

  if (!usuario) {
    return router.createUrlTree(['/login']);
  }

  return roles.includes(usuario.rol) ? true : router.createUrlTree(['/acceso-denegado']);
};
