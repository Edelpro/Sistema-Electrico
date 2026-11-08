import { Routes } from '@angular/router';

import { authGuard, roleGuard } from './core/guards/auth-guard';

const cargarAdmin = () =>
  import('./features/admin/dashboard/dashboard').then((modulo) => modulo.Dashboard);
const cargarTecnico = () =>
  import('./features/tecnico/dashboard/dashboard').then((modulo) => modulo.Dashboard);
const cargarUsuario = () =>
  import('./features/usuario/dashboard/dashboard').then((modulo) => modulo.Dashboard);

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full',
  },

  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login').then((modulo) => modulo.Login),
    title: 'Iniciar sesión — SGSE',
  },

  {
    path: 'recuperar-contrasena',
    loadComponent: () =>
      import('./features/auth/recuperacion/solicitar-recuperacion').then(
        (modulo) => modulo.SolicitarRecuperacion,
      ),
    title: 'Recuperar contraseña — SGSE',
  },

  {
    path: 'restablecer-contrasena',
    loadComponent: () =>
      import('./features/auth/recuperacion/restablecer-password').then(
        (modulo) => modulo.RestablecerPassword,
      ),
    title: 'Nueva contraseña — SGSE',
  },

  {
    path: 'admin/usuarios',
    loadComponent: cargarAdmin,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Administrador'], vista: 'usuarios' },
    title: 'Usuarios — SGSE',
  },

  {
    path: 'admin/clientes',
    loadComponent: cargarAdmin,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Administrador'], vista: 'clientes' },
    title: 'Clientes — SGSE',
  },

  {
    path: 'admin/suministros',
    loadComponent: cargarAdmin,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Administrador'], vista: 'suministros' },
    title: 'Suministros — SGSE',
  },

  {
    path: 'admin/medidores',
    loadComponent: cargarAdmin,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Administrador'], vista: 'medidores' },
    title: 'Medidores — SGSE',
  },

  {
    path: 'admin/facturacion',
    loadComponent: cargarAdmin,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Administrador'], vista: 'facturacion' },
    title: 'Facturación — SGSE',
  },

  {
    path: 'admin/pagos',
    loadComponent: cargarAdmin,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Administrador'], vista: 'pagos' },
    title: 'Pagos — SGSE',
  },

  {
    path: 'admin/ordenes',
    loadComponent: cargarAdmin,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Administrador'], vista: 'ordenes' },
    title: 'Órdenes de trabajo — SGSE',
  },

  {
    path: 'admin/cuadrillas',
    loadComponent: cargarAdmin,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Administrador'], vista: 'cuadrillas' },
    title: 'Cuadrillas — SGSE',
  },

  {
    path: 'admin/reclamos',
    loadComponent: cargarAdmin,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Administrador'], vista: 'reclamos' },
    title: 'Reclamos — SGSE',
  },

  {
    path: 'admin/auditoria',
    loadComponent: cargarAdmin,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Administrador'], vista: 'auditoria' },
    title: 'Auditoría y reportes — SGSE',
  },

  {
    path: 'admin/configuracion',
    loadComponent: cargarAdmin,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Administrador'], vista: 'configuracion' },
    title: 'Configuración — SGSE',
  },

  {
    path: 'admin',
    loadComponent: cargarAdmin,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Administrador'], vista: 'inicio' },
    title: 'Resumen operativo — SGSE',
  },

  {
    path: 'tecnico',
    loadComponent: cargarTecnico,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Tecnico'] },
    title: 'Panel técnico — SGSE',
  },

  {
    path: 'usuario',
    loadComponent: cargarUsuario,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Usuario'] },
    title: 'Mi servicio — SGSE',
  },

  {
    path: 'acceso-denegado',
    loadComponent: () =>
      import('./features/errors/forbidden/forbidden').then((modulo) => modulo.Forbidden),
    title: 'Acceso no autorizado — SGSE',
  },

  {
    path: '**',
    redirectTo: 'login',
  },
];
