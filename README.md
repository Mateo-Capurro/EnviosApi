# EnviosApi

API REST para la **gestión de envíos y clientes** construida con ASP.NET Core (.NET 10). Permite registrar clientes, crear envíos asociados a ellos y hacer avanzar cada envío por su ciclo de vida (`Pendiente` → `EnTransito` → `Entregado`, o `Cancelado`) respetando reglas de negocio. El acceso está protegido con JWT y roles.

## Tecnologías

- **.NET 10** / ASP.NET Core Web API (controllers)
- **Entity Framework Core 10** con migraciones (Code First)
- **SQL Server** (SQL Express en local, SQL Server 2022 en Docker)
- **JWT** (`Microsoft.AspNetCore.Authentication.JwtBearer`) con autorización por roles
- **BCrypt** (`BCrypt.Net-Next`) para el hash de contraseñas
- **OpenAPI** nativo (`Microsoft.AspNetCore.OpenApi`) + **Swagger UI**
- **Docker** y **Docker Compose** (build multi-stage, contenedor sin root)

## Arquitectura

La API está organizada en capas con responsabilidades separadas:

```
Controllers  ->  Services  ->  Repositories  ->  AppDbContext (EF Core)  ->  SQL Server
```

- **Controllers**: reciben la petición HTTP, validan el DTO de entrada (`[ApiController]` + DataAnnotations) y devuelven el código de estado adecuado. No contienen lógica de negocio.
- **Services**: contienen las reglas de negocio (transiciones de estado, DNI único, asignación de roles, login) y mapean entidades a DTOs.
- **Repositories**: encapsulan el acceso a datos con EF Core detrás de interfaces (`IEnvioRepository`, `IClienteRepository`, `IUsuarioRepository`).
- **AppDbContext**: configura el modelo (índices únicos, longitudes de columnas, enum guardado como texto, relación Cliente 1—N Envíos).
- **DTOs**: la API nunca expone las entidades directamente. Hay DTOs de entrada (`EnvioCreateDto`, `ClienteCreateDto`, `RegisterDto`, …) con validaciones y DTOs de salida (`EnvioDto`, `ClienteDto`, `UsuarioDto`, `TokenDto`). Por ejemplo, `UsuarioDto` nunca incluye el hash de la contraseña.
- **Inyección de dependencias**: servicios y repositorios se registran como `Scoped` en `Program.cs` y se inyectan por constructor mediante sus interfaces.
- **Manejo global de errores**: `GlobalExceptionHandler` (`IExceptionHandler`) traduce las excepciones de dominio a respuestas [`ProblemDetails`](https://www.rfc-editor.org/rfc/rfc9457) con un código HTTP coherente, sin `try/catch` en los controllers:

  | Excepción               | HTTP | Título                          |
  |-------------------------|------|---------------------------------|
  | `NotFoundException`     | 404  | Recurso no encontrado           |
  | `BusinessRuleException` | 400  | Regla de negocio incumplida     |
  | `ConflictException`     | 409  | Conflicto con el estado actual  |
  | `UnauthorizedException` | 401  | No autorizado                   |
  | Cualquier otra          | 500  | Error interno del servidor (sin detalle, para no filtrar información interna) |

### Estructura de carpetas

```
EnviosApi/
├── docker-compose.yml
├── .env.example
└── EnviosApi/
    ├── Controllers/       # AuthController, ClientesController, EnviosController
    ├── Services/          # Lógica de negocio + TokenService (JWT)
    ├── Repositories/      # Acceso a datos con EF Core
    ├── Data/              # AppDbContext
    ├── Models/            # Entidades: Cliente, Envio, Usuario, EstadoEnvio
    ├── DTOs/              # Contratos de entrada y salida
    ├── Exceptions/        # Excepciones de dominio + GlobalExceptionHandler
    ├── Infrastructure/    # Esquema Bearer para OpenAPI/Swagger
    ├── Migrations/        # Migraciones de EF Core
    ├── Program.cs
    ├── Dockerfile
    └── EnviosApi.http     # Peticiones de ejemplo
```

## Endpoints

| Método   | Ruta                       | Descripción                                   | Acceso        |
|----------|----------------------------|-----------------------------------------------|---------------|
| `POST`   | `/api/auth/register`       | Registra un usuario                           | Público       |
| `POST`   | `/api/auth/login`          | Inicia sesión y devuelve el JWT               | Público       |
| `GET`    | `/api/clientes`            | Lista los clientes                            | Autenticado   |
| `GET`    | `/api/clientes/{id}`       | Obtiene un cliente por id                     | Autenticado   |
| `POST`   | `/api/clientes`            | Crea un cliente                               | Autenticado   |
| `PUT`    | `/api/clientes/{id}`       | Actualiza un cliente                          | Autenticado   |
| `DELETE` | `/api/clientes/{id}`       | Elimina un cliente                            | Admin         |
| `GET`    | `/api/envios`              | Lista los envíos                              | Autenticado   |
| `GET`    | `/api/envios/{id}`         | Obtiene un envío por id                       | Autenticado   |
| `POST`   | `/api/envios`              | Crea un envío (nace en estado `Pendiente`)    | Autenticado   |
| `PATCH`  | `/api/envios/{id}/estado`  | Cambia el estado de un envío                  | Autenticado   |
| `DELETE` | `/api/envios/{id}`         | Elimina un envío                              | Admin         |

Sin token, los endpoints autenticados responden `401`; con un token de rol `Operador`, los endpoints exclusivos de Admin responden `403`.

## Reglas de negocio

**Envíos**

- Todo envío se crea en estado `Pendiente`, con la fecha de creación en UTC.
- Para crear un envío, el cliente indicado debe existir (si no, `404`).
- Transiciones de estado válidas:

  | Estado actual | Puede pasar a              |
  |---------------|----------------------------|
  | `Pendiente`   | `EnTransito`, `Cancelado`  |
  | `EnTransito`  | `Entregado`, `Cancelado`   |
  | `Entregado`   | — (estado final)           |
  | `Cancelado`   | — (estado final)           |

  Cualquier otra transición (por ejemplo `Pendiente` → `Entregado`, o volver desde un estado final) responde `400` con un `ProblemDetails` que indica el motivo.
- Los estados se envían y reciben como texto (`"EnTransito"`), no como números, y se guardan como texto en la base.

**Clientes**

- El DNI es único (índice único en la base y validación en el servicio): crear o actualizar con un DNI ya usado por otro cliente responde `409`.
- El DNI debe tener 7 u 8 dígitos; el email debe ser válido.
- No se puede eliminar un cliente que tiene envíos (`409`).

**Usuarios**

- **El primer usuario registrado obtiene el rol `Admin`**; todos los siguientes se registran como `Operador`.
- El email es único y se normaliza (sin espacios, en minúsculas) antes de guardarlo y al hacer login. Registrar un email existente responde `409`.
- La contraseña debe tener al menos 8 caracteres.

## Seguridad

- **JWT**: `POST /api/auth/login` devuelve un token firmado con HMAC-SHA256 que incluye el id del usuario (`sub`), su email y su rol. Se validan emisor, audiencia, firma y vencimiento. El token dura 60 minutos por defecto (`Jwt:ExpireMinutes`).
- **Contraseñas**: se guardan únicamente como hash BCrypt (con sal incluida); nunca en texto plano.
- **Login sin enumeración de usuarios**: email inexistente y contraseña incorrecta devuelven el mismo mensaje (`401`), así no se revela qué emails están registrados.
- **Roles**: `Admin` y `Operador`. Las operaciones de borrado requieren `Admin` (`[Authorize(Roles = "Admin")]`).
- **Secretos fuera del repositorio**: `Jwt:Key` **no está en `appsettings.json`**. En desarrollo se configura con *user-secrets* y en Docker con la variable de entorno `Jwt__Key` (tomada de `.env`, que está en `.gitignore`). Si la clave falta o tiene menos de 32 bytes, la aplicación no arranca y muestra un mensaje explicando cómo configurarla.

## Cómo ejecutarlo

### a) Con Docker (recomendado)

Requisito: Docker Desktop (o Docker Engine + Compose).

1. Copiá el archivo de ejemplo y completá los valores:

   ```bash
   cp .env.example .env
   ```

   - `SA_PASSWORD`: contraseña del usuario `sa` de SQL Server (mínimo 8 caracteres con mayúsculas, minúsculas, números y símbolos; evitá `;` y `$`).
   - `JWT_KEY`: clave para firmar los JWT, de al menos 32 bytes (recomendado 48+ caracteres aleatorios).

2. Levantá los contenedores:

   ```bash
   docker compose up --build -d
   ```

   Compose levanta SQL Server 2022, espera a que pase su *healthcheck* y luego inicia la API, que **aplica las migraciones automáticamente** al arrancar (con reintentos).

3. Abrí Swagger en **http://localhost:5006/swagger**.

Para detenerlo: `docker compose down` (agregá `-v` si también querés borrar el volumen con los datos).

### b) Sin Docker

Requisitos:

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server o SQL Server Express
- Herramienta de EF Core: `dotnet tool install --global dotnet-ef`

Pasos (desde la carpeta `EnviosApi/` del proyecto):

1. **Connection string**: por defecto `appsettings.json` apunta a SQL Express local con autenticación de Windows:

   ```
   Server=localhost\SQLEXPRESS;Database=EnviosDb;Trusted_Connection=True;TrustServerCertificate=True
   ```

   Si tu instancia es otra, podés sobrescribirla sin tocar el archivo:

   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<tu connection string>"
   ```

2. **Clave JWT** (al menos 32 caracteres):

   ```bash
   dotnet user-secrets set "Jwt:Key" "<clave aleatoria de al menos 32 caracteres>"
   ```

3. **Crear la base de datos** aplicando las migraciones:

   ```bash
   dotnet ef database update
   ```

4. **Ejecutar**:

   ```bash
   dotnet run
   ```

   Swagger queda disponible en **http://localhost:5006/swagger** (Swagger UI se habilita en el entorno `Development`).

## Cómo probarlo en Swagger

1. **Registrar un usuario**: `POST /api/auth/register`

   ```json
   { "email": "admin@envios.com", "password": "Password123" }
   ```

   Como es el primer usuario, queda con rol `Admin`.

2. **Iniciar sesión**: `POST /api/auth/login` con las mismas credenciales. Copiá el valor de `token` de la respuesta.

3. **Autorizar**: hacé clic en el botón **Authorize** (arriba a la derecha), pegá el token **sin** el prefijo `Bearer` y confirmá. A partir de ahí Swagger envía el token en todas las peticiones.

4. **Crear un cliente**: `POST /api/clientes`

   ```json
   { "nombre": "Juan Pérez", "dni": "30123456", "email": "juan@mail.com" }
   ```

5. **Crear un envío** para ese cliente: `POST /api/envios`

   ```json
   { "direccion": "Av. Colón 1234", "clienteId": 1 }
   ```

6. **Cambiar el estado**: `PATCH /api/envios/{id}/estado`

   ```json
   { "estado": "EnTransito" }
   ```

   Probá también una transición inválida (por ejemplo, de `Pendiente` directo a `Entregado`) para ver la respuesta `400` en formato `ProblemDetails`.

También podés usar el archivo [`EnviosApi/EnviosApi.http`](EnviosApi/EnviosApi.http) desde Visual Studio, Rider o VS Code (extensión REST Client): el request de login guarda el token y los siguientes lo reutilizan.

## Capturas

### Swagger UI y autenticación

Encabezado de Swagger con el botón **Authorize** para cargar el JWT y los endpoints públicos de registro y login.

<img width="927" height="532" alt="Swagger UI con el botón Authorize y los endpoints de Auth" src="https://github.com/user-attachments/assets/b85ba05b-29f4-45a3-b85b-5e6b45a9b354" />

### Endpoints de clientes y envíos

CRUD de clientes y operaciones sobre envíos, incluido el cambio de estado con `PATCH /api/envios/{id}/estado`.

<img width="935" height="811" alt="Endpoints de Clientes y Envios en Swagger UI" src="https://github.com/user-attachments/assets/f1022b1d-7e06-4024-8aaf-34e46a6edd6f" />

## Próximos pasos

Mejoras planificadas que **todavía no están implementadas**:

- [ ] Tests unitarios con xUnit para los servicios (reglas de negocio y autenticación).
- [ ] Paginación en los listados de clientes y envíos.
- [ ] Refresh tokens para renovar la sesión sin volver a iniciar sesión.

## Autor

**Mateo Capurro** — [github.com/Mateo-Capurro](https://github.com/Mateo-Capurro)
