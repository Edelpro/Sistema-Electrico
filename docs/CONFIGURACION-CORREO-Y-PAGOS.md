# Configuración de correo y pagos

## Recuperación de contraseña por Gmail/SMTP

El sistema nunca envía la contraseña actual. Genera un enlace de un solo uso, válido durante 30 minutos, guarda únicamente el hash del token y cierra las sesiones anteriores cuando el cambio termina.

Para desarrollo, configura los secretos fuera de `appsettings.json`:

```powershell
dotnet user-secrets init
dotnet user-secrets set "Smtp:Host" "smtp.gmail.com"
dotnet user-secrets set "Smtp:Port" "587"
dotnet user-secrets set "Smtp:EnableSsl" "true"
dotnet user-secrets set "Smtp:Username" "correo-remitente@gmail.com"
dotnet user-secrets set "Smtp:Password" "CLAVE-DE-APLICACION"
dotnet user-secrets set "Smtp:FromAddress" "correo-remitente@gmail.com"
dotnet user-secrets set "Smtp:FromName" "SGSE Servicio eléctrico"
```

Usa una contraseña de aplicación de Google; no escribas la contraseña normal de Gmail en el proyecto ni la envíes por chat. En producción, usa variables de entorno o el almacén de secretos del servidor.

Si SMTP no está configurado, la API devuelve el mismo mensaje genérico y registra solo que el envío fue omitido. Nunca registra el token ni confirma si el correo existe.

## Cobro con tarjeta

La interfaz permite preparar una factura y elegir tarjeta o transferencia, pero bloquea el cobro real hasta conectar una pasarela autorizada para Ecuador. SGSE no debe recibir, guardar ni registrar PAN, fecha de vencimiento o CVV.

La integración definitiva debe usar el checkout alojado o el componente seguro del proveedor, crear la sesión en el servidor y confirmar el pago únicamente mediante un webhook firmado e idempotente. Después de confirmar el proveedor, se conecta su SDK/API en el backend y se reemplaza el mensaje `pasarela no conectada` por la redirección segura.

Los nombres de bancos se presentan como texto para orientar la selección; no se copiaron logotipos ni marcas gráficas sin activos autorizados.
