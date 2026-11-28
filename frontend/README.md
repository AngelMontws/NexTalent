# NexTalent — Frontend con código en español y comentado

> **En el repositorio:** el código React está en esta carpeta. La demostración HTML independiente está en `prototipo/`, y las capturas, notas y resultados de pruebas en `docs/`.
>
> **Con el backend:** levanta `docker compose up -d` en la raíz y luego, aquí, `npm ci` y `npm run dev`. Vite reenvía `/api` y `/hubs` al gateway (`http://localhost:5000`).

## Qué está conectado

| Parte | Origen | Estado |
|---|---|---|
| Inicio de sesión | `POST /api/auth/login` (Identidad) | Conectado |
| Registro | `POST /api/auth/registro` (Identidad) | Pendiente de Backend 1 (muestra "todavía no disponible") |
| Postular, Mis postulaciones, historial | `/api/postulaciones` | Conectado |
| Tablero del reclutador, abrir perfil, mover etapa | `/api/postulaciones` | Conectado |
| Avisos sin recargar | SignalR `/hubs/estatus` | Conectado |
| Bandeja de notificaciones del reclutador (NT-041) | `/api/notificaciones` | Conectado: se guarda en el servidor y se marca leída una por una |
| Buscar y publicar vacantes | `/api/vacantes` (Vacantes) | Pendiente de Backend 1: usa `src/api/catalogoTemporal.ts` |
| Perfil y "Mi cuenta" | Identidad | Pendiente de Backend 1: se guarda en este navegador |

**El rol sale del token:** ya no hay "Entrar como…", "Cambiar de rol" ni "Restaurar datos".

### Cuentas de prueba

El inicio de sesión es el real de Identidad (`POST /api/auth/login`). Las cuentas las siembra la base de datos en desarrollo:

| Correo | Rol |
|---|---|
| `candidato1@nextalent.mx` … `candidato4@nextalent.mx` | Candidato |
| `reclutador1@empresa1.mx` | Reclutador (Empresa de prueba 1) |
| `reclutador2@empresa2.mx` | Reclutador (Empresa de prueba 2) |

Contraseña de todas: `NexTalent2026!` (se cambia con `SEED_DEFAULT_PASSWORD`). La sesión dura `JWT_EXPIRES_MINUTES` (480 por defecto).

Proyecto de demostración basado en el frontend anterior de NexTalent. Conserva el logotipo del maletín con engranaje, la paleta dorada, el inicio de sesión, el registro, las pantallas del candidato, el panel del reclutador y la interfaz responsiva.

## Archivos incluidos

- **`prototipo/vista-previa-funcional.html`**: demostración independiente; ábrela con doble clic en el navegador. Contiene HTML, CSS y JavaScript, con clases CSS, atributos `data-*`, funciones principales y estado propios nombrados en español. Hay comentarios antes de cada función y de los estilos principales.
- **`src/Aplicacion.tsx`**: inicia las pantallas, la navegación, el modo oscuro y el cierre de sesión.
- **`src/principal.tsx`**: crea la aplicación React y la monta en el HTML.
- **`src/tipos.ts`**: define cómo son vacantes, candidatos, postulaciones, documentos y notificaciones.
- **`src/datos.ts`**: contiene datos ficticios, formatos de fecha y salario, y cálculo de coincidencia.
- **`src/estilos.css`**: carga Tailwind CSS y define clases propias como `.boton-principal`, `.campo`, `.panel` y `.insignia`.
- **`src/contexto/ContextoAplicacion.tsx`**: conserva el estado compartido; permite postular, publicar, editar perfiles y cambiar etapas.
- **`src/componentes/PantallaAutenticacion.tsx`**: ingreso, registro, CAPTCHA de demostración y recuperación simulada.
- **`src/componentes/CargaDocumentos.tsx`**: selección de CV, tres cartas de recomendación, título y cédula.
- **`src/componentes/ComponentesUI.tsx`**: tarjetas de vacantes, estados, avatar e indicadores reutilizables.
- **`src/componentes/VentanaModal.tsx`**: ventanas emergentes para confirmar acciones.
- **`src/paginas/BusquedaCandidato.tsx`**: filtros, resultados y detalle de vacantes.
- **`src/paginas/PostulacionesCandidato.tsx`**: seguimiento y etapas de las postulaciones.
- **`src/paginas/ConfiguracionPerfil.tsx`**: edición de foto, datos de contacto y preferencias.
- **`src/paginas/PublicacionVacante.tsx`**: formulario de vacantes y validación de salario.
- **`src/paginas/TableroReclutador.tsx`**: tarjetas Kanban y movimiento de candidatos.
- **`src/paginas/NotificacionesReclutador.tsx`**: bandeja de avisos y preparación de mensajes de WhatsApp.

Las funciones, los componentes, los tipos y las propiedades **creadas para el proyecto** usan nombres en español siempre que pueden modificarse de forma segura. Cada archivo incluye comentarios cortos que explican qué representa y qué hacen sus funciones, efectos, estados y componentes.

## Nombres que deben permanecer en inglés

No se modifican las instrucciones del lenguaje ni las interfaces de bibliotecas, porque dejarían de funcionar. Ejemplos:

| Nombre técnico | Por qué debe mantenerse |
| --- | --- |
| `className`, `onClick`, `onChange`, `key`, `children` | Atributos estándar de React/JSX |
| `class`, `id`, `type`, `name`, `placeholder`, `aria-label`, `role` | Atributos estándar de HTML |
| `useState`, `useEffect`, `useMemo`, `useCallback` | Funciones oficiales de React |
| `getElementById`, `addEventListener`, `localStorage`, `File.name` | APIs del navegador |
| `display`, `background`, `margin`, `padding` | Propiedades oficiales de CSS |
| `flex`, `grid`, `bg-*`, `text-*`, `dark:*`, `sm:*`, `lg:*` | Utilidades y variantes de Tailwind CSS |
| `scripts`, `dependencies`, `devDependencies`, `type` | Claves reservadas de `package.json` |

En la **vista HTML**, los atributos propios de NexTalent sí se tradujeron: `data-accion`, `data-acceso`, `data-postulacion`, `data-identificador`, `data-filtro` y otros. Se mantienen ciertos identificadores y claves de compatibilidad interna (`id`, `min`, `max`, nombres nativos del archivo), ya que también se relacionan con APIs o formularios de prueba.

## Ejemplos de código explicado

```tsx
// Guarda el dato y vuelve a dibujar el componente cuando cambia.
const [busqueda, establecerBusqueda] = useState('');

// Busca las vacantes que siguen publicadas y no están vencidas.
const disponibles = vacantes.filter(vacante => vacante.activa);
```

```css
/* Botón principal: resalta la acción que debe realizar el usuario. */
.boton-principal {
  /* Tailwind aplica aquí los estilos responsivos. */
  @apply rounded-xl px-4 py-2.5;
}
```

## Cómo probarlo

**Opción 1: sin instalación.** Abre `prototipo/vista-previa-funcional.html` con Chrome, Edge o Firefox. Entra con los botones «Entrar como candidato» o «Entrar como reclutador».

**Opción 2: ejecutar React.** Necesitas Node.js 20 o superior:

```bash
npm install
npm run dev
```

Abre la dirección que te muestre Vite, normalmente `http://localhost:5173`.

Para crear la versión de producción:

```bash
npm run build
```

## Alcance de la demostración

- **Sin backend:** el acceso, el CAPTCHA y la recuperación son simulaciones; no verifican contraseñas reales.
- **WhatsApp:** los avisos aparecen en pantalla y permiten preparar un mensaje; **no se envían automáticamente**.
- **Documentos:** se validan y seleccionan archivos, pero no se suben a un servidor. No ingreses documentos ni datos personales reales.
- **Almacenamiento local:** las vacantes y los estados ficticios se pueden guardar en el navegador. Las contraseñas, la CURP y los archivos reales no se almacenan como documentos persistentes.
- **Adaptabilidad:** móvil (menor de 768 px), tableta (768–1023 px) y escritorio (1024 px en adelante).

## Verificaciones de esta entrega

- La vista HTML independiente se probó a **360, 390, 768, 1024, 1280 y 1600 px**, incluidos registro, búsqueda, carga de CV, postulación, cambio de rol, perfil y Postulaciones; no se detectaron errores de JavaScript ni desbordamiento horizontal en los recorridos comprobados.
- Se verificó la sintaxis de los **15 archivos React/TypeScript** y sus importaciones locales.
- No fue posible instalar las dependencias de npm en este entorno para ejecutar una compilación de producción de React. La prueba funcional completa corresponde a la vista HTML independiente.


## Correcciones responsivas — versión actual

Se ajustaron los espacios en el encabezado, los formularios, las tarjetas de vacantes, los paneles y la barra inferior de navegación. El diseño contempla **móvil (360–767 px)**, **tablet (768–1023 px)** y **PC (1024 px o más)**.

- **Vista inmediata:** abre `prototipo/vista-previa-funcional.html` en tu navegador (sin npm).
- **React:** ejecuta `npm install` y después `npm run dev` para iniciar la aplicación.
- **Capturas ilustrativas:** `docs/vistas_previas/NexTalent-vista-PC-corregida.png` (1440×900) y `docs/vistas_previas/NexTalent-vista-telefono-corregida.png` (390×844). Son representaciones del diseño final, no capturas de una sesión real del navegador.
- Los cambios principales están comentados en `src/estilos.css` y en `docs/AJUSTES_RESPONSIVOS.md`.

No se agregaron servicios de backend ni notificaciones automáticas de WhatsApp.


## Ajustes de interfaz
- Se retiraron las etiquetas de «demo» de las pantallas.
- Los formularios muestran **Teléfono celular**.
- El icono de perfil del encabezado abre **Mi cuenta** para ambos roles, también en móviles.

**Aviso técnico:** Es un frontend sin autenticación real ni mensajes automáticos de WhatsApp. La interfaz utiliza estado local; es necesaria una API/backend para cuentas reales.


## Ajustes solicitados
- Cualquier candidato puede postularse a una vacante activa, aunque no cumpla todas las habilidades. Se mantiene una postulación por candidato y vacante.
- Se eliminó el indicador/barra de coincidencia de la búsqueda del candidato.
- Se pueden marcar varias habilidades en los filtros; deben estar todas presentes en la vacante.
- El reclutador puede añadir la URL HTTPS de su empresa en «Mi cuenta»; el nombre de la empresa se convierte en un enlace en el buscador y el detalle.
- Los registros históricos de ejemplo ya no se atribuyen al usuario actual.
- Las funciones de inicio de sesión y envío de documentos siguen siendo una simulación local sin backend.


## Auditoría final de correcciones (noviembre de 2026)

Se corrigieron en la versión HTML y/o React, según correspondía:

1. **Publicación de vacantes:** los campos del formulario y la lógica comparten las mismas claves.
2. **Pestaña «En proceso»:** variable corregida, sin error de JavaScript.
3. **Separación de postulaciones:** las candidaturas se filtran por el identificador de cuenta basado en el correo en la interfaz local. Los datos personales sensibles no se guardan como archivos locales.
4. **Empresa del reclutador:** las vacantes usan un identificador de empresa estable, incluso si el nombre cambia. El sitio web se actualiza en sus vacantes.
5. **WhatsApp:** se conserva el consentimiento seleccionado al crear una cuenta en la vista HTML. Las notificaciones automáticas reales continúan pendientes del backend.
6. **Contraste:** el texto de los controles dorados ahora es oscuro; en el botón principal la relación es aproximadamente 5.65:1.
7. **Consistencia:** «Ir al Kanban» se sustituyó por «Ir a Postulaciones»; se redujeron las clases azules residuales de la interfaz React.

**Pruebas ejecutadas:** 13 de 13 comprobaciones funcionales de la vista HTML terminadas correctamente con Chromium (registro, publicación, postulación, consentimiento, enlace de empresa, aislamiento por correo, contraste, pestañas y seis anchos de pantalla). Se verificó además la sintaxis y las referencias locales de 15 archivos TS/TSX. **No se ha completado una compilación React con npm**, porque faltan dependencias en este entorno.

**Límite importante:** el registro y el inicio de sesión siguen siendo una simulación del frontend. La separación local de cuentas no es seguridad real. Para impedir accesos indebidos y enviar mensajes de WhatsApp se requiere autenticación y autorización en el backend. No utilizar este prototipo para almacenar documentos o información personal reales.
