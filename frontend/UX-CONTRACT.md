# UX Contract

## Product context

- Audience: administradores, técnicos y usuarios del servicio eléctrico.
- Primary jobs: iniciar sesión según rol; consultar y gestionar clientes, suministros, pagos, órdenes y reconexiones.
- Target market(s): Ecuador.
- Active locales: `es-EC`.
- Language/content register and native-review policy: español claro, operativo y sin anglicismos evitables; revisión de dominio pendiente antes de producción.
- Timezone/calendar policy: almacenamiento UTC en API y presentación `America/Guayaquil` con calendario gregoriano.
- Accessibility target: WCAG 2.2 AA.

## Business-context sources

| Domain / scope                      | Authoritative source                                                                                                           | Source type                                       | Reviewed date |
| ----------------------------------- | ------------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------- | ------------- |
| Permission model                    | `../backend/Models/Rol.cs`, `../backend/Models/Usuario.cs`, `../backend/Controllers/AuthController.cs`                         | API / modelo de dominio                           | 2026-09-27    |
| Authentication and roles            | `../backend/Controllers/AuthController.cs`, `../backend/Services/TokenService.cs`                                              | API contract / implementation                     | 2026-09-27    |
| User directory                      | `../backend/Controllers/UsuariosController.cs`, `../backend/Models/Usuario.cs`                                                 | API contract / implementation                     | 2026-09-29    |
| Client directory                    | `../backend/Controllers/ClientesController.cs`, `../backend/Models/Cliente.cs`                                                 | API contract / implementation                     | 2026-09-29    |
| Supply directory                    | `../backend/Controllers/SuministrosController.cs`, `../backend/Models/Suministro.cs`                                           | API contract / implementation                     | 2026-09-28    |
| Meter directory                     | `../backend/Controllers/MedidoresController.cs`, `../backend/Models/Medidor.cs`                                                | API contract / implementation                     | 2026-09-29    |
| Billing import and consultation     | `../backend/Controllers/FacturasController.cs`, `../../sgcr-ecuador/docs/SRS.md`, `../../sgcr-ecuador/docs/MODELO-PLANILLA.md` | API + SRS / data dictionary                       | 2026-10-01    |
| Crew management                     | `../backend/Controllers/CuadrillasController.cs`, `../backend/Models/Cuadrilla.cs`                                             | API contract / implementation                     | 2026-10-04    |
| Claims and service support          | `../backend/Controllers/ReclamosController.cs`, `../backend/Models/Reclamo.cs`                                                 | API contract / implementation                     | 2026-10-04    |
| Customer self-service portal        | `../backend/Controllers/PortalUsuarioController.cs`, `../../sgcr-ecuador/docs/SRS.md`                                          | API contract + SRS                                | 2026-10-04    |
| Technician field portal             | `../backend/Controllers/PortalTecnicoController.cs`, `../backend/Models/EvidenciaOrden.cs`                                     | API contract + domain model                       | 2026-10-04    |
| Product roles and operational scope | `../../sgcr-ecuador/docs/SRS.md`                                                                                               | SRS                                               | 2026-09-27    |
| Data lifecycle                      | Clients, accounts and supplies use reversible activation; meters preserve history; imported invoices are immutable             | API contract / SRS                                | 2026-10-01    |
| Deletion / retention                | Clients, accounts, supplies and meters are never hard-deleted; imported invoices cannot be edited or deleted                   | Current-task product decision + SRS RN-014/RN-016 | 2026-10-01    |
| Billing / payment                   | SRS plus future API endpoints; no payment mutation is introduced in this scope                                                 | Domain specification                              | 2026-09-27    |
| Legal / regulatory copy             | No authoritative legal copy supplied; none is invented                                                                         | Unresolved / deferred                             | 2026-09-27    |
| Market / content conventions        | User brief and SRS identify Ecuador and electrical-service terminology                                                         | Product brief                                     | 2026-09-27    |

## Visual contract

- Project `DESIGN.md`: `DESIGN.md`.
- Token ownership model: existing runtime canonical.
- Runtime design-system/token source: `src/styles.css`.
- Mapping/export/adapters: semantic CSS variables consumed by screen styles and `app-icon`.
- Token drift gate: `designmd lint DESIGN.md`, production build and visual comparison.
- Supported themes: light and dark; system preference seeds the first visit.
- Design-context owner/review policy: update `DESIGN.md` and runtime variables in the same change when a durable visual decision changes.

## Canonical UI Map

| Capability     | Canonical owner                                                                                      | Source of truth                    | Allowed variants                                          | Verification                                                            |
| -------------- | ---------------------------------------------------------------------------------------------------- | ---------------------------------- | --------------------------------------------------------- | ----------------------------------------------------------------------- |
| Form           | Angular Reactive Forms in Login and admin CRUD modules                                               | This contract + Angular validators | login, create, edit                                       | invalid-submit, field association, server failure, duplicate prevention |
| Scrollbar      | Global rules in `src/styles.css`                                                                     | `DESIGN.md`                        | compact geometry only                                     | computed style + browser inspection                                     |
| Data table     | Admin Usuarios, Clientes, Suministros, Medidores, Facturación, Pagos, Órdenes, Cuadrillas y Reclamos | This contract + module API         | CRUD, operational workflow or immutable import, paginated | loading, empty, no-results, error, mobile overflow, paging              |
| Dialog         | Admin CRUD confirmation dialogs                                                                      | This contract                      | discard, state change                                     | focus trap, Escape, pending, error, focus restoration                   |
| CRUD           | Admin Clientes, Usuarios, Suministros and Medidores                                                  | This contract + module APIs        | create, edit, reversible state                            | full browser flow + API authorization                                   |
| Select/Listbox | Native select in roles, states, priorities and classifications                                       | This contract                      | platform-owned popup accepted                             | keyboard, label, open popup, mobile                                     |
| Date           | Native date/datetime input for installation, billing and deadlines                                   | This contract                      | ISO/local value with platform picker                      | range validation, keyboard, browser                                     |

Table selection, authored listbox and toast remain unresolved. Native select and date-picker popups are explicitly accepted because product-specific popup geometry is not required; persistent inline success/error feedback remains canonical.

## Component behavior

| Component    | Default                         | Hover                 | Focus           | Active                   | Disabled                  | Busy                       | Error                           |
| ------------ | ------------------------------- | --------------------- | --------------- | ------------------------ | ------------------------- | -------------------------- | ------------------------------- |
| Button       | semantic role and visible label | tonal lift            | 3 px focus ring | no layout shift          | native disabled and muted | fixed-size spinner + label | persistent contextual message   |
| Icon button  | 44 px important target          | tinted surface        | visible ring    | pressed feedback         | native disabled           | n/a                        | n/a                             |
| Input        | 52 px, visible label            | stronger border       | blue ring       | n/a                      | muted                     | n/a                        | `aria-invalid` + linked message |
| Secret input | masked                          | as input              | as input        | reveal keeps value/focus | n/a                       | n/a                        | as input                        |
| Search       | local query + clear             | clear control visible | visible ring    | Enter is non-destructive | n/a                       | n/a                        | live no-results status          |

## Dataset navigation

- Admin Usuarios, Clientes, Suministros, Medidores, Facturación, Pagos, Órdenes, Cuadrillas y Reclamos: server pagination with 10 rows per page; each API accepts a maximum of 50.
- URL state: admin CRUD modules preserve the committed search and page in `q` and `pagina`; the current home search remains local and non-sensitive.
- Empty/no-results/error/loading treatment: distinguish no connected data, no matching results and request failure.
- Selection scope: not applicable.

## Flow ledger

| Operation                      | Trigger                                 | Pending                              | Success destination                          | Success feedback  | Failure recovery                                                                                | Focus outcome                          | Source ref                   |
| ------------------------------ | --------------------------------------- | ------------------------------------ | -------------------------------------------- | ----------------- | ----------------------------------------------------------------------------------------------- | -------------------------------------- | ---------------------------- |
| Sign in                        | “Iniciar sesión”                        | button disabled with stable spinner  | role home (`/admin`, `/tecnico`, `/usuario`) | route change      | inline generic credential or connection error; preserve email and clear password                | first invalid field or page heading    | `AuthController.cs` + routes |
| Sign out                       | “Cerrar sesión”                         | immediate local clear                | `/login`                                     | route change      | n/a                                                                                             | login heading                          | `AuthService.logout()`       |
| Search home                    | search field / Enter                    | none; local-only                     | same route                                   | live status text  | clear query                                                                                     | search input                           | current dashboard only       |
| Consult users                  | Usuarios navigation / search field      | stable loading region                | `/admin/usuarios` with query state           | result count      | persistent error with retry; clear search for no-results                                        | search input or list region            | `UsuariosController.cs`      |
| Create login account           | “Crear cuenta”                          | stable busy button                   | same Usuarios list                           | inline success    | preserve entered values; map duplicate email/ID and validation errors                           | success message / create trigger       | `UsuariosController.cs`      |
| Edit login account             | Row action “Editar”                     | detail load, then stable busy button | same Usuarios list                           | inline success    | preserve values; require audit reason; reject protected administrator                           | success message / row trigger          | `UsuariosController.cs`      |
| Change account state           | Row action “Desactivar/Reactivar”       | confirmation action stays busy       | same Usuarios list                           | inline success    | dialog retains reason and API error; account sessions are rejected after disable                | success message / row trigger          | `UsuariosController.cs`      |
| Consult clients                | Clientes navigation / search field      | stable loading region                | `/admin/clientes` with query state           | result count      | persistent error with retry; clear search for no-results                                        | search input or list region            | `ClientesController.cs`      |
| Create client                  | “Crear cliente”                         | stable busy button                   | same Clientes list                           | inline success    | preserve entered values; inline field and form errors                                           | success message / create trigger       | `ClientesController.cs`      |
| Edit client                    | Row action “Editar”                     | detail load, then stable busy button | same Clientes list                           | inline success    | preserve entered values; inline field and form errors                                           | success message / row trigger          | `ClientesController.cs`      |
| Change client state            | Row action “Desactivar/Reactivar”       | confirmation action stays busy       | same Clientes list                           | inline success    | dialog remains open with reason and API error; retry/cancel                                     | success message / row trigger          | `ClientesController.cs`      |
| Consult supplies               | Suministros navigation / search field   | stable loading region                | `/admin/suministros` with query state        | result count      | persistent error with retry; clear search for no-results                                        | search input or list region            | `SuministrosController.cs`   |
| Create/edit supply             | “Crear suministro” / “Editar”           | stable busy button                   | same Suministros list                        | inline success    | preserve values; map duplicate identifiers and inactive/unknown holder                          | success message / row trigger          | `SuministrosController.cs`   |
| Change supply state            | “Desactivar/Reactivar”                  | confirmation action stays busy       | same Suministros list                        | inline success    | block deactivation while an active meter exists; retain reason and API error                    | success message / row trigger          | `SuministrosController.cs`   |
| Consult meters                 | Medidores navigation / search field     | stable loading region                | `/admin/medidores` with query state          | result count      | persistent error with retry; clear search for no-results                                        | search input or list region            | `MedidoresController.cs`     |
| Create/edit meter              | “Registrar medidor” / “Editar”          | stable busy button                   | same Medidores list                          | inline success    | preserve values; prevent duplicate serial and two active meters per supply                      | success message / row trigger          | `MedidoresController.cs`     |
| Change meter state             | “Cambiar estado”                        | confirmation action stays busy       | same Medidores list                          | inline success    | preserve reason; validate active supply and one-active-meter invariant                          | success message / row trigger          | `MedidoresController.cs`     |
| Consult invoices               | Facturación navigation / search field   | stable loading region                | `/admin/facturacion` with query state        | result count      | persistent error with retry; clear search for no-results                                        | search input or list region            | `FacturasController.cs`      |
| Import invoice                 | “Importar factura”                      | stable busy button                   | same Facturación list                        | inline success    | preserve values; validate supply/meter; repeated source + external id returns existing document | success message / import trigger       | SRS RN-016/RF-FAC-001        |
| View invoice detail            | “Ver detalle”                           | stable loading region                | inline read-only detail                      | none              | keep list usable and report load failure without exposing raw payload                           | detail heading / row trigger           | SRS RF-FAC-002–004           |
| Manage crews                   | “Nueva cuadrilla” / “Editar”            | stable busy button                   | same Cuadrillas list                         | inline success    | preserve values; validate unique code, capacity, zone and reversible state                      | success message / trigger              | `CuadrillasController.cs`    |
| Add/retire crew member         | detail member actions                   | action remains busy                  | same crew detail                             | inline success    | active technician only; prevent duplicate active membership and unsafe retirement               | detail heading / action trigger        | `CuadrillasController.cs`    |
| Create claim                   | “Nuevo reclamo”                         | stable busy button                   | same Reclamos list                           | inline success    | preserve values; validate supply ownership and linked payment/order                             | success message / create trigger       | `ReclamosController.cs`      |
| Manage claim                   | detail state action                     | action remains busy                  | same claim detail                            | inline success    | only allowed transitions; resolution required before resolve/close                              | detail heading / action trigger        | `ReclamosController.cs`      |
| Open/close mobile navigation   | menu button / Escape / backdrop / close | transition only                      | same route                                   | visible drawer    | Escape/backdrop available                                                                       | first close control / original trigger | navigation contract          |
| Consult own electrical service | Usuario role home                       | stable loading panel                 | `/usuario`                                   | account summary   | explicit unlinked-account message; retry after administrator links the client                   | page heading                           | `PortalUsuarioController.cs` |
| Create own support claim       | “Nuevo reclamo” in user portal          | committing button remains disabled   | same selected supply                         | persistent banner | preserve description; validate ownership of linked payment/order                                | success banner / form trigger          | `PortalUsuarioController.cs` |
| Manage assigned field order    | “Gestionar” in technician portal        | stable pending state                 | same technician order drawer                 | persistent banner | only assigned/direct-crew orders and valid state transitions; preserve entered reason           | drawer heading / order trigger         | `PortalTecnicoController.cs` |
| Register technical evidence    | “Registrar evidencia de campo”          | upload action remains disabled       | same technician order drawer                 | persistent banner | JPG/PNG/WebP up to 5 MB; preserve form on API failure; execution blocked without evidence       | evidence form                          | `PortalTecnicoController.cs` |

## Navigation and responsive behavior

- Route document title policy: `{Página} — SGSE` through Angular route titles.
- Route error / 403 page behavior: direct access with an authenticated but disallowed role shows `/acceso-denegado`; unauthenticated access returns to `/login`.
- Sidebar/drawer transformation: persistent at desktop; modal left drawer below 900 px with backdrop, Escape, focus containment and focus restoration.
- Responsive table strategy: activity headers become a labeled empty-state region on narrow screens; admin CRUD modules preserve column relationships with an announced horizontal-scroll region.
- Admin home consumption chart remains demonstrative. The customer portal chart is derived only from that customer's invoice consumption and labels its unit.
- Truncation/full-value access: names wrap; important values are not hover-only.
- Focus restoration and sticky-obstruction policy: overlays restore the trigger and sticky bars never cover focused controls.

## Overlays and feedback

- Dialog primitive: app-owned alert dialog in Clientes and Usuarios for discard and state-change confirmation.
- Toast placement/duration/deduplication: unresolved until a shared provider is introduced; login errors remain inline.
- Alert/banner scope and persistence: authentication errors persist in the form until edit/retry.
- Tooltip delay/dismissal: no essential information depends on a tooltip.
- Layer/z-index contract: sticky 100, dropdown/popover 200–399, mobile backdrop 500, drawer 700.

## Async and resilience

- Mutation default: pessimistic. All admin CRUD mutations wait for API confirmation.
- Idempotency and duplicate-submit policy: login and admin CRUD mutations return early while pending and disable their committing control.
- Offline/read-stale/write behavior: connection failure receives a distinct inline message; no offline login is claimed.
- Retry/backoff/timeout behavior: user-initiated retry; no automatic credential retry.
- Session expiry/re-authentication: auth guard redirects to login; future 401 interceptor work must preserve safe state.
- Stale-request cancellation/invalidation and pending-state ownership: one login, client or account mutation at a time; list searches use RxJS `switchMap`, and component teardown cancels subscriptions.

## Validation

- Schema/validation layer: Angular Reactive Forms plus API validation for all admin CRUD forms.
- Trigger timing: submit first, then touched-field updates.
- Error summary/inline policy: inline per-field errors plus one form-level server banner.
- Server error mapping: 401 uses a generic credential message; network failure has a connection message; raw payloads are not exposed.
- Sensitive-value handling: passwords are never returned, listed or audited; login clears its password after failure, while account creation preserves the unsaved form only for correction/retry. JWT and user response remain in session storage per existing architecture.
- `noValidate`, first-invalid focus, duplicate-submit prevention and submit recovery: required for Login and all admin CRUD forms. CRUD forms warn before discarding dirty data and on page unload.

## Permission and clipboard

- Permission UI strategy: route guard for authentication and role guard for direct access. Administrator manages master/operational data; Technician only receives assigned-order actions; Usuario only receives read access to linked service data plus support-claim creation.
- Disabled-state explanation: planned sidebar modules use visible “Pendiente” context rather than enabled false affordances.
- Security authority: backend authorization remains required; client guards are only a UX boundary. The Usuarios API enforces Administrator access, rejects administrator mutation and revalidates active-account state for authenticated requests.
- Object-level authority: Usuario access is scoped by the explicit `Usuario.ClienteId` link; Técnico access is scoped by direct assignment or active crew membership. The UI never supplies an unrestricted customer/technician identifier as authority.

## Verification

- Required static commands: strict premium audit, anti-pattern scans, `npm test`, `npm run build`.
- Browser/device/locale/theme matrix: desktop 1440×900 and mobile 390×844; Login + Admin Inicio + Clientes + Usuarios; light + dark; Spanish copy.
- Accessibility checks: keyboard submission, first-error focus, password reveal name, menu/dialog Escape and focus restoration, semantic controls.
- Canonical sibling flow used for comparison: role routing already present in Login and existing Admin dashboard.
- Project audit command/result: recorded at handoff.
- CRUD full-flow evidence: create, read/list/detail, update, deactivate and reactivate in Clientes and Usuarios; identity is masked in lists and complete only in authorized detail/edit.
- Failure-path evidence: invalid form, duplicate identification/email, unavailable list API, dirty-form discard, protected Administrator and failed state transition inspected in browser.

# Extensión operativa: Pagos y Órdenes de trabajo (2026-10-03)

- **Pagos:** registro inmutable e idempotente por proveedor + evento; la conciliación manual exige motivo y asignación explícita a una o más facturas. No ofrece editar ni eliminar.
- **Efecto operativo atómico:** un pago confirmado cancela un corte pendiente; si el corte ya fue ejecutado, crea o reutiliza una única reconexión activa.
- **Órdenes:** estados permitidos `CREADA → ASIGNADA → ACEPTADA → EN_RUTA → EN_SITIO → EJECUTADA/CERRADA`, con ramas `FALLIDA → REPROGRAMADA` y cancelación antes de ejecutar.
- **Seguridad operacional:** toda creación, asignación y transición exige motivo. Una reconexión no puede marcarse ejecutada sin el futuro módulo de evidencia técnica.
- **SLA:** la fecha límite es opcional y depende de una política autorizada; la interfaz no presenta 10/24 horas como compromiso legal.
- **Fecha y hora:** los controles nativos capturan hora local; la API conserva UTC y la interfaz presenta `es-EC`.
- **Estados de interfaz:** ambos módulos contemplan carga, vacío, sin resultados, error recuperable, éxito, detalle y guardado bloqueado.

# Extensión operativa: Cuadrillas y Reclamos (2026-10-04)

- **Cuadrillas:** administra código, nombre, zona, capacidad, disponibilidad y miembros técnicos. No cubre nómina, contratación, flota ni inventario.
- **Integridad operativa:** un técnico solo puede pertenecer activamente a una cuadrilla; no se desactiva una cuadrilla con órdenes activas ni se retira un técnico con trabajo activo asignado en ella.
- **Asignación:** una orden puede asignarse a un técnico, una cuadrilla o ambos; la reasignación no retrocede una orden que ya fue aceptada o iniciada.
- **Reclamos:** registra casos por pago no reflejado, reconexión demorada, facturación u otro motivo, con vínculo opcional a pago u orden del mismo suministro.
- **Trazabilidad:** cada cambio de estado o asignación conserva historial inmutable, motivo, responsable y fecha UTC. Resolver o cerrar exige una resolución escrita.
- **Retención:** cuadrillas, miembros, reclamos, pagos y órdenes no se eliminan físicamente; se usan estados y bajas reversibles cuando corresponda.
- **Validación y UX:** búsqueda cancelable, paginación, vacíos diferenciados, errores persistentes, formularios con validación en cliente y servidor, y acciones irreversibles protegidas por confirmación propia de la aplicación.

# Portales por rol y evidencia técnica (actualizado 2026-10-05)

- **Administrador:** conserva la gestión completa de clientes, cuentas, suministros, medidores, facturas, pagos, órdenes, cuadrillas y reclamos. Al crear una cuenta `Usuario` debe vincularla a un cliente activo mediante su identificación.
- **Técnico:** consulta únicamente órdenes asignadas directamente o mediante una cuadrilla activa; puede avanzar solo por transiciones permitidas, registrar ubicación/lectura/observación/fotografía y no puede cerrar, cancelar, reasignar ni modificar pagos.
- **Usuario:** consulta exclusivamente suministros, saldos, facturas, pagos, órdenes, SLA publicado y reclamos del cliente vinculado. Puede crear reclamos propios, pero no alterar pagos, facturas, órdenes ni evidencia.
- **Evidencia:** una orden no puede marcarse `EJECUTADA` sin al menos una fotografía técnica. El archivo se guarda con nombre aleatorio y hash SHA-256; coordenadas y precisión permanecen restringidas al flujo técnico/administrativo.
- **Descarga protegida:** las evidencias no se exponen mediante enlaces públicos. Usuario y Técnico las descargan con una solicitud autenticada; la API vuelve a comprobar propiedad del suministro o asignación de la orden.
- **Cierre de trabajo:** `FALLIDA` exige motivo y una observación que explique la novedad; `EJECUTADA` permanece deshabilitada hasta que exista evidencia. Las mismas reglas se validan otra vez en la API.
- **Formularios de campo:** aceptan únicamente JPG, PNG o WebP de hasta 5 MB, muestran errores dentro del cajón, preservan valores tras un fallo y enfocan el primer dato inválido.
- **Cajones modales:** contienen el foco, se cierran con Escape o el control visible y devuelven el foco al botón que los abrió.
- **Separación de responsabilidades:** la navegación y los controles son distintos por rol y la API repite las mismas restricciones para impedir acceso horizontal aunque se manipule la URL.

# Panel operativo y configuración versionada (actualizado 2026-10-05)

- **Panel real:** Inicio consume clientes activos, órdenes pendientes, cuadrillas disponibles, alertas SLA, reconexiones prioritarias, facturación mensual y auditoría desde la API; no presenta datos demostrativos.
- **Búsqueda global:** admite cliente, identificación, suministro, contrato, CUEN u orden; cada resultado indica su tipo y abre el módulo canónico con la consulta conservada en la URL.
- **Configuración general:** los cambios institucionales, zona horaria, tamaño máximo de evidencia y activación de canales requieren motivo y generan auditoría.
- **Políticas SLA:** cada combinación de tipo de orden y zona mantiene una sola versión activa. Crear una versión cierra la anterior; desactivar conserva el historial y exige motivo.
- **Cálculo de reconexión:** al confirmar un pago que requiere reconexión, la API fija el límite usando horas laborables, jornada y política activa urbana/rural. Sin política, la orden se crea y declara explícitamente que el límite no pudo calcularse.
- **Canales externos:** correo y WhatsApp pueden declararse habilitados solo después de configurar proveedor y credenciales; la interfaz no afirma entregas que todavía no existen.

# Recuperación de acceso, catálogos y pago seguro (actualizado 2026-10-06)

- **Recuperación:** la respuesta es idéntica exista o no el correo. El enlace expira en 30 minutos, es de un solo uso y transporta el token en el fragmento de URL para evitar registros del servidor. Al cambiar la contraseña se invalidan las sesiones anteriores.
- **Contraseñas:** nunca se envía, devuelve, audita ni registra una contraseña. La nueva clave exige al menos 10 caracteres, mayúscula, minúscula, número y símbolo; se permite pegar y usar gestores de contraseñas.
- **Selectores operativos:** clientes, suministros, medidores, facturas pendientes, pagos, órdenes, técnicos, cuadrillas y administradores se eligen desde catálogos autorizados. Las facturas, pagos y órdenes se filtran por el suministro seleccionado. El valor enviado sigue siendo el identificador esperado por la API; no se permite texto arbitrario para relaciones entre entidades.
- **Cobro:** SGSE no captura ni almacena PAN, vencimiento ni CVV. El portal prepara factura y método, y debe transferir el control a una pasarela alojada cuando exista proveedor autorizado. Mientras no exista, muestra un error explícito y no simula una transacción real.
- **Marcas bancarias:** la interfaz usa nombres y símbolos genéricos; incorporar logotipos requiere activos y autorización de uso.

# Alta e invitación de cuentas (actualizado 2026-10-07)

- **Creación administrada:** el administrador registra la identidad, el rol, el vínculo de cliente y decide explícitamente si la cuenta queda activa.
- **Entrega segura:** una cuenta activa puede recibir un enlace de acceso durante el alta o mediante “Enviar acceso”. El enlace dura 30 minutos, es de un solo uso y permite que la persona cree su propia contraseña.
- **Sin contraseñas por correo:** el sistema nunca muestra ni envía una contraseña escrita al usuario o al administrador. La cuenta se crea con un secreto aleatorio desconocido hasta que el destinatario establece su clave.
- **Cuenta inactiva:** no recibe invitaciones ni puede iniciar sesión. La interfaz explica que debe reactivarse antes de enviar el acceso y la API vuelve a validar esta regla.
- **Fallo de correo:** crear la cuenta no se revierte si SMTP no está disponible. La respuesta lo declara sin afirmar una entrega inexistente y permite reintentar desde la lista.
- **Trazabilidad:** cada solicitud de invitación invalida enlaces anteriores sin usar y registra un evento de auditoría sin guardar el token ni la contraseña.
