# Guía rápida para entender el código

El sistema se explica mediante comentarios cortos justo antes de la función, el bloque de estado o el estilo que controlan. Estos son los conceptos principales:

## Nombres de React

- **`ProveedorAplicacion`**: hace disponibles las vacantes y postulaciones para todas las pantallas.
- **`usarAplicacion`**: permite leer el estado compartido sin pasar cada dato de componente en componente.
- **`postular`**: registra una postulación ficticia y muestra un folio.
- **`publicar`**: valida que exista un rango salarial y crea una vacante de prueba.
- **`moverPostulaciones`**: modifica la etapa del Kanban y registra el cambio en el historial.
- **`guardarPerfil`**: aplica la edición del perfil del usuario durante la sesión.
- **`actualizarPreferenciasReclutador`**: guarda el número y consentimiento para WhatsApp de la demostración.

## Nombres de la vista HTML

- **`dibujarInterfaz`**: actualiza la pantalla cada vez que hay un cambio.
- **`mostrarAcceso`**: muestra inicio de sesión, crear cuenta o recuperar contraseña.
- **`manejarAccion`**: recibe clics en botones y ejecuta la acción correspondiente.
- **`dibujarBusqueda`**, **`dibujarPostulaciones`**, **`dibujarPublicacion`**, **`dibujarKanban`**: pintan las pantallas principales.
- **`dibujarPerfil`**: muestra campos para editar datos y fotografía.
- **`dibujarNotificaciones`**: muestra avisos del reclutador.
- **`alCambiarArchivo`**: valida archivos adjuntos; no los envía a un servidor.
- **`guardarDatos`**: conserva información de demostración en el navegador.

## Atributos propios en español

Los botones de la demo llevan un atributo **`data-accion`**, que identifica qué debe hacer el programa cuando se presionan:

```html
<!-- Abre la pantalla donde se ven las postulaciones. -->
<button data-accion="go-apps">Mis postulaciones</button>
```

En JavaScript se lee con la API nativa `dataset`:

```javascript
// Lee la acción elegida. El nombre propio 'accion' está en español.
const accion = boton.dataset.accion;
```

**`dataset`**, **`button`**, **`className`**, **`onClick`** y otras palabras similares no pueden traducirse, porque son parte de HTML, JavaScript o React. Su función se explica mediante comentarios en español.
