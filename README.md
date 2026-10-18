# SistemaSoporte - Módulo de Tickets (Mesa de Ayuda)

**Curso:** SC-701 Programación Avanzada en Web  
**Evaluación:** Práctica Programada 1 - API de Tickets  
**Estudiante:** Fabián Sojo  
**Variante Personal:** **Variante 0** (Carnet terminado en 0 o 5)

---

## 1. Arquitectura de la Solución

El proyecto sigue una arquitectura por capas estrictamente desacoplada mediante proyectos .NET:

- **[`Abtracciones`](file:///c:/Users/Fabián%20Sojo/Documents/Universidad/PrograAvanzadaWeb/SistemaSoporte/Abtracciones/):** Define los modelos del dominio (`Ticket`, `Cliente`), enumeraciones (`PrioridadTicket`, `EstadoTicket`), DTOs de transferencia y el Patrón Resultado (`ResultadoOperacion<T>`) para comunicar estados sin acoplar la capa de negocio a HTTP.
- **[`DA`](file:///c:/Users/Fabián%20Sojo/Documents/Universidad/PrograAvanzadaWeb/SistemaSoporte/DA/):** Acceso a datos. Implementa la persistencia de Tickets en memoria (`TicketDA.cs`) hilo-segura con cerrojo de sincronización `lock`, precargada con 26 tickets de ejemplo y sincronizada con clientes existentes.
- **[`Flujo`](file:///c:/Users/Fabián%20Sojo/Documents/Universidad/PrograAvanzadaWeb/SistemaSoporte/Flujo/):** Lógica pura de negocio. Aplica las máquinas de estados, correlativos `TK-AAAA-NNNN` y la regla de negocio especial de la Variante 0. No referencia ningún paquete de ASP.NET Core (`Microsoft.AspNetCore.*`).
- **[`API`](file:///c:/Users/Fabián%20Sojo/Documents/Universidad/PrograAvanzadaWeb/SistemaSoporte/API/):** Presentación HTTP. Contiene el controlador `TicketsController`, endpoints Minimal API (`/min/tickets/resumen`, `/min/tickets/{id}/reabrir`), la restricción de ruta personalizada `CodigoTicketConstraint`, documentación OpenAPI/Scalar y la interfaz web `tickets.html`.

---

## 2. Parámetros de la Variante 0
- **Tamaño máximo de página:** 20 registros.
- **Formato del código:** `TK-AAAA-NNNN` (ej. `TK-2026-0001`).
- **Restricción de ruta personalizada:** Valida la expresión regular `^TK-\d{4}-\d{4}$`.
- **Regla de negocio especial:** *Un ticket de prioridad Crítica no puede pasar a Resuelto sin tener AgenteId asignado (responde 409 al violarse).*

---

## 3. Requisitos Previos
- [.NET 10 SDK](https://dotnet.microsoft.com/) instalado (`net10.0`).
- SQL Server LocalDB (`(localdb)\MSSQLLocalDB`) para la persistencia del módulo de Clientes.

---

## 4. Instrucciones para Correr el Proyecto

### 4.1. Clonar el repositorio y restaurar dependencias
```bash
git clone <URL_DEL_REPOSITORIO>
cd SistemaSoporte
dotnet restore
```

### 4.2. Aplicar migraciones a la Base de Datos (para Clientes)
```bash
dotnet ef database update --project DA --startup-project API
```

### 4.3. Compilar la solución
```bash
dotnet build
```

### 4.4. Ejecutar la API
```bash
dotnet run --project API
```
Por defecto, la API se levantará en los puertos configurados (ej. `http://localhost:5000` o `https://localhost:5001`).

---

## 5. Accesos y Navegación

- **Interfaz Web Interactiva:**  
  Visite en su navegador: `http://localhost:<PUERTO>/tickets.html` (o `index.html` para clientes).
- **Documentación Interactiva Scalar:**  
  Visite: `http://localhost:<PUERTO>/scalar/v1`
- **Documento OpenAPI (JSON):**  
  Visite: `http://localhost:<PUERTO>/openapi/v1.json`

---

## 6. Endpoints Principales del Módulo Tickets

| Método | Ruta | Tipo | Descripción |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/tickets` | Controller | Listado paginado con filtros (`estado`, `prioridad`, `clienteId`, `desde`, `hasta`, `q`, `orden`, `pagina`, `tamano` max 20). Responde headers `X-Total-Count`, `X-Page`, `X-Page-Size`. |
| `GET` | `/api/tickets/{id}` | Controller | Consulta por ID con restricción entera `{id:int:min(1)}`. Soporta caché condicional con `ETag` y responde `304 Not Modified` si coincide `If-None-Match`. |
| `GET` | `/api/tickets/codigo/{codigo}` | Controller | Consulta por código con restricción de ruta personalizada `codigoTicket` (`TK-AAAA-NNNN`). Formatos inválidos responden `404` por ruteo. |
| `POST` | `/api/tickets` | Controller | Crea un ticket en estado `Abierto` con código autogenerado. Responde `201 Created` con encabezado `Location`. |
| `PATCH` | `/api/tickets/{id}/estado` | Controller | Actualiza estado, agente o prioridad. Valida transiciones válidas y regla de Variante 0 (responde `409 Conflict` si se viola). |
| `GET` | `/min/tickets/resumen` | Minimal API | Responde `200 OK` con `{ "total": n, "porEstado": {...}, "porPrioridad": {...} }`. |
| `POST` | `/min/tickets/{id}/reabrir` | Minimal API | Reabre un ticket `Resuelto` pasándolo a `EnProceso` e incrementando `Reaperturas`. Responde `409` si no estaba resuelto. |
| `GET` | `/tickets.html` | Archivo estático | Página web en Vanilla JS/CSS con tabla, filtros interactivos y paginación consumiendo el API. |

---

## 7. Pruebas Rápidas con `curl`

### Paginación y límite de página (Variante 0: max 20)
```bash
# Paginación normal (200 OK con headers X-Total-Count, X-Page, X-Page-Size)
curl -i "http://localhost:<PUERTO>/api/tickets?pagina=1&tamano=10"

# Exceder el tamaño máximo (400 Bad Request ProblemDetails)
curl -i "http://localhost:<PUERTO>/api/tickets?tamano=25"
```

### Caché condicional con ETag (304 Not Modified)
```bash
# Obtener ticket y su ETag
curl -i "http://localhost:<PUERTO>/api/tickets/1"

# Enviar If-None-Match con el ETag obtenido (304 Not Modified sin cuerpo)
curl -i -H 'If-None-Match: "<ETAG_OBTENIDO>"' "http://localhost:<PUERTO>/api/tickets/1"
```

### Restricción de ruta personalizada para Variante 0
```bash
# Código con formato válido (200 OK)
curl -i "http://localhost:<PUERTO>/api/tickets/codigo/TK-2026-0001"

# Código con formato inválido (404 Not Found a nivel de enrutador)
curl -i "http://localhost:<PUERTO>/api/tickets/codigo/INVALIDO"
```

### Regla de negocio especial (Variante 0: Crítico requiere Agente para resolver)
```bash
# Intentar resolver ticket crítico sin agente (409 Conflict)
curl -i -X PATCH "http://localhost:<PUERTO>/api/tickets/9/estado" \
  -H "Content-Type: application/json" \
  -d '{"estado":"Resuelto"}'

# Resolver asignando agente en la misma petición (200 OK)
curl -i -X PATCH "http://localhost:<PUERTO>/api/tickets/9/estado" \
  -H "Content-Type: application/json" \
  -d '{"estado":"Resuelto", "agenteId": 105}'
```

---

## 8. Documentos de Entrega
- [`DECISIONES.md`](file:///c:/Users/Fabián%20Sojo/Documents/Universidad/PrograAvanzadaWeb/SistemaSoporte/DECISIONES.md): Justificación técnica exhaustiva de los 9 puntos solicitados en la rúbrica.
- Historial de commits progresivo en Git sin carpetas `bin` ni `obj`.
