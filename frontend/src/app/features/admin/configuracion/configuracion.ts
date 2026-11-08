import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, ElementRef, HostListener, ViewChild, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { Icon } from '../../../shared/icon/icon';
import { PoliticaSla } from './configuracion.model';
import { ConfiguracionService } from './configuracion.service';

@Component({
  selector: 'app-admin-configuracion',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, Icon],
  templateUrl: './configuracion.html',
  styleUrl: './configuracion.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Configuracion {
  private readonly service = inject(ConfiguracionService);
  readonly cargando = signal(true);
  readonly guardando = signal(false);
  readonly error = signal('');
  readonly exito = signal('');
  readonly politicas = signal<PoliticaSla[]>([]);
  readonly editorPolitica = signal(false);
  readonly confirmar = signal<PoliticaSla | null>(null);

  readonly general = new FormGroup({
    nombreEmpresa: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(160)] }),
    ruc: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(20)] }),
    correoSoporte: new FormControl('', { nonNullable: true, validators: [Validators.email, Validators.maxLength(160)] }),
    telefonoSoporte: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(30)] }),
    direccion: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(300)] }),
    zonaHoraria: new FormControl('America/Guayaquil', { nonNullable: true, validators: [Validators.required] }),
    tamanoMaximoEvidenciaMb: new FormControl(5, { nonNullable: true, validators: [Validators.required, Validators.min(1), Validators.max(20)] }),
    notificacionesCorreoActivas: new FormControl(false, { nonNullable: true }),
    notificacionesWhatsAppActivas: new FormControl(false, { nonNullable: true }),
    motivo: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(5), Validators.maxLength(300)] }),
  });
  readonly politica = new FormGroup({
    tipoOrden: new FormControl('RECONEXION', { nonNullable: true }),
    tipoZona: new FormControl('URBANA', { nonNullable: true }),
    horasObjetivo: new FormControl(10, { nonNullable: true, validators: [Validators.required, Validators.min(1), Validators.max(168)] }),
    horaInicioLaboral: new FormControl('08:00', { nonNullable: true, validators: [Validators.required] }),
    horaFinLaboral: new FormControl('17:00', { nonNullable: true, validators: [Validators.required] }),
    incluyeFinDeSemana: new FormControl(false, { nonNullable: true }),
    motivo: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(5), Validators.maxLength(300)] }),
  });
  readonly motivoDesactivacion = new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(5)] });
  @ViewChild('newPolicyButton') private newPolicyButton?: ElementRef<HTMLButtonElement>;
  @ViewChild('policyClose') private policyClose?: ElementRef<HTMLButtonElement>;

  constructor() { this.cargar(); }

  cargar() {
    this.cargando.set(true); this.error.set('');
    this.service.obtener().pipe(finalize(() => this.cargando.set(false))).subscribe({
      next: (x) => {
        this.politicas.set(x.politicasSla);
        this.general.reset({ ...x.general, motivo: '' });
      },
      error: () => this.error.set('No fue posible cargar la configuración.'),
    });
  }
  guardarGeneral() {
    this.general.markAllAsTouched();
    if (this.general.invalid) return;
    this.guardando.set(true); this.error.set(''); this.exito.set('');
    this.service.actualizarGeneral(this.general.getRawValue()).pipe(finalize(() => this.guardando.set(false))).subscribe({
      next: (x) => { this.general.patchValue({ ...x, motivo: '' }); this.exito.set('Configuración general guardada y auditada.'); },
      error: (e) => this.error.set(this.mensaje(e, 'No se pudo guardar la configuración.')),
    });
  }
  abrirPolitica() {
    this.politica.reset({ tipoOrden: 'RECONEXION', tipoZona: 'URBANA', horasObjetivo: 10,
      horaInicioLaboral: '08:00', horaFinLaboral: '17:00', incluyeFinDeSemana: false, motivo: '' });
    this.editorPolitica.set(true); this.error.set('');
    setTimeout(() => this.policyClose?.nativeElement.focus());
  }
  cerrarPolitica() { this.editorPolitica.set(false); setTimeout(() => this.newPolicyButton?.nativeElement.focus()); }
  crearPolitica() {
    this.politica.markAllAsTouched();
    const v = this.politica.getRawValue();
    if (this.politica.invalid || v.horaInicioLaboral >= v.horaFinLaboral) {
      if (v.horaInicioLaboral >= v.horaFinLaboral) this.error.set('La hora final debe ser posterior a la hora inicial.');
      return;
    }
    this.guardando.set(true); this.error.set('');
    this.service.crearPolitica(v).pipe(finalize(() => this.guardando.set(false))).subscribe({
      next: () => { this.editorPolitica.set(false); this.exito.set('Nueva versión de la política SLA creada.'); this.cargar(); },
      error: (e) => this.error.set(this.mensaje(e, 'No se pudo crear la política SLA.')),
    });
  }
  pedirDesactivar(x: PoliticaSla) { this.motivoDesactivacion.reset(''); this.confirmar.set(x); }
  desactivar() {
    const x = this.confirmar(); this.motivoDesactivacion.markAsTouched();
    if (!x || this.motivoDesactivacion.invalid) return;
    this.guardando.set(true);
    this.service.desactivarPolitica(x.id, this.motivoDesactivacion.value.trim())
      .pipe(finalize(() => this.guardando.set(false))).subscribe({
        next: () => { this.confirmar.set(null); this.exito.set('Política desactivada; su historial se conserva.'); this.cargar(); },
        error: (e) => this.error.set(this.mensaje(e, 'No se pudo desactivar la política.')),
      });
  }
  fecha(x: string | null) { return x ? new Date(x).toLocaleString('es-EC', { timeZone: 'America/Guayaquil' }) : 'Vigente'; }
  @HostListener('document:keydown.escape')
  cerrarOverlay() {
    if (this.confirmar()) this.confirmar.set(null);
    else if (this.editorPolitica()) this.cerrarPolitica();
  }
  private mensaje(e: any, fallback: string) {
    return e?.error?.mensaje || Object.values(e?.error?.errors ?? {}).flat().join(' ') || fallback;
  }
}
