import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  HostListener,
  OnDestroy,
  ViewChild,
  inject,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { AuthService } from '../../../core/services/auth';
import { ThemeService } from '../../../core/services/theme';
import { Icon, IconName } from '../../../shared/icon/icon';
import { Clientes } from '../clientes/clientes';
import { Suministros } from '../suministros/suministros';
import { Medidores } from '../medidores/medidores';
import { Facturacion } from '../facturacion/facturacion';
import { Usuarios } from '../usuarios/usuarios';
import { Pagos } from '../pagos/pagos';
import { Ordenes } from '../ordenes/ordenes';
import { Cuadrillas } from '../cuadrillas/cuadrillas';
import { Reclamos } from '../reclamos/reclamos';
import { Configuracion } from '../configuracion/configuracion';
import { Auditoria } from '../auditoria/auditoria';
import { DashboardService } from './dashboard.service';
import { PanelAdministrativo, ResultadoBusquedaGlobal } from './dashboard.model';

type AdminView =
  | 'inicio'
  | 'usuarios'
  | 'clientes'
  | 'suministros'
  | 'medidores'
  | 'facturacion'
  | 'pagos'
  | 'ordenes'
  | 'cuadrillas'
  | 'reclamos'
  | 'auditoria'
  | 'configuracion';

const ADMIN_VIEW_METADATA: Record<
  AdminView,
  { readonly titulo: string; readonly icono: IconName }
> = {
  inicio: { titulo: 'Inicio', icono: 'home' },
  usuarios: { titulo: 'Usuarios', icono: 'user' },
  clientes: { titulo: 'Clientes', icono: 'users' },
  suministros: { titulo: 'Suministros', icono: 'plug' },
  medidores: { titulo: 'Medidores', icono: 'gauge' },
  facturacion: { titulo: 'Facturación', icono: 'file-text' },
  pagos: { titulo: 'Pagos', icono: 'card' },
  ordenes: { titulo: 'Órdenes de trabajo', icono: 'clipboard' },
  cuadrillas: { titulo: 'Cuadrillas', icono: 'users' },
  reclamos: { titulo: 'Reclamos', icono: 'message' },
  auditoria: { titulo: 'Auditoría y reportes', icono: 'file-text' },
  configuracion: { titulo: 'Configuración', icono: 'settings' },
};

interface NavigationItem {
  readonly label: string;
  readonly icon: IconName;
  readonly available: boolean;
  readonly route?: string;
  readonly view?: AdminView;
}

interface ConsumoMensual {
  readonly mes: string;
  readonly consumoKwh: number;
  readonly valorTexto: string;
  readonly altura: number;
}

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [
    FormsModule,
    RouterLink,
    Icon,
    Usuarios,
    Clientes,
    Suministros,
    Medidores,
    Facturacion,
    Pagos,
    Ordenes,
    Cuadrillas,
    Reclamos,
    Configuracion,
    Auditoria,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnDestroy {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly dashboardService = inject(DashboardService);

  readonly theme = inject(ThemeService);
  readonly vistaActual = (this.route.snapshot.data['vista'] ?? 'inicio') as AdminView;
  readonly tituloModulo = ADMIN_VIEW_METADATA[this.vistaActual].titulo;
  readonly iconoModulo = ADMIN_VIEW_METADATA[this.vistaActual].icono;
  readonly menuAbierto = signal(false);
  readonly perfilAbierto = signal(false);
  readonly ayudaVisible = signal(false);
  readonly consulta = signal('');
  readonly mensajeBusqueda = signal('');
  readonly cargandoPanel = signal(false);
  readonly errorPanel = signal('');
  readonly panel = signal<PanelAdministrativo | null>(null);
  readonly resultadosBusqueda = signal<ResultadoBusquedaGlobal[]>([]);

  @ViewChild('menuTrigger')
  private menuTrigger?: ElementRef<HTMLButtonElement>;

  @ViewChild('sidebar')
  private sidebar?: ElementRef<HTMLElement>;

  @ViewChild('profileTrigger')
  private profileTrigger?: ElementRef<HTMLButtonElement>;

  @ViewChild('profileMenu')
  private profileMenu?: ElementRef<HTMLElement>;

  @ViewChild('logoutButton')
  private logoutButton?: ElementRef<HTMLButtonElement>;

  @ViewChild('searchInput')
  private searchInput?: ElementRef<HTMLInputElement>;

  readonly navigation: readonly NavigationItem[] = [
    { label: 'Inicio', icon: 'home', available: true, route: '/admin', view: 'inicio' },
    {
      label: 'Usuarios',
      icon: 'user',
      available: true,
      route: '/admin/usuarios',
      view: 'usuarios',
    },
    {
      label: 'Clientes',
      icon: 'users',
      available: true,
      route: '/admin/clientes',
      view: 'clientes',
    },
    {
      label: 'Suministros',
      icon: 'plug',
      available: true,
      route: '/admin/suministros',
      view: 'suministros',
    },
    {
      label: 'Medidores',
      icon: 'gauge',
      available: true,
      route: '/admin/medidores',
      view: 'medidores',
    },
    {
      label: 'Facturación',
      icon: 'file-text',
      available: true,
      route: '/admin/facturacion',
      view: 'facturacion',
    },
    { label: 'Pagos', icon: 'card', available: true, route: '/admin/pagos', view: 'pagos' },
    {
      label: 'Órdenes de trabajo',
      icon: 'clipboard',
      available: true,
      route: '/admin/ordenes',
      view: 'ordenes',
    },
    {
      label: 'Cuadrillas',
      icon: 'users',
      available: true,
      route: '/admin/cuadrillas',
      view: 'cuadrillas',
    },
    {
      label: 'Reclamos',
      icon: 'message',
      available: true,
      route: '/admin/reclamos',
      view: 'reclamos',
    },
    { label: 'Auditoría y reportes', icon: 'file-text', available: true, route: '/admin/auditoria', view: 'auditoria' },
    { label: 'Configuración', icon: 'settings', available: true, route: '/admin/configuracion', view: 'configuracion' },
  ];

  readonly historialConsumo = signal<readonly ConsumoMensual[]>([]);

  readonly nombreUsuario: string;
  readonly iniciales: string;

  constructor() {
    const usuario = this.authService.getUsuario();
    this.nombreUsuario = usuario?.nombreCompleto || 'Administrador';
    this.iniciales = this.obtenerIniciales(this.nombreUsuario);
    if (this.vistaActual === 'inicio') this.cargarPanel();
  }

  ngOnDestroy(): void {
    document.body.style.removeProperty('overflow');
  }

  abrirMenu(): void {
    this.menuAbierto.set(true);
    document.body.style.overflow = 'hidden';
    queueMicrotask(() => {
      const firstButton =
        this.sidebar?.nativeElement.querySelector<HTMLButtonElement>('.sidebar-close');
      firstButton?.focus();
    });
  }

  cerrarMenu(restaurarFoco = true): void {
    this.menuAbierto.set(false);
    document.body.style.removeProperty('overflow');

    if (restaurarFoco) {
      queueMicrotask(() => this.menuTrigger?.nativeElement.focus());
    }
  }

  alternarPerfil(event: Event): void {
    event.stopPropagation();
    this.perfilAbierto.update((abierto) => !abierto);

    if (this.perfilAbierto()) {
      queueMicrotask(() => this.logoutButton?.nativeElement.focus());
    }
  }

  alternarAyuda(): void {
    this.ayudaVisible.update((visible) => !visible);
  }

  actualizarConsulta(value: string): void {
    this.consulta.set(value);
    this.mensajeBusqueda.set('');
    this.resultadosBusqueda.set([]);
  }

  buscar(): void {
    const termino = this.consulta().trim();
    if (termino.length < 2) {
      this.mensajeBusqueda.set('Escribe al menos 2 caracteres para buscar.');
      return;
    }
    this.mensajeBusqueda.set('Buscando…');
    this.dashboardService.buscar(termino).subscribe({
      next: (items) => {
        this.resultadosBusqueda.set(items);
        this.mensajeBusqueda.set(items.length ? `${items.length} resultado(s) para “${termino}”.` : `No se encontraron coincidencias para “${termino}”.`);
      },
      error: () => this.mensajeBusqueda.set('No fue posible realizar la búsqueda.'),
    });
  }

  limpiarBusqueda(): void {
    this.consulta.set('');
    this.mensajeBusqueda.set('');
    this.resultadosBusqueda.set([]);
    queueMicrotask(() => this.searchInput?.nativeElement.focus());
  }

  cerrarSesion(): void {
    this.authService.logout();
    void this.router.navigate(['/login']);
  }

  cargarPanel(): void {
    this.cargandoPanel.set(true);
    this.errorPanel.set('');
    this.dashboardService.obtener().subscribe({
      next: (data) => {
        this.panel.set(data);
        const maximo = Math.max(1, ...data.consumoMensual.map((x) => x.consumoKwh));
        const meses = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'];
        this.historialConsumo.set(data.consumoMensual.map((x) => ({
          mes: `${meses[x.mes - 1]} ${String(x.anio).slice(-2)}`,
          consumoKwh: x.consumoKwh,
          valorTexto: x.totalFacturado.toLocaleString('es-EC', { style: 'currency', currency: 'USD' }),
          altura: Math.max(8, Math.round((x.consumoKwh / maximo) * 100)),
        })));
        this.cargandoPanel.set(false);
      },
      error: () => { this.errorPanel.set('No se pudo cargar el resumen operativo.'); this.cargandoPanel.set(false); },
    });
  }

  irAResultado(ruta: string): void {
    void this.router.navigateByUrl(ruta);
  }

  fechaLocal(fecha: string | null): string {
    return fecha ? new Date(fecha).toLocaleString('es-EC', { timeZone: 'America/Guayaquil' }) : 'Sin límite configurado';
  }

  @HostListener('document:click', ['$event'])
  cerrarPerfilDesdeFuera(event: MouseEvent): void {
    const target = event.target;

    if (
      target instanceof Node &&
      (this.profileTrigger?.nativeElement.contains(target) ||
        this.profileMenu?.nativeElement.contains(target))
    ) {
      return;
    }

    this.perfilAbierto.set(false);
  }

  @HostListener('document:keydown', ['$event'])
  gestionarTeclado(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      if (this.menuAbierto()) {
        event.preventDefault();
        this.cerrarMenu();
        return;
      }

      if (this.perfilAbierto()) {
        event.preventDefault();
        this.perfilAbierto.set(false);
        queueMicrotask(() => this.profileTrigger?.nativeElement.focus());
      }
      return;
    }

    if (event.key !== 'Tab' || !this.menuAbierto() || !this.sidebar) {
      return;
    }

    const focusables = Array.from(
      this.sidebar.nativeElement.querySelectorAll<HTMLElement>(
        'button:not([disabled]), a[href], [tabindex]:not([tabindex="-1"])',
      ),
    );

    if (!focusables.length) {
      return;
    }

    const first = focusables[0];
    const last = focusables[focusables.length - 1];

    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  }

  private obtenerIniciales(nombre: string): string {
    return (
      nombre
        .split(/\s+/)
        .filter(Boolean)
        .slice(0, 2)
        .map((parte) => parte.charAt(0).toUpperCase())
        .join('') || 'A'
    );
  }
}
