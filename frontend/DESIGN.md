---
version: alpha
name: 'SGSE'
description: 'Interfaz operativa, clara y confiable para gestionar el servicio eléctrico en Ecuador.'
colors:
  primary: '#075DD8'
  primary-strong: '#064EBA'
  accent-cyan: '#07766B'
  accent-soft: '#DDFBF6'
  attention: '#C2410C'
  attention-soft: '#FFF0E5'
  success: '#0B6B45'
  success-soft: '#E6F7EF'
  danger: '#C83D4D'
  canvas: '#FFFFFF'
  surface: '#F6F9FD'
  surface-strong: '#EDF4FC'
  border: '#D8E3F0'
  text: '#0B1F44'
  text-muted: '#60708F'
typography:
  sans:
    fontFamily: 'Manrope, Segoe UI, system-ui, sans-serif'
    fontSize: '1rem'
    lineHeight: '1.55'
  display:
    fontFamily: 'Manrope, Segoe UI, system-ui, sans-serif'
    fontSize: '2rem'
    lineHeight: '1.15'
  mono:
    fontFamily: 'Cascadia Code, Consolas, monospace'
rounded:
  DEFAULT: '0.75rem'
  sm: '0.5rem'
  md: '0.75rem'
  lg: '1rem'
  full: '999px'
spacing:
  control: '2.875rem'
  panel: '1.5rem'
  section-gap: '1.5rem'
  page-gutter: '2rem'
  page-max: '90rem'
components:
  button:
    backgroundColor: '{colors.primary}'
    textColor: '#FFFFFF'
    rounded: '{rounded.md}'
    height: '{spacing.control}'
  button-hover:
    backgroundColor: '{colors.primary-strong}'
    textColor: '#FFFFFF'
    rounded: '{rounded.md}'
  input:
    backgroundColor: '{colors.canvas}'
    textColor: '{colors.text}'
    rounded: '{rounded.md}'
    height: '3.25rem'
  panel:
    backgroundColor: '{colors.canvas}'
    textColor: '{colors.text}'
    rounded: '{rounded.lg}'
    padding: '{spacing.panel}'
  sidebar:
    backgroundColor: '{colors.canvas}'
    textColor: '{colors.text-muted}'
    width: '14rem'
  icon:
    textColor: '{colors.primary}'
    size: '1.5rem'
  accent-icon:
    backgroundColor: '{colors.accent-soft}'
    textColor: '{colors.accent-cyan}'
    rounded: '{rounded.md}'
  attention-icon:
    backgroundColor: '{colors.attention-soft}'
    textColor: '{colors.attention}'
    rounded: '{rounded.md}'
  success-status:
    backgroundColor: '{colors.success-soft}'
    textColor: '{colors.success}'
    rounded: '{rounded.full}'
  danger-alert:
    backgroundColor: '{colors.danger}'
    textColor: '{colors.canvas}'
    rounded: '{rounded.md}'
  activity-header:
    backgroundColor: '{colors.surface-strong}'
    textColor: '{colors.text}'
    rounded: '{rounded.md}'
  bordered-surface:
    backgroundColor: '{colors.surface}'
    textColor: '{colors.text-muted}'
    rounded: '{rounded.md}'
  separator:
    backgroundColor: '{colors.border}'
    textColor: '{colors.text}'
    height: '1px'
---

# SGSE Design System

## Overview

### Creative North Star

Una consola de despacho eléctrico dibujada con la precisión de un plano técnico: fondo blanco verdadero, líneas finas, nodos de energía y señales de prioridad muy contenidas. La composición toma de la referencia entregada su navegación lateral tranquila, búsqueda horizontal y jerarquía abierta, pero reemplaza el lenguaje de galería NFT por un producto operativo para servicios básicos.

### Product context and register

- **Audience and primary job:** administradores, técnicos y clientes que consultan pagos, órdenes, reconexiones y operación del servicio.
- **Target market(s) and evidence:** Ecuador; se deriva del SRS del proyecto y del dominio de la API.
- **Locale(s) and language policy:** español de Ecuador (`es-EC`), con vocabulario directo y comprensible; no se mezclan etiquetas en inglés.
- **Usage scene:** escritorio administrativo como entorno principal y móvil como acceso complementario; las tareas pueden ser urgentes y deben leerse con rapidez.
- **Register:** producto. La familiaridad, la claridad de estado y la accesibilidad tienen prioridad sobre la expresión decorativa.
- **Memorable signature:** una línea de flujo eléctrico con nodos circulares comunica continuidad entre servicio, pago, orden y reconexión.
- **Restraint:** formularios, navegación y estados vacíos se mantienen sobrios; el naranja solo marca atención o prioridad.
- **Anti-references:** no debe parecer una landing de marketing, un tablero criptográfico, una interfaz de videojuegos ni una colección de tarjetas genéricas.
- **Token ownership/runtime mapping:** el archivo `src/styles.css` es la fuente canónica en ejecución. Este documento refleja sus variables semánticas; los componentes consumen variables y no colores literales.

## Colors

`primary` identifica navegación, foco y acciones seguras. `accent-cyan` distingue capacidad/disponibilidad, `attention` señala prioridad y `success` comunica estados confirmados. `canvas` permanece blanco verdadero en tema claro; `surface` y `surface-strong` crean separación sin teñir toda la aplicación. El tema oscuro remapea roles semánticos, no duplica estilos de componente. Los estados nunca dependen solo del color.

## Typography

Manrope es la primera opción por su lectura amable en paneles y formularios, con Segoe UI y fuentes del sistema como alternativas estables. Los títulos usan peso 700–800 y espaciado ligeramente negativo; cuerpo y controles se mantienen entre 14 y 16 px. No se usan mayúsculas espaciadas como subtítulos decorativos. Identificadores futuros pueden usar la familia monoespaciada.

## Layout

El escritorio usa una barra lateral persistente de 224 px, encabezado de 72 px y un área principal fluida con máximo de 1440 px. El panel operativo principal domina sobre el estado secundario; las listas conservan estructura abierta. A menos de 900 px la navegación pasa a un cajón modal desde la izquierda. A menos de 680 px los paneles se apilan y el formulario de acceso pasa a una columna. El contenido reserva espacio para errores, indicadores y controles para evitar saltos.

## Elevation & Depth

La jerarquía se crea con bordes azul grisáceo, cambios tonales y una sombra muy leve solo en superficies elevadas o activas. No se usan glassmorphism, halos ni sombras profundas. Los encabezados y barras fijas conservan fondo opaco para evitar solapamientos.

## Shapes

Los controles comparten radio de 12 px y los paneles 16 px. El símbolo de marca vive en un cuadrado de 12 px de radio; los avatares y nodos de la línea eléctrica son circulares. Los iconos son de contorno, `currentColor`, 24×24 y trazo 1.8; no se mezclan emojis ni glifos de otra familia.

## Components

### Foundational visual states

Todos los controles tienen estado por defecto, hover, `focus-visible`, activo, deshabilitado y ocupado cuando aplica. El foco usa un anillo azul de 3 px con separación visible. Los estados de error incluyen texto y asociación ARIA. El indicador de carga mantiene la geometría del botón; no se usan esqueletos por defecto.

### Buttons and actions

La acción primaria es azul sólido y se limita a una por zona de decisión. Las acciones secundarias usan borde o estilo transparente. El naranja no sustituye al rojo de error. Los botones de icono tienen nombre accesible y un área mínima de 44 px cuando son importantes.

### Navigation and data display

Inicio usa el estado seleccionado con fondo azul suave y una barra lateral de 3 px. Los módulos aún no implementados se presentan como texto atenuado y se anuncian como pendientes, sin falsa interactividad. Las métricas sin fuente de datos muestran un guion y “Sin datos conectados”; nunca se presenta cero como dato medido si no hubo consulta.

### Forms and overlays

Los campos miden 52 px, tienen etiqueta visible, ayuda/error asociada y validación controlada por Angular con `novalidate`. La contraseña está enmascarada y ofrece mostrar/ocultar mediante un botón real. El cajón móvil usa fondo inerte, cierre con Escape, contención y restauración del foco.

### Iconography

La familia local `app-icon` contiene SVG de contorno coherentes. Los iconos decorativos quedan ocultos de tecnologías de asistencia; los botones de icono reciben su nombre en el control padre.

### Motion

Las transiciones de control duran 160–220 ms. El cajón usa 240 ms y el contenido puede aparecer con una única entrada suave. Con `prefers-reduced-motion: reduce` se eliminan traslaciones y animaciones continuas no esenciales.

### Content and data visualization

La voz es directa: nombra la acción, el estado y la siguiente opción. Los estados vacíos explican qué aparecerá y por qué. Las gráficas sin una fuente real solo se permiten como prototipos claramente rotulados como “Datos demostrativos”, nunca como información de una cuenta; toda visualización incluye valores textuales y explica qué endpoint reemplazará la muestra.

## Do's and Don'ts

- **Do:** mantener el fondo blanco, la línea eléctrica y el azul como señal de estructura y continuidad.
- **Do:** diferenciar “sin datos”, “sin resultados” y un valor real de cero.
- **Don't:** convertir cada bloque en una tarjeta flotante o añadir estadísticas decorativas.
- **Don't:** usar gradientes, emojis o controles habilitados que no ejecuten una acción real.
