import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, catchError, debounceTime, distinctUntilChanged, finalize, forkJoin, of, switchMap } from 'rxjs';
import { Icon } from '../../../shared/icon/icon';
import { EventoAuditoriaResumen, ReporteOperativo } from './auditoria.model';
import { AuditoriaService } from './auditoria.service';

@Component({ selector: 'app-admin-auditoria', standalone: true, imports: [CommonModule, ReactiveFormsModule, Icon], templateUrl: './auditoria.html', styleUrl: './auditoria.css', changeDetection: ChangeDetectionStrategy.OnPush })
export class Auditoria {
  private readonly service = inject(AuditoriaService); private readonly destroyRef = inject(DestroyRef); private readonly recargar$ = new Subject<void>();
  readonly items = signal<EventoAuditoriaResumen[]>([]); readonly total = signal(0); readonly pagina = signal(1);
  readonly resumen = signal<ReporteOperativo | null>(null); readonly cargando = signal(true); readonly exportando = signal(false); readonly error = signal(''); readonly exito = signal('');
  readonly busqueda = new FormControl('', { nonNullable: true }); readonly desde = new FormControl('', { nonNullable: true }); readonly hasta = new FormControl('', { nonNullable: true });
  constructor() {
    this.recargar$.pipe(switchMap(() => { this.cargando.set(true); this.error.set(''); return forkJoin({ pagina: this.service.listar(this.busqueda.value.trim(), this.pagina(), this.desde.value, this.hasta.value), resumen: this.service.resumen() }).pipe(catchError(() => of(null)), finalize(() => this.cargando.set(false))); }), takeUntilDestroyed(this.destroyRef)).subscribe(x => { if (!x) { this.error.set('No se pudo consultar la auditoría.'); return; } this.items.set(x.pagina.items); this.total.set(x.pagina.total); this.resumen.set(x.resumen); });
    this.busqueda.valueChanges.pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef)).subscribe(() => { this.pagina.set(1); this.cargar(); });
    this.cargar();
  }
  cargar() { this.recargar$.next(); }
  aplicarFechas() { if (this.desde.value && this.hasta.value && this.desde.value > this.hasta.value) { this.error.set('La fecha inicial no puede ser posterior a la final.'); return; } this.pagina.set(1); this.cargar(); }
  limpiar() { this.busqueda.setValue(''); this.desde.setValue(''); this.hasta.setValue(''); this.pagina.set(1); this.cargar(); }
  anterior() { if (this.pagina() > 1) { this.pagina.update(x => x - 1); this.cargar(); } }
  siguiente() { if (this.pagina() * 10 < this.total()) { this.pagina.update(x => x + 1); this.cargar(); } }
  exportar() { this.exportando.set(true); this.error.set(''); this.service.exportar(this.desde.value, this.hasta.value).pipe(finalize(() => this.exportando.set(false)), takeUntilDestroyed(this.destroyRef)).subscribe({ next: r => { const url = URL.createObjectURL(r.body!); const a = document.createElement('a'); a.href = url; a.download = this.nombreArchivo(r.headers.get('content-disposition')); a.click(); URL.revokeObjectURL(url); this.exito.set('Reporte CSV generado.'); }, error: () => this.error.set('No se pudo exportar el reporte.') }); }
  fecha(x: string) { return new Date(x).toLocaleString('es-EC', { timeZone: 'America/Guayaquil' }); }
  dinero(x: number) { return x.toLocaleString('es-EC', { style: 'currency', currency: 'USD' }); }
  private nombreArchivo(h: string | null) { return h?.match(/filename\*?=(?:UTF-8'')?"?([^";]+)/i)?.[1] ?? 'auditoria-sgse.csv'; }
}
