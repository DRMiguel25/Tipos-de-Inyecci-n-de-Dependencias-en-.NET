using OrdersApi.Models;

namespace OrdersApi.Services;

public interface IOrderService
{
    Guid GetInstanceId();
    void AddOrder(Order order);
    List<Order> GetOrders();
    int GetOrdersCount();
}
