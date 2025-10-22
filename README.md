# Orders API - Ciclos de Vida de Dependencias en ASP.NET Core

##  Descripción del Proyecto

Esta API demuestra los tres ciclos de vida de inyección de dependencias en ASP.NET Core: **Transient**, **Scoped** y **Singleton**. Utiliza el mismo servicio (`OrderService`) registrado con diferentes ciclos de vida para ilustrar cómo cada uno gestiona las instancias.

**Repositorio:** https://github.com/DRMiguel25/Tipos-de-Inyecci-n-de-Dependencias-en-.NET.git

##  Implementación de Cada Tipo de Servicio

### 1. **Transient** (`AddKeyedTransient`)
```csharp
builder.Services.AddKeyedTransient<IOrderService, OrderService>("transient");
```

**Características:**
- Se crea una **nueva instancia** cada vez que se solicita el servicio
- Cada inyección en el constructor genera un objeto diferente
- Cada petición HTTP obtiene instancias completamente independientes

**Implementación en el código:**
- Registrado con la clave `"transient"` en `Program.cs`
- Inyectado en el controlador usando `[FromKeyedServices("transient")]`
- Los datos (`_orders`) no persisten entre peticiones porque cada instancia tiene su propia lista

### 2. **Scoped** (`AddKeyedScoped`)
```csharp
builder.Services.AddKeyedScoped<IOrderService, OrderService>("scoped");
```

**Características:**
- Se crea **una instancia por solicitud HTTP**
- Dentro de la misma petición, todas las inyecciones comparten la misma instancia
- Al finalizar la petición, la instancia se destruye

**Implementación en el código:**
- Registrado con la clave `"scoped"`
- Ideal para operaciones que necesitan compartir estado durante una transacción HTTP
- Los datos persisten solo durante la ejecución de una petición

### 3. **Singleton** (`AddKeyedSingleton`)
```csharp
builder.Services.AddKeyedSingleton<IOrderService, OrderService>("singleton");
```

**Características:**
- Se crea **una única instancia** durante toda la vida de la aplicación
- Todas las peticiones y todos los servicios comparten la misma instancia
- Los datos persisten mientras la aplicación esté ejecutándose

**Implementación en el código:**
- Registrado con la clave `"singleton"`
- La lista `_orders` se mantiene entre todas las peticiones HTTP
- El `_instanceId` es el mismo para todas las operaciones

##  Comportamiento Observado en las Pruebas

### Prueba con **Transient**

| Acción | Resultado |
|--------|-----------|
| POST `/api/orders/transient` | Agrega un pedido, pero... |
| GET `/api/orders/transient` | **Cantidad: 0** - La lista está vacía |
| Múltiples POST | Cada POST crea una nueva instancia, no hay acumulación |
| `GetInstanceId()` | Devuelve GUIDs diferentes en cada petición |

**Observación clave:** Los datos **no persisten** porque cada petición usa una instancia diferente.

**Ejemplo de respuesta:**
```json
{
  "Ciclo": "Transient",
  "Instancia": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "Cantidad": 0,
  "Pedidos": []
}
```

### Prueba con **Scoped**

| Acción | Resultado |
|--------|-----------|
| POST `/api/orders/scoped` | Agrega un pedido |
| GET `/api/orders/scoped` (misma petición) | Mostraría el pedido si se hace en el mismo request |
| GET `/api/orders/scoped` (nueva petición) | **Cantidad: 0** - Nueva instancia |
| `GetInstanceId()` | Cambia entre peticiones, pero sería igual dentro de la misma |

**Observación clave:** Los datos persisten **solo durante la petición** actual.

**Ejemplo de respuesta:**
```json
{
  "Ciclo": "Scoped",
  "Instancia": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
  "Cantidad": 0,
  "Pedidos": []
}
```

### Prueba con **Singleton**

| Acción | Resultado |
|--------|-----------|
| POST `/api/orders/singleton` (Pedido 1) | Total: 1 |
| POST `/api/orders/singleton` (Pedido 2) | Total: 2 |
| GET `/api/orders/singleton` | **Cantidad: 2** - Todos los pedidos están ahí |
| Reiniciar navegador/cliente | Los datos **siguen ahí** |
| `GetInstanceId()` | Siempre el mismo GUID |

**Observación clave:** Los datos persisten **durante toda la ejecución de la aplicación**.

**Ejemplo de respuesta después de agregar 2 pedidos:**
```json
{
  "Ciclo": "Singleton",
  "Instancia": "c3d4e5f6-a7b8-9012-cdef-123456789012",
  "Cantidad": 2,
  "Pedidos": [
    {
      "Id": 1,
      "NombreProducto": "Laptop",
      "Cantidad": 1,
      "Fecha": "2025-10-22T10:30:00Z"
    },
    {
      "Id": 2,
      "NombreProducto": "Mouse",
      "Cantidad": 3,
      "Fecha": "2025-10-22T10:31:00Z"
    }
  ]
}
```

##  Escenarios de Uso en Proyectos Reales

###  Cuándo usar **Transient**

**Escenarios ideales:**
- **Servicios ligeros sin estado** (stateless)
- Operaciones de transformación de datos
- Validadores que no mantienen información
- Servicios de mapeo (AutoMapper profiles)
- Generadores de tokens o IDs únicos
- Servicios de notificaciones por email/SMS

**Ejemplo real:**
```csharp
// Servicio que genera PDFs - cada petición necesita su propio generador
builder.Services.AddTransient<IPdfGenerator, PdfGenerator>();

// Servicio de encriptación - sin estado compartido
builder.Services.AddTransient<IEncryptionService, AesEncryptionService>();

// Validadores
builder.Services.AddTransient<IOrderValidator, OrderValidator>();
```

** No usar cuando:**
- El servicio es costoso de crear (conexiones a BD, clientes HTTP)
- Necesitas compartir estado entre componentes
- El servicio mantiene recursos no administrados

###  Cuándo usar **Scoped**

**Escenarios ideales:**
- **DbContext de Entity Framework** (el caso más común)
- Unidades de trabajo (Unit of Work pattern)
- Servicios que necesitan mantener estado durante una petición
- Tracking de cambios en una transacción
- Servicios de auditoría por petición
- Repositorios que comparten el mismo contexto de BD

**Ejemplo real:**
```csharp
// DbContext - una instancia por petición HTTP
builder.Services.AddScoped<ApplicationDbContext>();

// Servicio de auditoría que registra acciones en la misma transacción
builder.Services.AddScoped<IAuditService, AuditService>();

// Unit of Work que coordina múltiples repositorios
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Repositorio que usa el DbContext scoped
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
```

** No usar cuando:**
- Necesitas compartir datos entre diferentes usuarios/peticiones
- El servicio es completamente stateless (mejor Transient)
- Necesitas persistencia más allá de una petición HTTP

###  Cuándo usar **Singleton**

**Escenarios ideales:**
- **Configuraciones que no cambian** durante la ejecución
- Cachés en memoria (MemoryCache)
- Clientes HTTP reutilizables (HttpClient con IHttpClientFactory)
- Servicios de logging
- Servicios de configuración
- Contadores globales o estadísticas de la aplicación
- Servicios de feature flags

**Ejemplo real:**
```csharp
// Caché global de la aplicación
builder.Services.AddSingleton<IMemoryCache, MemoryCache>();

// Servicio de configuración
builder.Services.AddSingleton<IAppSettings, AppSettings>();

// Cliente HTTP compartido
builder.Services.AddHttpClient<IExternalApiClient, ExternalApiClient>();

// Servicio de métricas global
builder.Services.AddSingleton<IMetricsCollector, MetricsCollector>();

// Logger factory
builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
```

** CUIDADO - No usar cuando:**
- El servicio no es **thread-safe** (puede causar condiciones de carrera)
- Mantiene estado específico del usuario (violación de privacidad/seguridad)
- Inyecta servicios Scoped (causará excepciones en runtime)
- El servicio necesita ser liberado/disposed regularmente

##  Diagramas de Ciclos de Vida

### Diagrama 1: Ciclo de Vida General
```
┌─────────────────────────────────────────────────────────────┐
│                    APLICACIÓN ASP.NET CORE                   │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │  TRANSIENT   │  │   SCOPED     │  │  SINGLETON   │      │
│  ├──────────────┤  ├──────────────┤  ├──────────────┤      │
│  │ Nueva inst.  │  │ Una inst.    │  │ Una inst.    │      │
│  │ por inyec.   │  │ por request  │  │ por app      │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│         │                  │                  │              │
│    ┌────┴────┐        ┌────┴────┐       ┌────┴────┐        │
│    │ Inst A1 │        │ Inst B1 │       │ Inst C  │        │
│    │ Inst A2 │        │         │       │ (única) │        │
│    │ Inst A3 │        └─────────┘       └─────────┘        │
│    │   ...   │             │                  │             │
│    └─────────┘        Se destruye       Vive toda          │
│   Se crean y           al terminar        la app           │
│   destruyen            el request                           │
│   constantemente                                            │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

### Diagrama 2: Flujo de Peticiones HTTP
```
REQUEST 1                    REQUEST 2                    REQUEST 3
───────────────              ───────────────              ───────────────

 Transient                  Transient                  Transient
Instancia: A1                Instancia: B1                Instancia: C1
Orders: []                   Orders: []                   Orders: []
POST → Agrega 1              POST → Agrega 1              GET → []
(no persiste)                (no persiste)                (lista vacía)

 Scoped                     Scoped                     Scoped
Instancia: A2                Instancia: B2                Instancia: C2
Orders: []                   Orders: []                   Orders: []
POST → Agrega 1              POST → Agrega 1              GET → []
(persiste en request)        (nueva instancia)            (nueva instancia)

 Singleton                  Singleton                  Singleton
Instancia: ÚNICA             Instancia: MISMA             Instancia: MISMA
Orders: []                   Orders: [1]                  Orders: [1, 2]
POST → Orders: [1]           POST → Orders: [1, 2]        GET → [1, 2]
(persiste siempre)           (acumula)                    (acumula)
```

### Diagrama 3: Árbol de Dependencias
```
                         ┌──────────────────┐
                         │ OrdersController │
                         └────────┬─────────┘
                                  │
                 ┌────────────────┼────────────────┐
                 │                │                │
                 ▼                ▼                ▼
        ┌─────────────┐  ┌─────────────┐  ┌─────────────┐
        │  Transient  │  │   Scoped    │  │  Singleton  │
        │ IOrderSvc   │  │ IOrderSvc   │  │ IOrderSvc   │
        └─────────────┘  └─────────────┘  └─────────────┘
               │                │                │
               │                │                │
        Cada llamada      Por request      Una vez
        = Nueva inst.     = Misma inst.    = Siempre igual
        
        InstanceId:       InstanceId:      InstanceId:
        guid-1            guid-4           guid-7
        guid-2            guid-5           guid-7
        guid-3            guid-6           guid-7
        (siempre          (cambia por      (nunca
         diferente)        request)         cambia)
```

### Diagrama 4: Persistencia de Datos
```
TIEMPO ────────────────────────────────────────────────────────►

 TRANSIENT
[Data]     [    ]     [    ]     [    ]     [    ]
   ↓         ↓         ↓         ↓         ↓
Request 1  Req 2    Req 3    Req 4    Req 5
(No hay persistencia - cada petición inicia vacío)


 SCOPED
[Data═══════]  [Data═══════]  [Data═══════]
   Request 1      Request 2      Request 3
(Persiste durante el request, luego se limpia)


 SINGLETON
[Data═══════════════════════════════════════════════════════►]
  Request 1→2→3→4→5→...→N
(Persiste durante toda la vida de la aplicación)
```

### Diagrama 5: Comparación Visual
```
┌─────────────────────────────────────────────────────────────┐
│                   COMPARACIÓN DE LIFETIMES                   │
├─────────────────┬──────────────┬──────────────┬─────────────┤
│  Característica │  Transient   │   Scoped     │  Singleton  │
├─────────────────┼──────────────┼──────────────┼─────────────┤
│ Instancias      │ Múltiples    │ 1 por req    │ Una única   │
│ Persistencia    │ No           │ Por request  │ Toda la app │
│ Thread-safe     │ Sí*          │ Sí*          │ Debe serlo  │
│ Memoria         │ Alta rotación│ Media        │ Baja        │
│ Performance     │ Más lento    │ Balanceado   │ Más rápido  │
│ Complejidad     │ Simple       │ Moderada     │ Alta        │
└─────────────────┴──────────────┴──────────────┴─────────────┘
* Si no mantiene estado compartido
```

##  Cómo Probar la API

### 1. Clonar el Repositorio
```bash
git clone https://github.com/DRMiguel25/Tipos-de-Inyecci-n-de-Dependencias-en-.NET.git
cd Tipos-de-Inyecci-n-de-Dependencias-en-.NET
```

### 2. Restaurar Dependencias y Ejecutar
```bash
dotnet restore
dotnet run
```

### 3. Acceder a la API

- **URL Base:** http://localhost:5078
- **Swagger UI:** http://localhost:5078/swagger
- **Raíz:** http://localhost:5078/

### 4. Secuencia de Pruebas Recomendada

#### Prueba A: Singleton (Persistencia Global)
```bash
# 1. Agregar primer pedido
POST http://localhost:5078/api/orders/singleton
Body: {
  "nombreProducto": "Laptop Dell",
  "cantidad": 1
}

# 2. Agregar segundo pedido
POST http://localhost:5078/api/orders/singleton
Body: {
  "nombreProducto": "Mouse Logitech",
  "cantidad": 3
}

# 3. Obtener todos los pedidos
GET http://localhost:5078/api/orders/singleton
# Resultado: Verás AMBOS pedidos 

# 4. Hacer otro GET después de 5 minutos
GET http://localhost:5078/api/orders/singleton
# Resultado: Los pedidos SIGUEN ahí 
```

#### Prueba B: Transient (Sin Persistencia)
```bash
# 1. Agregar pedido
POST http://localhost:5078/api/orders/transient
Body: {
  "nombreProducto": "Teclado Mecánico",
  "cantidad": 1
}
# Observa el InstanceId en la respuesta

# 2. Intentar obtener pedidos
GET http://localhost:5078/api/orders/transient
# Resultado: Cantidad = 0, Pedidos = [] 
# Observa que el InstanceId es DIFERENTE al del POST
```

#### Prueba C: Scoped (Persistencia por Request)
```bash
# 1. Agregar pedido
POST http://localhost:5078/api/orders/scoped
Body: {
  "nombreProducto": "Monitor LG",
  "cantidad": 2
}

# 2. Inmediatamente hacer GET
GET http://localhost:5078/api/orders/scoped
# Resultado: Cantidad = 0 (es un nuevo request) 

# 3. Observar InstanceId diferentes entre requests
```

### 5. Comparación de InstanceIds
```bash
# Ejecuta estos 3 GET varias veces y observa los GUIDs:

GET http://localhost:5078/api/orders/transient
# InstanceId: cambia en CADA petición

GET http://localhost:5078/api/orders/scoped
# InstanceId: cambia en CADA petición

GET http://localhost:5078/api/orders/singleton
# InstanceId: SIEMPRE el mismo 
```

##  Resultados Esperados

### Tabla Comparativa de Pruebas

| Acción | Transient | Scoped | Singleton |
|--------|-----------|--------|-----------|
| POST pedido #1 | Agrega (Total: 1) | Agrega (Total: 1) | Agrega (Total: 1) |
| GET inmediato | Cantidad: 0  | Cantidad: 0  | Cantidad: 1  |
| POST pedido #2 | Agrega (Total: 1) | Agrega (Total: 1) | Agrega (Total: 2) |
| GET después | Cantidad: 0  | Cantidad: 0  | Cantidad: 2  |
| InstanceId | Siempre diferente | Diferente por request | Siempre igual |

##  Conclusiones y Mejores Prácticas

### Resumen de Cuándo Usar Cada Uno

1. **Transient**: 
   - Default para servicios stateless
   - Bajo consumo de memoria si el servicio es ligero
   - Ideal para operaciones independientes

2. **Scoped**: 
   - **Default recomendado para aplicaciones web**
   - Perfecto para DbContext y Unit of Work
   - Balancea rendimiento y seguridad

3. **Singleton**: 
   - Solo para servicios verdaderamente globales
   - DEBE ser thread-safe
   - Excelente para cachés y configuraciones

### Reglas de Oro

 **SÍ hacer:**
- Usar Scoped para DbContext
- Usar Singleton para cachés thread-safe
- Documentar por qué elegiste cada lifetime
- Probar la concurrencia en Singletons

 **NO hacer:**
- Inyectar Scoped en Singleton (excepción en runtime)
- Usar Singleton para estado del usuario
- Asumir que Transient es siempre la opción más segura
- Ignorar el impacto en memoria de Transient con servicios pesados

### Patrón de Decisión Rápida
```
¿El servicio mantiene estado?
├─ NO → ¿Es costoso de crear?
│       ├─ NO → Transient 
│       └─ SÍ → Singleton (si thread-safe) 
│
└─ SÍ → ¿El estado es por usuario/request?
        ├─ SÍ → Scoped 
        └─ NO → ¿Es compartido globalmente?
                ├─ SÍ → Singleton (con thread-safety) 
                └─ NO → Revisar diseño 
```

---

##  Tecnologías Utilizadas

- ASP.NET Core 8.0.414
- C# 12
- Swagger/OpenAPI
- Keyed Services (Inyección de dependencias con claves)

## Autor
DRMiguel25

**Repositorio:** https://github.com/DRMiguel25/Tipos-de-Inyecci-n-de-Dependencias-en-.NET.git

---

##  Referencias

- [Microsoft Docs - Dependency Injection](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection)
- [Service Lifetimes](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection#service-lifetimes)
- [Keyed Services](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection#keyed-services)