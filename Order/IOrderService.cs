using BirdPlatFormEcommerce.DEntity;
using BirdPlatFormEcommerce.Order.Requests;
using BirdPlatFormEcommerce.Payment;

namespace BirdPlatFormEcommerce.Order
{
    public interface IOrderService
    {
        public Task<List<TbOrder>> CreateOrder(int userId, CreateOrderModel orderModel);

        public Task<TbOrder?> GetOrder(int orderId);

        public Task<string?> PayOrder(TbOrder order, PaymentMethod method);

        public Task<TbOrder> UpdateOrder(TbOrder order);

        public Task<TbOrder?> GetOrderByPaymentId(int paymentId);
        public Task<TbOrder> CompleteOrder(TbOrder order);
        public Task<List<TbOrder>> GetConfirmedOrdersByUser(int userId, int toConfirm);
        public Task<List<TbOrderDetail>> Orderservice(int shopid);
        public Task<List<TbOrder>> GetConfirmedOrdersByShop(int userId,int shopId);
    }
}
