# Documento de Decisiones Técnicas y Arquitectónicas (DECISIONES.md)

## 1. Variante Personal y Datos del Estudiante
- **Nombre completo:** Fabián Sojo
- **Variante asignada:** **Variante 0** (Último dígito de carnet % 5 = 0)
- **Parámetros de la Variante 0:**
  - **Tamaño máximo de página:** 20 registros (por defecto 10).
  - **Formato del código del ticket:** `TK-AAAA-NNNN` (ejemplo: `TK-2026-0001`). Autogenerado por el sistema en la creación.
  - **Restricción de ruta personalizada:** Valida el patrón `^TK-\d{4}-\d{4}$`.
  - **Regla de negocio especial (responde 409 al violarse):** *Un ticket de prioridad Crítica no puede pasar a Resuelto sin tener AgenteId asignado.*

---

## 2. ClienteId Inexistente al Crear un Ticket: ¿400, 404 o 422?
**Decisión adoptada: `400 Bad Request` en formato RFC 9457 (`application/problem+json`).**

### Justificación frente a las alternativas:
- **Frente a `404 Not Found`:** Según la especificación RFC 9110 (sección 15.5.5), `404 Not Found` indica que el servidor no encontró una representación actual para el URI de destino (`Request-URI`). En una petición `POST /api/tickets`, el recurso de destino es la colección `/api/tickets`, la cual **sí existe**. Si el servidor retornara 404, el cliente o las herramientas de monitoreo deducirían que la ruta de la API está mal escrita o no existe en el servidor. Por lo tanto, 404 sería engañoso y semánticamente incorrecto para reportar una clave foránea inválida en el cuerpo de la petición.
- **Frente a `422 Unprocessable Content`:** El código 422 (RFC 9110 sección 15.5.21, originado en RFC 4918 WebDAV) indica que el servidor comprende el tipo de contenido y la sintaxis, pero no puede procesar las instrucciones contenidas. Si bien algunos diseñadores de APIs utilizan 422 para errores de lógica de negocio, en ASP.NET Core Web API y las convenciones REST tradicionales de la industria, las solicitudes cuya carga útil contiene referencias foráneas que violan la integridad referencial básica de entrada se consideran entradas inválidas del cliente (`Client Error`), correspondiendo a `400 Bad Request` acompañado de un objeto `ProblemDetails` que especifica claramente qué campo falló (`clienteId`).

---

## 3. Identificador "abc" en `/api/tickets/abc`: 404 por Restricción de Ruta vs 400 por Body Inválido
### ¿Por qué la restricción de ruta responde 404 mientras que un body inválido responde 400?
- **El URI como identificador sintáctico:** El URI identifica un recurso. La plantilla de ruta `api/tickets/{id:int:min(1)}` define que dicho recurso solo existe bajo un esquema de identificación entero mayor o igual a 1. Cuando se envía `/api/tickets/abc`, el motor de enrutamiento de ASP.NET Core evalúa sus tablas de rutas y determina que no existe ningún endpoint que coincida con esa gramática de URL. Por lo tanto, responde `404 Not Found` en la etapa de enrutamiento, antes de que cualquier controlador sea instanciado.
- **El Body como datos de la solicitud:** En cambio, cuando un body es sintácticamente inválido o viola reglas de validación (`DataAnnotations`), el endpoint sí fue localizado con éxito en el árbol de rutas (la URL existe), pero los datos provistos en la carga útil no pueden ser procesados o no cumplen los contratos de entrada. Por ello, el framework genera `400 Bad Request` indicando que la solicitud es defectuosa.

### ¿Cuándo usar cada mecanismo?
- **Restricción de ruta (`IRouteConstraint` / `:int` / `:codigoTicket`):** Se utiliza para resolver ambigüedades en el pipeline de enrutamiento, separar controladores con prefijos similares (ej. `/api/tickets/{id:int}` vs `/api/tickets/codigo/{codigo:codigoTicket}`) y descartar de inmediato solicitudes con identificadores mal formados sin gastar recursos en instanciar controladores ni consultar capas inferiores.
- **Validación de modelo / Body (`[ApiController]` / `ValidationProblem`):** Se utiliza cuando la ruta fue encontrada y se necesita validar la completitud semántica de los datos (longitudes de texto, rangos, obligatoriedad, reglas de formato) devolviendo al cliente una lista detallada de errores por campo (`errors`).

---

## 4. Comunicación Flujo -> API sin Acoplamiento a HTTP
La regla arquitectónica del proyecto prohíbe que la capa `Flujo` dependa de paquetes de ASP.NET Core (`Microsoft.AspNetCore.*`).

### Comparación de alternativas:
1. **Alternativa 1: Excepciones personalizadas de dominio (`ReglaNegocioException`, `EntidadNoEncontradaException`):**
   - *Funcionamiento:* Flujo lanza excepciones cuando una regla o búsqueda falla. Un filtro de excepciones o middleware global en API captura la excepción y la convierte en la respuesta HTTP correspondiente (404, 409, 400).
   - *Desventajas y costo:* Las excepciones en .NET son muy costosas en rendimiento debido al armado de la traza de pila (`stack unwinding`). Utilizar excepciones para el flujo normal de control de validaciones viola el principio de diseño de que las excepciones solo deben representar condiciones excepcionales e imprevistas.
2. **Alternativa 2: Patrón Resultado / Result Object (`ResultadoOperacion<T>`) (Elegida):**
   - *Funcionamiento:* Se definió en la capa [`Abtracciones`](file:///c:/Users/Fabián%20Sojo/Documents/Universidad/PrograAvanzadaWeb/SistemaSoporte/Abtracciones/) la clase `ResultadoOperacion<T>` con propiedades inmutables `Exito`, `Valor`, `TipoError` (`Ninguno`, `NoEncontrado`, `ReglaNegocio`, `Invalido`) y `Mensaje`. Flujo retorna este objeto determinista y la capa `API` simplemente inspecciona el `TipoError` para mapearlo a la respuesta HTTP adecuada (`Ok`, `ProblemDetails` con 404, 409 o 400).
   - *Costo:* Requiere escribir una función de mapeo en los controladores (`TraducirFalla`) o delegates de Minimal API, pero proporciona cero sobrecarga de rendimiento, tipado estricto y total desacoplamiento de HTTP.

---

## 5. Paginación
### ¿Por qué limitar el tamaño máximo de página?
Limitar el tamaño de página (fijado en **20** para la Variante 0) es un control indispensable de estabilidad y seguridad (prevención de Denial of Service involuntario). Si no existiera un límite superior, un cliente podría solicitar `?tamano=1000000`, provocando asignación masiva de memoria, sobrecarga del recolector de basura (GC) y saturación del canal de red, degradando el servicio para todos los usuarios.

### ¿Qué devuelve una página fuera de rango y por qué?
Devuelve una lista vacía `[]` con código `200 OK` y los encabezados `X-Total-Count: N`, `X-Page: P`, `X-Page-Size: S`.
- *Por qué 200 y no 404:* En la arquitectura REST, el recurso coleccionado sí existe; lo que ocurre es que el subconjunto paginado actual no contiene elementos. Retornar 404 sugeriría erróneamente que el recurso no existe, mientras que con 200 y `X-Total-Count`, el cliente puede saber cuántos registros hay en total y ajustar sus controles de paginación.

### ¿Qué es la paginación por cursor y por qué no se usó aquí?
- **Paginación por cursor (Keyset Pagination):** En lugar de indicar página y salto (`OFFSET / SKIP`), el cliente envía un puntero o "cursor" opaco que corresponde al identificador del último registro visto (por ejemplo: `?cursor=TK-2026-0010&tamano=10`), y la consulta recupera los N registros siguientes de forma continua. Es ideal para "scroll infinito" y grandes volúmenes con inserciones en tiempo real.
- **Por qué no se usó en esta práctica:**
  1. No permite el salto directo a una página arbitraria (ej. ir directamente a la página 3).
  2. La práctica exige controles de navegación previa y siguiente y ordenación dinámica por múltiples columnas (`fecha`, `-fecha`, `prioridad`, `-prioridad`), lo cual es sumamente complejo de implementar con cursores compuestos y trivial con paginación por desplazamiento (`Skip/Take`).

---

## 6. ETag y Caché Condicional
### ¿Qué problema resuelve?
Resuelve el desperdicio de ancho de banda y la sobrecarga computacional en peticiones de lectura repetitivas. Permite a los clientes y proxies intermediarios verificar si su copia en caché sigue siendo idéntica a la del servidor.

### ¿Fuerte o débil, y cómo se calculó?
- **Tipo:** Se implementó un **ETag Fuerte** (`"hash"` sin prefijo `W/`), garantizando que la representación byte a byte del recurso no ha cambiado.
- **Cálculo:** Se calcula obteniendo un hash MD5 a partir de las propiedades esenciales del ticket:
  `raw = $"{Id}:{Codigo}:{FechaActualizacion.Ticks}:{Estado}:{Prioridad}:{AgenteId}:{Reaperturas}"`
  El hash resultante se formatea en hexadecimal y se envuelve en comillas dobles según RFC 9110: `"{hash}"`.
- **Comportamiento:** Si el cliente envía la cabecera `If-None-Match` con el ETag vigente, el controlador responde inmediatamente con `304 Not Modified` con el header `ETag` y sin cuerpo (`body`), ahorrando transferencia de datos.

### ¿Qué situación no cubre?
No cubre cambios en entidades externas asociadas que no modifiquen el ticket en sí (por ejemplo, si el cliente con `ClienteId: 1` cambia su nombre en la base de datos de Clientes, el ETag del ticket seguirá siendo el mismo porque sus propiedades intrínsecas no han cambiado). Tampoco previene por sí solo colisiones de escritura si el cliente no envía encabezados `If-Match` en operaciones de actualización (`PATCH`/`PUT`).

---

## 7. Transición de Estado Inválida: ¿Por qué 409 y no 400 ni 422?
La especificación HTTP RFC 9110 (sección 15.5.10) establece textualmente:
> *"The 409 (Conflict) status code indicates that the request could not be completed due to a conflict with the current state of the target resource."*

- **Frente a `400 Bad Request`:** Un error 400 implica que la sintaxis de la petición o sus tipos de datos son erróneos (ej. enviar una fecha malformada o un string donde va un entero). Al intentar pasar un ticket de `EnProceso` a `Cerrado`, el JSON enviado es sintácticamente perfecto (`{"estado":"Cerrado"}`); el problema radica en que el ticket en el servidor se encuentra en un estado que no admite dicha transición.
- **Frente a `422 Unprocessable Content`:** El código 422 se refiere a errores semánticos en las instrucciones, pero el RFC 9110 especifica que cuando la causa del rechazo es el **estado actual persistido del recurso en el servidor**, el código canónico es **`409 Conflict`**.

---

## 8. Fuentes Consultadas
1. **Microsoft Learn:** *Routing in ASP.NET Core - Custom Route Constraints*  
   https://learn.microsoft.com/aspnet/core/fundamentals/routing
2. **Microsoft Learn:** *Handle errors in ASP.NET Core web APIs (ProblemDetails RFC 9457)*  
   https://learn.microsoft.com/aspnet/core/web-api/handle-errors
3. **Microsoft Learn:** *JSON serialization and JsonStringEnumConverter in .NET*  
   https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/customize-properties
4. **IETF RFC 9110:** *HTTP Semantics - Section 15: Status Codes (304, 400, 404, 409, 422)*  
   https://www.rfc-editor.org/rfc/rfc9110.html
5. **IETF RFC 9457:** *Problem Details for HTTP APIs*  
   https://www.rfc-editor.org/rfc/rfc9457.html

---

## 9. Declaración de Uso de Asistente de IA
Conforme a las reglas del juego de la práctica académica (Sección 2):
- **Qué se le pidió a la IA:**
  1. Realizar una auditoría técnica y diagnóstico de la solución existente `SistemaSoporte` (.NET 10).
  2. Estructurar la matriz de trazabilidad y el plan incremental para la **Variante 0** (límite de 20 registros, código `TK-AAAA-NNNN` y regla de prioridad Crítica con Agente).
  3. Implementar paso a paso, incremento por incremento: contratos y Patrón Resultado en `Abtracciones`, persistencia en memoria y semilla en `DA/TicketDA.cs`, máquina de estados en `Flujo/TicketFlujo.cs`, pipeline y restricción de ruta en `API/Constraints`, endpoints REST con ETag en `TicketsController`, endpoints Minimal API y UI en `tickets.html`.
- **Qué devolvió la IA:**
  - Código C# por capas, scripts de compilación y verificación, y la estructura de los contratos y vistas HTML.
- **Qué se corrigió, ajustó o validó personalmente:**
  - Se revisó la pertinencia de la inicialización de la semilla para garantizar que los `ClienteId` existieran en `ClienteDA`.
  - Se confirmó la decisión de devolver `400 Bad Request` ante clientes inexistentes para alinearse con RFC 9457.
  - Se verificó que los endpoints Minimal API y los controladores convivieran en el mismo proyecto API sin colisiones de serialización mediante la doble configuración de `JsonStringEnumConverter`.
  - Se comprobó en vivo la compilación limpia y la ejecución de cada código HTTP ante peticiones reales de prueba.
