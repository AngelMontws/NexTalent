# WhatsApp Cloud API: investigación (tarea 0.1)

| | |
|---|---|
| **Responsable** | Backend 2 (Postulaciones y Notificaciones) |
| **Fase** | 0 - Investigación |
| **Consulta de fuentes** | 24 de noviembre de 2026 |
| **Historias** | HU-07.01 a HU-07.04 |
| **Estado** | Parte técnica lista para revisión. Faltan los textos de P-01 y P-02 y el flujo F-03 |

**Cómo leer este documento**

- **Verificado**: aparece en la fuente citada entre corchetes.
- **Por comprobar**: no se encontró en las fuentes oficiales leídas; se prueba con el número de prueba.
- **Propuesta**: decisión sugerida por Backend 2, aún sin acordar.

## 1. Conclusiones

1. El prototipo parece viable con el número de prueba y un túnel HTTPS, con el simulador como plan B. Todavía no se ha probado.
2. **Los costos del documento v5 (sección 3.5) están desactualizados.** Desde el 1 de octubre de 2026, las plantillas de utilidad también se cobran dentro de la ventana de 24 h.
3. **P-01 probablemente sería marketing, no utilidad.**
4. El código de 6 dígitos exige una plantilla de **autenticación**, que el v5 no inventaría.
5. **El teléfono no basta como identificador**: Meta puede omitirlo en los webhooks.
6. La guía de ngrok para WhatsApp pide un plan de pago. Propuesta: probar primero Dev Tunnels.

## 2. Cuenta, número de prueba y token

**Verificado** [1]

- Se necesita una cuenta de Facebook o una cuenta administrada de Meta, registro como desarrollador y un dispositivo con WhatsApp.
- Se crea una app con el caso de uso "Connect with customers through WhatsApp".
- Meta genera y registra un número de prueba automáticamente [2].
- El token temporal caduca pronto y no es adecuado para desarrollar. Los tokens de usuario se renuevan cada pocas horas [3].
- Para algo estable se usa un token de System User con `business_management`, `whatsapp_business_messaging` y `whatsapp_business_management`.

**Por comprobar**

- Máximo de 5 destinatarios de prueba: lo indican terceros [20]. Una página oficial sí señala que escribir a un número fuera de la lista devuelve el error 131030 [4].
- Si los mensajes del número de prueba son gratuitos y qué plantillas propias admite. La guía usa `hello_world`.
- Duración exacta del token temporal (terceros indican 24 h).

**Pasos para probar**

1. Crear la app en Meta for Developers y agregar WhatsApp.
2. Registrar como destinatarios los teléfonos del equipo, con permiso de cada persona.
3. Enviar `hello_world`.
4. Crear un System User y su token.
5. Guardar solo en `.env`: token, identificador del número, App Secret y token de verificación.

## 3. Webhook

### 3.1 Verificación (GET)

**Verificado** [5]: Meta envía `hub.mode=subscribe`, `hub.challenge` y `hub.verify_token`. El endpoint compara el token y devuelve el valor de `hub.challenge`.

### 3.2 Firma (POST)

**Verificado** [5]: `X-Hub-Signature-256` lleva el prefijo `sha256=` y se calcula con el contenido y el App Secret.

**Por comprobar**: la página leída dice "SHA256" sin nombrar HMAC. Se espera HMAC-SHA256 sobre el cuerpo **sin modificar**; se confirma con un webhook real.

Propuesta: leer los bytes antes de deserializar, comparar en tiempo constante y rechazar firmas inválidas.

### 3.3 Entrega

**Verificado** [6]

- El certificado debe ser válido; no se aceptan autofirmados [5].
- Se suscribe el campo `messages`.
- Si la respuesta no es 200, Meta reintenta hasta 7 días y puede haber duplicados.
- El contenido puede medir hasta 3 MB.
- Algunos webhooks no se envían si la app está en modo Dev; Meta pide modo Live.

**No encontrado**: tiempo máximo de respuesta y si hace falta verificar la empresa para recibir webhooks.

**Propuesta de idempotencia**: guardar el `id` del mensaje y descartar repetidos. La restricción UNIQUE protege el alta de la postulación, pero no evita responder dos veces.

### 3.4 Ruta en NexTalent

El gateway ya envía `/whatsapp/**` a Notificaciones sin cambiar la ruta. La URL pública sería `https://<tunel>/whatsapp/webhook`. Propuesta: el webhook no lleva JWT y se protege con la firma; `/whatsapp/vincular` y `/whatsapp/verificar` sí requieren JWT.

### 3.5 HTTPS en desarrollo

| | Dev Tunnels | ngrok gratuito |
|---|---|---|
| Instalación | `winget install Microsoft.devtunnel` [7] | No instalado |
| URL estable | Sí, al reutilizar el túnel [8] | Sí, un dominio de desarrollo [10] |
| Límites | 5 GB por usuario y 1,500 peticiones/min por puerto [9] | 1 GB y 20,000 peticiones HTTP al mes [10] |
| Página intermedia | No mencionada en las páginas leídas | No afecta a webhooks [10] |
| Compatibilidad con Meta | Por comprobar | Su guía pide el plan Pay-as-you-go [11] |

Propuesta: Dev Tunnels. Está en versión preliminar y sin SLA, y expira tras 30 días sin actividad [8]. Los límites de [9] se leyeron en resumen, no en texto literal.

```powershell
devtunnel user login
devtunnel create nextalent
devtunnel port create nextalent -p 5000 --protocol http
devtunnel access create nextalent --anonymous
devtunnel host nextalent
```

Estos comandos aún no se han ejecutado. `--anonymous` expone el gateway completo a quien conozca la URL: encenderlo solo durante las pruebas.

## 4. Plantillas

### 4.1 Registro y límites

**Verificado** [12] [13]

- Se crean en WhatsApp Manager o por API; la revisión puede tardar hasta 24 horas.
- Solo se envían las que están en estado `APPROVED`.
- Cuerpo: 1,024 caracteres. Pie: 60. Cada variable requiere un ejemplo.
- Respuesta rápida: hasta 10 botones de 25 caracteres. Con 4 o más botones, la plantilla no se ve en WhatsApp Desktop.

### 4.2 Categorías

**Verificado** [14]

| Categoría | Regla |
|---|---|
| Utilidad | No promocional y, además, específica o solicitada por la persona, o esencial para ella |
| Marketing | Promociones, contenido mixto o poco claro; el retargeting cuenta aunque la persona lo haya pedido |
| Autenticación | Códigos de un solo uso |

Meta puede recategorizar una plantilla aprobada. La página no menciona alertas de empleo. Clasificar P-01 como marketing es una **inferencia**: un aviso de coincidencia con una búsqueda guardada aparece como marketing.

| Mensaje | Categoría esperada | Observación |
|---|---|---|
| P-01 Alerta | Marketing (inferencia) | Meta puede limitar marketing por persona: error 131049, esperar al menos 24 h [15] |
| P-02 Estatus | Utilidad | Debe tratar solo de la postulación |
| Código de 6 dígitos | Autenticación | Texto fijo de Meta; expiración de 1 a 90 min, admite los 10 de HU-07.01 [16] |
| Confirmación con folio | Mensaje de servicio o plantilla | El toque abre la ventana, así que no exige plantilla; D-03 la llama "plantilla de confirmación" |

### 4.3 Botón "Postularme" en tres vacantes

**Por comprobar**: si dos botones de una plantilla pueden repetir texto, cómo se define su `payload` al enviar y su longitud máxima. En los botones interactivos, que son otro tipo de mensaje, los títulos deben ser únicos [17].

| Opción | Ventaja | Desventaja |
|---|---|---|
| A. Un mensaje con tres botones numerados | Cumple "un mensaje con máximo 3 vacantes" | El texto ya no es exactamente "Postularme" |
| B. Un mensaje por vacante | Botón idéntico y sin ambigüedad | Hasta tres mensajes y tres cobros; contradice "un mensaje" |

Propuesta: A. Para no depender del `payload`, guardar al enviar el id del mensaje y la vacante de cada botón. Al recibir, `context.id` identifica la alerta y el texto del botón, la vacante.

## 5. Mensajes de entrada

Llegan en `entry[].changes[].value.messages[]`. Extractos según [18] y [19]:

**Botón de una plantilla (P-01)**

```json
{
  "context": { "from": "<numero_empresa>", "id": "<id_de_la_alerta>" },
  "from": "<telefono>",
  "from_user_id": "<bsuid>",
  "id": "<id_del_mensaje>",
  "timestamp": "<fecha>",
  "type": "button",
  "button": { "payload": "<texto_del_boton>", "text": "<texto_del_boton>" }
}
```

**Texto, por ejemplo "BAJA"**

```json
{
  "from": "<telefono>",
  "from_user_id": "<bsuid>",
  "id": "<id_del_mensaje>",
  "timestamp": "<fecha>",
  "type": "text",
  "text": { "body": "BAJA" }
}
```

- Los botones de mensajes interactivos llegan distinto: `interactive.button_reply.id`.
- **BAJA** (propuesta): quitar espacios, ignorar mayúsculas, guardar la baja antes de responder y confirmar dentro de la ventana que abre ese mensaje. Meta pide dar instrucciones de baja y respetarlas [21].

### 5.1 Identificar a la persona

**Verificado** [18]

- `from_user_id` (BSUID) siempre viene; `from` puede faltar si la persona usa nombre de usuario y su teléfono no está disponible.
- Las plantillas de autenticación no se pueden enviar a un BSUID: error 131062 [22].
- En México, la API puede modificar el prefijo del teléfono [22].

Propuesta: guardar el teléfono y, cuando se conozca, el BSUID. No comparar teléfonos sin normalizar; el formato real se confirma con un webhook. La página de BSUID tiene un cambio del 5 de noviembre de 2026 que no se alcanzó a leer.

## 6. Costos y ventana de 24 horas

**Verificado** [23]

- Se cobra por mensaje entregado, según categoría y país. México es un mercado propio.
- Fuera de la ventana de 24 h solo se envían plantillas. La ventana se abre cuando la persona responde.
- Desde el 1 de octubre de 2026 se cobran las plantillas de utilidad dentro de la ventana.
- Desde esa fecha, los mensajes de servicio tienen 1,000 gratuitos por número y mes. Sin método de pago dejan de entregarse al agotarlos.

El v5 dice que la utilidad es gratuita dentro de la ventana: ya no es así. No se verificaron las tarifas actuales de México; las cifras del v5 no deben tomarse como vigentes.

Un portafolio nuevo puede escribir a 250 personas únicas cada 24 h fuera de la ventana [24].

## 7. Riesgos y plan B

| Riesgo | Respuesta propuesta |
|---|---|
| Meta tarda o rechaza plantillas | Simulador |
| P-01 se clasifica como marketing | Aceptar la categoría y, ante el error 131049, no reintentar de inmediato |
| El token temporal vence en la demo | Token de System User |
| Dev Tunnels no pasa la verificación | ngrok de pago o servidor con certificado |
| Webhooks duplicados | Registrar el id del mensaje |
| Teléfono ausente | Usar BSUID |
| Número de prueba frente a la "cuenta verificada" del v5 | El equipo declara la excepción del prototipo |

### 7.1 Simulador (solo diseño)

1. Construye el JSON de la sección 5 y lo firma con un secreto de desarrollo.
2. Lo envía a `POST /whatsapp/webhook`, para recorrer el mismo código que Meta.
3. El envío saliente usa una interfaz con dos implementaciones: Meta y simulador.
4. Solo existe en Development.

No demuestra la entrega real por WhatsApp: el v5 verifica RNF-03 con el número de prueba. Su apariencia queda pendiente de Frontend.

## 8. Pendiente: P-01, P-02 y F-03

El v5 indica que se detallan en el **Reporte de Diseño**, que todavía no existe. No se redactan textos hasta recibirlos. Requisitos ya conocidos:

| | Contenido exigido |
|---|---|
| P-01 | Máximo 3 vacantes; puesto, empresa verificada, rango salarial y botón Postularme; solo texto |
| P-02 | Nueva etapa; plantilla de utilidad |
| Ambas | Sin dinero, datos bancarios ni sensibles; sin CURP, CV ni dirección; folio y origen; tono claro, cercano, útil y respetuoso |

## 9. Decisiones por acordar

| # | Decisión | Con quién |
|---|---|---|
| 1 | Textos de P-01 y P-02, y flujo F-03 | Frontend/UX |
| 2 | Folio de P-01, si todavía no hay postulación | Equipo |
| 3 | Orden entre consentimiento y código (HU-07.01) | Equipo |
| 4 | Opción A o B del botón | Frontend/UX |
| 5 | Cómo conoce Notificaciones el opt-in, que vive en Identidad | Backend 1 |
| 6 | Cómo autentica Notificaciones `POST /postulaciones` sin el JWT del candidato | Backend 1 |
| 7 | Guardar BSUID, ids de mensajes y relación botón-vacante | Base de Datos |
| 8 | Corregir costos y declarar la plantilla de código en el v5 | Equipo |
| 9 | Persona dueña de la cuenta de Meta | Equipo |

## 10. Fuentes

Base de Meta: `https://developers.facebook.com/documentation/business-messaging/whatsapp/`

1. https://developers.facebook.com/docs/whatsapp/cloud-api/get-started
2. Base + `business-phone-numbers/phone-numbers`
3. Base + `access-tokens`
4. Base + `calling/sandbox` (vista en resultado de búsqueda)
5. https://developers.facebook.com/docs/graph-api/webhooks/getting-started
6. https://developers.facebook.com/docs/whatsapp/cloud-api/guides/set-up-webhooks
7. https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started
8. https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/faq y https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/cli-commands
9. https://learn.microsoft.com/en-us/azure/azure-resource-manager/management/azure-subscription-service-limits#dev-tunnels-limits
10. https://ngrok.com/docs/pricing-limits/free-plan-limits
11. https://ngrok.com/docs/integrations/webhooks/whatsapp-webhooks
12. https://developers.facebook.com/docs/whatsapp/business-management-api/message-templates
13. https://developers.facebook.com/docs/whatsapp/business-management-api/message-templates/components
14. https://developers.facebook.com/docs/whatsapp/updates-to-pricing/new-template-guidelines
15. Base + `templates/marketing-templates/per-user-limits`
16. https://developers.facebook.com/docs/whatsapp/business-management-api/authentication-templates
17. Base + `messages/interactive-reply-buttons-messages`
18. Base + `webhooks/reference/messages` y base + `business-scoped-user-ids` (lectura parcial)
19. Base + `webhooks/reference/messages/button` y base + `webhooks/reference/messages/interactive`
20. Terceros, sin confirmar (vistos en resultado de búsqueda): https://richautomate.in/blog/whatsapp-cloud-api-test-number-sandbox-india-2026 y https://ericanadia.github.io/technical-writing-portfolio/whatsapp-quickstart/
21. https://developers.facebook.com/docs/whatsapp/overview/getting-opt-in
22. Base + `messages/send-messages`
23. Base + `pricing` (lectura parcial)
24. https://developers.facebook.com/docs/whatsapp/messaging-limits
