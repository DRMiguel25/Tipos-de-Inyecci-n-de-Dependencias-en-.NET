using OrdersApi.Models;
using OrdersApi.Services;

namespace OrdersApi.Services;

public class OrderService : IOrderService
{
    private readonly List<Order> _orders = new();
    private readonly Guid _instanceId;

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
