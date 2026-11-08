# SGSE — Sistema de Gestión del Servicio Eléctrico

Prototipo funcional full stack para portafolio, orientado a la gestión de clientes, suministros, medición, facturación, pagos, reclamos y órdenes técnicas de una empresa eléctrica.

> **Alcance:** esta versión demuestra arquitectura, flujos operativos, seguridad por roles y experiencia de usuario. No es un sistema productivo ni procesa cobros bancarios reales.

## Tecnologías

- Angular 22 y TypeScript.
- ASP.NET Core 10, Entity Framework Core y autenticación JWT.
- SQL Server para persistencia local.
- Diseño responsive, tema claro/oscuro y estados accesibles de carga, error y datos vacíos.

## Estructura

- `frontend/`: aplicación Angular para administración, portal del cliente y portal técnico.
- `backend/`: API REST en ASP.NET Core con reglas de negocio, autorización y persistencia.
- `design/concepts/`: conceptos visuales utilizados como referencia.
- `design/qa/`: capturas de verificación en escritorio, móvil y tema oscuro.
- `docs/`: decisiones de seguridad y configuración pendiente de servicios externos.

## Funcionalidad incluida

- Inicio de sesión y navegación por roles `Administrador`, `Tecnico` y `Usuario`.
- Gestión de usuarios, clientes, suministros y medidores.
- Facturas, conciliación auditada de pagos, reclamos y órdenes de servicio.
- Portal del cliente con consumo, facturas, historial, seguimiento y preparación de pago.
- Portal técnico con órdenes asignadas, transiciones válidas y evidencias de campo.
- Flujo visual de pago con tarjeta o transferencia. La tarjeta es una representación de interfaz; el prototipo no captura ni almacena PAN, vencimiento o CVV.
- Validaciones, trazabilidad, diseño responsive y estados claros de interacción.

## Ejecutar el frontend

Desde la raíz del repositorio:

```powershell
cd frontend
npm ci
npm start
```

Abrir `http://localhost:4200`.

## Ejecutar la API

La configuración local espera SQL Server y la base `SistemaElectricoDb`. Revisa `backend/appsettings.json` antes de iniciar.

```powershell
cd backend
dotnet run --launch-profile https
```

La API utiliza `https://localhost:7112` en el perfil local incluido.

## Verificación

```powershell
cd frontend
npm test -- --watch=false
npm run build
```

```powershell
cd backend
dotnet build
```

## Pendiente para producción

- Configurar secretos mediante variables de entorno o un almacén seguro.
- Conectar una pasarela de pago autorizada y confirmar transacciones mediante webhooks firmados e idempotentes.
- Configurar correo transaccional y recuperación de contraseña.
- Definir infraestructura, copias de seguridad, observabilidad y pruebas de seguridad.

Consulta [docs/CONFIGURACION-CORREO-Y-PAGOS.md](docs/CONFIGURACION-CORREO-Y-PAGOS.md) para el alcance de correo y pagos.
