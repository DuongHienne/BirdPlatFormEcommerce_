using BirdPlatFormEcommerce.DEntity;
using BirdPlatFormEcommerce.Order.Requests;
using BirdPlatFormEcommerce.Payment;

namespace BirdPlatFormEcommerce.Order
{
    public interface IOrderService
    {
        public Task<List<TbOrder>> CreateOrder(int userId, CreateOrderModel orderModel);

        public Task<TbOrder?> GetOrder(int orderId);
        public Task<List<TbOrder>> GetOrders(List<int> orderIds);
        public Task<string?> PayOrder(TbOrder order, PaymentMethod method);

        public Task<TbOrder> UpdateOrder(TbOrder order);

        public Task<List<TbOrder>> GetOrderByPaymentId(int paymentId);
        public Task<List<TbOrder>> CompleteOrder(List<int> processedOrderIds);
        public Task<List<TbOrder>> GetConfirmedOrdersByUser(int userId, int toConfirm);
        public Task<List<TbOrderDetail>> Orderservice(int shopid);
        public Task<List<TbOrder>> GetConfirmedOrdersByShop(int userId, int shopId);
        public Task<string?> PayOrders(List<int> processedOrderIds, PaymentMethod method);

    }
}
