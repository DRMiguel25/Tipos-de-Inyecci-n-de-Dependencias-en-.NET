------------------------------------------------------------------------------------
-- Estructura del proyecto --

OrdersApi/
├── Program.cs                    # Configuración principal y DI
├── OrdersApi.csproj             # Configuración del proyecto
├── Properties/
│   └── launchSettings.json      # Configuración de ejecución
├── Models/                      #  Modelos de datos
│   └── Order.cs                 # Clase Order
├── Services/                    #  Lógica de negocio
│   ├── IOrderService.cs        # Interfaz del servicio
│   └── OrderService.cs         # Implementación del servicio
└── Controllers/                 #  Controladores API
    └── OrdersController.cs      # Endpoints REST

------------------------------------------------------------------------------------



-- Diseño de la Arquitectura del Sistema --
┌─────────────────────────────────────────────────┐
│              Cliente (Postman/cURL)             │
└───────────────────┬─────────────────────────────┘
                    │ HTTP Requests
                    ▼
┌─────────────────────────────────────────────────┐
│          OrdersController (API Layer)           │
│  ┌───────────┬───────────┬────────────┐         │
│  │ Transient │  Scoped   │ Singleton  │         │
│  └─────┬─────┴─────┬─────┴──────┬─────┘         │
└────────┼───────────┼────────────┼────────────-──┘
         │           │            │
         ▼           ▼            ▼
┌─────────────────────────────────────────────────┐
│         Dependency Injection Container          │
│  ┌───────────┬───────────┬────────────┐         │
│  │ Instance1 │ Instance2 │ Instance3  │         │
│  │  (new)    │ (request) │  (global)  │         │
│  └─────┬─────┴─────┬─────┴──────┬─────┘         │
└────────┼───────────┼────────────┼──────────────-┘
         │           │            │
         ▼           ▼            ▼
┌─────────────────────────────────────────────────┐
│           OrderService (Business Logic)         │
│  ┌─────────────────────────────────────────┐    │ 
│  │ - Guid _instanceId                      │    │
│  │ - List<Order> _orders                   │    │
│  │                                         │    │
│  │ + GetInstanceId()                       │    │
│  │ + AddOrder(Order)                       │    │
│  │ + GetOrders()                           │    │
│  │ + GetOrdersCount()                      │    │ 
│  └─────────────────────────────────────────┘    │
└─────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────┐
│           Order Model (Data Layer)              │
│  ┌─────────────────────────────────────────┐    │
│  │ + int Id                                │    │
│  │ + string NombreProducto                 │    │
│  │ + int Cantidad                          │    │
│  │ + DateTime Fecha                        │    │
│  └─────────────────────────────────────────┘    │
└─────────────────────────────────────────────────┘

------------------------------------------------------------------------------------

-- Flujo de Datos --
Cliente
  │
  │ POST /api/orders/singleton
  │ Body: { "NombreProducto": "Laptop", "Cantidad": 1 }
  │
  ▼
OrdersController.AddSingleton()
  │
  │ _singletonService.AddOrder(order)
  │
  ▼
OrderService (Singleton Instance)
  │
  │ 1. Genera Id autoincremental
  │ 2. Agrega a List<Order>
  │
  ▼
Response: { "Mensaje": "Agregado", "Total": 1 }

------------------------------------------------------------------------------------
-- GET Request (Obtener Pedidos) --
Cliente
  │
  │ GET /api/orders/singleton
  │
  ▼
OrdersController.GetSingleton()
  │
  │ _singletonService.GetInstanceId()
  │ _singletonService.GetOrdersCount()
  │ _singletonService.GetOrders()
  │
  ▼
OrderService (Singleton Instance)
  │
  │ Retorna datos actuales
  │
  ▼
Response: {
  "Ciclo": "Singleton",
  "Instancia": "a1b2c3d4-...",
  "Cantidad": 1,
  "Pedidos": [...]
}

------------------------------------------------------------------------------------
 Endpoints REST API

 Base URL: `https://localhost:7260/api/orders`

| Método | Endpoint | Descripción | Request Body |
|--------|----------|-------------|--------------|
| **GET** | `/transient` | Obtener info Transient | - |
| **POST** | `/transient` | Agregar a Transient | `Order` JSON |
| **GET** | `/scoped` | Obtener info Scoped | - |
| **POST** | `/scoped` | Agregar a Scoped | `Order` JSON |
| **GET** | `/singleton` | Obtener info Singleton | - |
| **POST** | `/singleton` | Agregar a Singleton | `Order` JSON |

------------------------------------------------------------------------------------
-- Modelo de datos Order --
{
  "Id": 1,                           // Auto-generado
  "NombreProducto": "Laptop Dell",   // string
  "Cantidad": 2,                     // int
  "Fecha": "2025-10-21T21:13:00Z"   // DateTime (UTC)
}

------------------------------------------------------------------------------------


--  Configuración de Dependencias --
 // Program.cs - Registro de servicios

builder.Services.AddKeyedTransient<IOrderService, OrderService>("transient");
  ↓
  Crea nueva instancia en CADA inyección

builder.Services.AddKeyedScoped<IOrderService, OrderService>("scoped");
  ↓
  Una instancia por REQUEST HTTP

builder.Services.AddKeyedSingleton<IOrderService, OrderService>("singleton");
  ↓
  Una ÚNICA instancia para toda la app

------------------------------------------------------------------------------------ 
-- Diagrama de ciclo de vida --
TRANSIENT
┌─────────┐  ┌─────────┐  ┌─────────┐
│ Request │  │ Request │  │ Request │
│    1    │  │    2    │  │    3    │
└────┬────┘  └────┬────┘  └────┬────┘
     │            │            │
     ▼            ▼            ▼
┌─────────┐  ┌─────────┐  ┌─────────┐
│Instance │  │Instance │  │Instance │
│   New   │  │   New   │  │   New   │
│  Guid:A │  │  Guid:B │  │  Guid:C │
│ List:[] │  │ List:[] │  │ List:[] │
└─────────┘  └─────────┘  └─────────┘
  Diferente    Diferente    Diferente


SCOPED
┌─────────────────┐  ┌─────────────────┐
│    Request 1    │  │    Request 2    │
│  ┌───┐   ┌───┐  │  │  ┌───┐   ┌───┐  │
│  │GET│   │POST│ │  │  │GET│   │POST│ │
│  └─┬─┘   └─┬─┘  │  │  └─┬─┘   └─┬─┘  │
│    │       │    │  │    │       │    │
│    ▼       ▼    │  │    ▼       ▼    │
│  ┌─────────┐    │  │  ┌─────────┐    │
│  │Instance │    │  │  │Instance │    │
│  │ Guid:A  │    │  │  │ Guid:B  │    │
│  │ List:[1]│    │  │  │ List:[] │    │
│  └─────────┘    │  │  └─────────┘    │
└─────────────────┘  └─────────────────┘
   Mismo en req1       Nuevo en req2


SINGLETON
┌─────────┐  ┌─────────┐  ┌─────────┐
│ Request │  │ Request │  │ Request │
│    1    │  │    2    │  │    3    │
└────┬────┘  └────┬────┘  └────┬────┘
     │            │            │
     └────────┬───┴────────────┘
              ▼
     ┌─────────────────┐
     │    Instance     │
     │    Guid: A      │
     │  List:[1,2,3]   │
     └─────────────────┘
       SIEMPRE el mismo

------------------------------------------------------------------------------------ 
 Tabla de Comportamiento Esperado del responde json

| Ciclo | InstanceId | Lista de Pedidos | Cuándo Usar |
|-------|-----------|------------------|-------------|
| **Transient** | Cambia siempre | Siempre vacía | Validaciones, helpers sin estado |
| **Scoped** | Igual por request | Persiste en request | DbContext, servicios por usuario |
| **Singleton** | Siempre igual | Persiste toda la app | Caché, configuración global |
------------------------------------------------------------------------------------
-- Ejemplo esperado --
{
  "ciclo": "Singleton",
  "instancia": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "cantidad": 3,
  "pedidos": [
    {
      "id": 1,
      "nombreProducto": "Laptop Dell",
      "cantidad": 2,
      "fecha": "2025-10-21T21:13:00Z"
    },
    {
      "id": 2,
      "nombreProducto": "Mouse Logitech",
      "cantidad": 5,
      "fecha": "2025-10-21T21:15:00Z"
    },
    {
      "id": 3,
      "nombreProducto": "Teclado Mecánico",
      "cantidad": 1,
      "fecha": "2025-10-21T21:17:00Z"
    }
  ]
}

------------------------------------------------------------------------------------
