# OrdersApi - Gestión de Pedidos y Ciclo de Vida de Servicios en .NET 8

## Descripción del Proyecto

Desarrollé esta API REST con .NET 8 para demostrar y comprender cómo funcionan los tres ciclos de vida de servicios en ASP.NET Core: **Transient**, **Scoped** y **Singleton**. El proyecto gestiona pedidos en memoria y me permitió observar de manera práctica cómo cada ciclo de vida afecta el comportamiento de la aplicación.

---

## Tecnologías Utilizadas

- **.NET 8 SDK** (ASP.NET Core Web API)
- **C#** como lenguaje de programación
- **Swagger/OpenAPI** para documentación automática
- **Postman** para pruebas de endpoints
- **Zorin OS** (Linux) como entorno de desarrollo

---

## Tabla de Contenidos

1. [Instalación y Ejecución](#instalación-y-ejecución)
2. [Estructura del Proyecto](#estructura-del-proyecto)
3. [Implementación de los Servicios](#implementación-de-los-servicios)
4. [Pruebas y Comportamiento Observado](#pruebas-y-comportamiento-observado)
5. [Escenarios de Uso Real](#escenarios-de-uso-real)
6. [Diagramas del Ciclo de Vida](#diagramas-del-ciclo-de-vida)

---

## Instalación y Ejecución

### Clonar el repositorio:
```bash
git clone https://github.com/DRMiguel25/Tipos-de-Inyecci-n-de-Dependencias-en-.NET.git
cd OrdersApi
```

### Ejecutar el proyecto:
```bash
dotnet run
```

### Acceder a Swagger:
```
http://localhost:5078/swagger
```

---

## Estructura del Proyecto

Organicé el proyecto siguiendo las mejores prácticas de arquitectura limpia:

```
OrdersApi/
├── Controllers/
│   └── OrdersController.cs    # Endpoints REST de la API
├── Models/
│   └── Order.cs               # Modelo de datos del pedido
├── Services/
│   ├── IOrderService.cs       # Interfaz del servicio
│   └── OrderService.cs        # Implementación del servicio
├── Program.cs                 # Configuración e inyección de dependencias
├── OrdersApi.csproj          # Archivo de proyecto
└── README.md                 # Este archivo
```

---

## Implementación de los Servicios

### 1. Modelo Order

Creé la clase `Order` con las propiedades necesarias para representar un pedido:

```csharp
public class Order
{
    public int Id { get; set; }
    public string NombreProducto { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
```

### 2. Interfaz IOrderService

Definí la interfaz con los métodos que necesitaba para gestionar pedidos:

```csharp
public interface IOrderService
{
    Guid GetInstanceId();           // Para identificar la instancia
    void AddOrder(Order order);     // Para agregar pedidos
    List<Order> GetOrders();        // Para obtener todos los pedidos
    int GetOrdersCount();           // Para contar pedidos
}
```

### 3. Implementación OrderService

Implementé el servicio con lógica completa:

```csharp
public class OrderService : IOrderService
{
    private readonly Guid _instanceId;
    private readonly List<Order> _orders = new();

    public OrderService()
    {
        _instanceId = Guid.NewGuid();
    }

    public Guid GetInstanceId() => _instanceId;

    public void AddOrder(Order order)
    {
        order.Id = _orders.Count == 0 ? 1 : _orders.Max(o => o.Id) + 1;
        _orders.Add(order);
    }

    public List<Order> GetOrders() => _orders;
    
    public int GetOrdersCount() => _orders.Count;
}
```

### 4. Registro de Servicios en Program.cs

Registré el mismo servicio tres veces con diferentes ciclos de vida usando Keyed Services:

```csharp
builder.Services.AddKeyedTransient<IOrderService, OrderService>("transient");
builder.Services.AddKeyedScoped<IOrderService, OrderService>("scoped");
builder.Services.AddKeyedSingleton<IOrderService, OrderService>("singleton");
```

### 5. Controlador OrdersController

Inyecté los tres servicios en el controlador usando el atributo `[FromKeyedServices]`:

```csharp
public class OrdersController : ControllerBase
{
    private readonly IOrderService _transientService;
    private readonly IOrderService _scopedService;
    private readonly IOrderService _singletonService;

    public OrdersController(
        [FromKeyedServices("transient")] IOrderService transientService,
        [FromKeyedServices("scoped")] IOrderService scopedService,
        [FromKeyedServices("singleton")] IOrderService singletonService)
    {
        _transientService = transientService;
        _scopedService = scopedService;
        _singletonService = singletonService;
    }
}
```

---

## Pruebas y Comportamiento Observado

### Endpoints Implementados

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| `GET` | `/api/orders/transient` | Obtener información del servicio Transient |
| `POST` | `/api/orders/transient` | Agregar pedido al servicio Transient |
| `GET` | `/api/orders/scoped` | Obtener información del servicio Scoped |
| `POST` | `/api/orders/scoped` | Agregar pedido al servicio Scoped |
| `GET` | `/api/orders/singleton` | Obtener información del servicio Singleton |
| `POST` | `/api/orders/singleton` | Agregar pedido al servicio Singleton |

### Ejemplo de cuerpo para POST:
```json
{
  "NombreProducto": "Laptop Dell",
  "Cantidad": 2
}
```

### Resultados de las Pruebas

Realicé múltiples solicitudes HTTP con Postman y observé lo siguiente:

#### Transient
- **InstanceId:** Cambió en cada solicitud
- **Lista de pedidos:** Siempre vacía, se reinició en cada llamada
- **Conclusión:** Cada vez que solicité el servicio, se creó una nueva instancia completamente independiente

#### Scoped
- **InstanceId:** Se mantuvo igual durante una misma solicitud HTTP
- **Lista de pedidos:** Persistió solo dentro de la misma petición
- **Conclusión:** En APIs REST, como cada llamada es independiente, se comportó similar a Transient

#### Singleton
- **InstanceId:** Siempre fue el mismo durante toda la ejecución de la aplicación
- **Lista de pedidos:** Se mantuvo entre todas las solicitudes
- **Conclusión:** Una única instancia compartida globalmente que conservó todos los pedidos agregados

### Tabla Comparativa de Comportamiento

| Ciclo de Vida | ¿Cambia el InstanceId? | ¿Se mantienen los pedidos? |
|--------------|------------------------|---------------------------|
| **Transient** | Sí, en cada solicitud | No, siempre vacía |
| **Scoped** | No, durante la solicitud | Sí, en la misma petición |
| **Singleton** | No, nunca cambia | Sí, toda la aplicación |

---

## Escenarios de Uso Real

Basándome en mi experiencia con este proyecto, identifico los siguientes escenarios de uso:

### Transient - Nueva instancia cada vez
**Cuándo lo usaría:**
- Servicios de validación de datos
- Helpers o utilidades sin estado
- Conversores de formato (JSON, XML, etc.)
- Servicios que no necesitan mantener información entre llamadas

**Ejemplo real:**
```csharp
builder.Services.AddTransient<IEmailValidator, EmailValidator>();
```

### Scoped - Una instancia por solicitud
**Cuándo lo usaría:**
- Contextos de base de datos (Entity Framework DbContext)
- Servicios que necesitan mantener estado durante una transacción
- Servicios relacionados con el usuario actual en una petición
- Operaciones que requieren consistencia dentro de una solicitud

**Ejemplo real:**
```csharp
builder.Services.AddScoped<AppDbContext>();
builder.Services.AddScoped<IUserService, UserService>();
```

### Singleton - Una instancia global
**Cuándo lo usaría:**
- Sistemas de caché en memoria
- Servicios de logging
- Configuraciones globales de la aplicación
- Servicios que son costosos de crear y son thread-safe
- Contadores o estadísticas globales

**Ejemplo real:**
```csharp
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
builder.Services.AddSingleton<ILogger, AppLogger>();
```

### Consideraciones Importantes

1. **Transient:** No usar para servicios pesados que se llaman frecuentemente (impacto en performance)
2. **Scoped:** Ideal para DbContext porque mantiene el seguimiento de cambios durante la petición
3. **Singleton:** Tener cuidado con estado mutable y concurrencia (thread-safety)

---

## Diagramas del Ciclo de Vida

### 1. Transient: Nueva instancia en cada solicitud

```
[Solicitud 1] → [Crea OrderService] → [GUID: A1B2C3] → [Lista vacía]
                     ↓
              [Se destruye después de usarse]

[Solicitud 2] → [Crea OrderService] → [GUID: X9Y8Z7] → [Lista vacía]
                     ↓
              [Se destruye después de usarse]

[Solicitud 3] → [Crea OrderService] → [GUID: M4N5O6] → [Lista vacía]
                     ↓
              [Se destruye después de usarse]
```

Cada solicitud crea una instancia nueva. No hay estado compartido.

---

### 2. Scoped: Misma instancia por solicitud HTTP

```
[Solicitud 1 - Inicio]
    ↓
[Crea OrderService] → [GUID: A1B2C3]
    ↓
├── GET /scoped → [GUID: A1B2C3, Pedidos: 0]
├── POST /scoped → [Agrega pedido, Pedidos: 1]
└── GET /scoped → [GUID: A1B2C3, Pedidos: 1]
    ↓
[Solicitud 1 - Fin] → [Se destruye la instancia]

[Solicitud 2 - Inicio]
    ↓
[Crea OrderService] → [GUID: X9Y8Z7, Pedidos: 0]
    ↓
[Solicitud 2 - Fin] → [Se destruye la instancia]
```

Misma instancia durante una solicitud. Se destruye al finalizar la petición.

---

### 3. Singleton: Una instancia global

```
[Inicio de la Aplicación]
         ↓
[Crea OrderService UNA VEZ] → [GUID: S7T8U9]
         ↓
    ┌────┴────┐
    │         │
[Solicitud 1] [Solicitud 2] [Solicitud 3]
    │         │         │
    ├─────────┼─────────┤
    │    MISMA INSTANCIA │
    └─────────┴─────────┘
         ↓
[GUID: S7T8U9, Pedidos acumulados: 1, 2, 3...]
         ↓
[Fin de la Aplicación] → [Se destruye]
```

Una única instancia para toda la aplicación. Estado compartido entre todas las solicitudes.

---

### Diagrama Comparativo Visual

```
┌─────────────────────────────────────────────────────────┐
│                  CICLO DE VIDA                          │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  TRANSIENT:    [●] [●] [●] [●]  ← Nueva cada vez       │
│                                                         │
│  SCOPED:       [Request 1: ●] [Request 2: ●]           │
│                                                         │
│  SINGLETON:    [●]  ← Solo una instancia               │
│                ↑                                        │
│                └── Compartida por todos                │
└─────────────────────────────────────────────────────────┘
```

---

## Conclusiones

Este proyecto me permitió comprender cómo funciona la inyección de dependencias en .NET y cuándo usar cada ciclo de vida:

1. **Transient** es útil cuando no necesito mantener estado y el servicio es ligero
2. **Scoped** es perfecto para trabajar con bases de datos usando Entity Framework
3. **Singleton** es poderoso pero requiere cuidado con la concurrencia

La implementación de Keyed Services en .NET 8 facilitó poder comparar los tres ciclos de vida en el mismo controlador.

---

## Licencia

Este proyecto está bajo la licencia MIT.

---

## Autor

**Miguel**  
GitHub: [@DRMiguel25](https://github.com/DRMiguel25)

---

## Referencias

- [Documentación oficial de ASP.NET Core - Dependency Injection](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection)
- [Service Lifetimes en .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection#service-lifetimes)