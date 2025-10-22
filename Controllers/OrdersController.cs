using Microsoft.AspNetCore.Mvc;
using OrdersApi.Models;
using OrdersApi.Services;

namespace OrdersApi.Controllers;

[ApiController]
[Route("api/[controller]")]
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

    // --- Transient ---
    [HttpGet("transient")]
    public ActionResult<object> GetTransient()
    {
        return new
        {
            Ciclo = "Transient",
            Instancia = _transientService.GetInstanceId(),
            Cantidad = _transientService.GetOrdersCount(),
            Pedidos = _transientService.GetOrders()
        };
    }

    [HttpPost("transient")]
    public IActionResult AddTransient([FromBody] Order order)
    {
        _transientService.AddOrder(order);
        return Ok(new { Mensaje = "Agregado a Transient", Total = _transientService.GetOrdersCount() });
    }

    // --- Scoped ---
    [HttpGet("scoped")]
    public ActionResult<object> GetScoped()
    {
        return new
        {
            Ciclo = "Scoped",
            Instancia = _scopedService.GetInstanceId(),
            Cantidad = _scopedService.GetOrdersCount(),
            Pedidos = _scopedService.GetOrders()
        };
    }

    [HttpPost("scoped")]
    public IActionResult AddScoped([FromBody] Order order)
    {
        _scopedService.AddOrder(order);
        return Ok(new { Mensaje = "Agregado a Scoped", Total = _scopedService.GetOrdersCount() });
    }

    // --- Singleton ---
    [HttpGet("singleton")]
    public ActionResult<object> GetSingleton()
    {
        return new
        {
            Ciclo = "Singleton",
            Instancia = _singletonService.GetInstanceId(),
            Cantidad = _singletonService.GetOrdersCount(),
            Pedidos = _singletonService.GetOrders()
        };
    }

    [HttpPost("singleton")]
    public IActionResult AddSingleton([FromBody] Order order)
    {
        _singletonService.AddOrder(order);
        return Ok(new { Mensaje = "Agregado a Singleton", Total = _singletonService.GetOrdersCount() });
    }
}
