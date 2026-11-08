# Registro de fidelidad visual

Comparación realizada el 27 de septiembre de 2026 entre los conceptos aprobados y las capturas de la implementación a 1440×900, con verificación móvil adicional a 390×844.

| Punto comparado | Evidencia del concepto | Evidencia renderizada | Resultado / ajuste |
|---|---|---|---|
| Composición del Login | `concepts/login-concepto.png`: relato eléctrico a la izquierda y formulario integrado a la derecha | `qa/login-desktop.png` | Coincide la proporción, jerarquía, línea eléctrica, capacidades y ancho del formulario. La ilustración se simplificó para mantenerla editable en SVG. |
| Encabezado del Inicio | `concepts/inicio-concepto.png`: búsqueda larga, tema y perfil sobre fondo blanco | `qa/inicio-desktop.png` | Coincide. Se eliminó la campana porque no existe todavía un flujo real de notificaciones. |
| Navegación lateral | Referencia y concepto: barra lateral clara, Inicio seleccionado e iconos de contorno | `qa/inicio-desktop.png` y `qa/inicio-mobile-menu.png` | Coincide. Los módulos sin ruta se renderizan atenuados y no son falsos controles; su estado pendiente sigue disponible para lectores de pantalla. |
| Panel operacional principal | Concepto: Reconexiones prioritarias, línea con nodos, estado vacío y tres resúmenes | `qa/inicio-desktop.png` | Coincide estructura, firma visual y densidad. Las cifras 0 se sustituyeron por “— / Sin datos conectados” porque no existe un endpoint de métricas. |
| Panel secundario | Concepto: estado de red visualmente dominante | `qa/inicio-desktop.png` | Se cambió a “Estado de integración” para no afirmar telemetría en tiempo real inexistente. Conserva jerarquía, icono de torre y lista de estados. |
| Actividad reciente | Concepto: lista/tablero inferior con estado vacío | `qa/inicio-desktop.png` | Coincide encabezado, columnas y estado vacío; queda en flujo vertical y continúa bajo el primer viewport cuando el alto disponible es menor. |
| Tema oscuro | Concepto principal en tema claro con control de tema | `qa/inicio-dark.png` | El mismo sistema semántico se remapea a superficies navy sin cambiar jerarquía ni estados. |
| Navegación móvil | Continuación responsive solicitada | `qa/inicio-mobile-menu.png` | Cajón modal desde la izquierda, fondo bloqueado, foco inicial en Cerrar, Escape y restauración al disparador verificados. |

## Decisiones intencionales

- No se incorporaron imágenes raster al producto: los conceptos pedían una superficie implementable en código y la firma eléctrica se resolvió con SVG accesible/decorativo.
- No se muestran métricas ni un estado de red como hechos hasta que existan endpoints que los respalden.
- No se añadieron rutas ficticias para Usuarios, Clientes u otros módulos; la navegación visual deja explícito que están pendientes sin ofrecer una interacción falsa.
- El Login no incluye “Olvidé mi contraseña” porque la API no expone recuperación. “Contactar soporte” despliega orientación real dentro de la página.
