using BirdPlatFormEcommerce.IEntity;
using BirdPlatFormEcommerce.Helper.Mail;
using BirdPlatFormEcommerce.Order.Requests;
using BirdPlatFormEcommerce.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using BirdPlatFormEcommerce.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Features;

namespace BirdPlatFormEcommerce.Order
{
    public class OrderService : IOrderService
    {
        private readonly SwpContextContext _context;
        private readonly IVnPayService _vnPayService;
        private readonly IMailService _mailService;

        public OrderService(SwpContextContext context, IVnPayService vnPayService, IMailService mailService)
        {
            _context = context;
            _vnPayService = vnPayService;
            _mailService = mailService;
        }
        public async Task<TbOrder> CompleteOrder(TbOrder order)
        {
            // Update product quantity
            var orderDetails = order.TbOrderDetails;

            foreach (var item in orderDetails)
            {
                var product = _context.TbProducts.FirstOrDefault(prod => prod.ProductId == item.ProductId);
                if (product == null)
                {
                    throw new Exception("Product not found");
                }
                if (product.Quantity < item.Quantity)
                {
                    throw new Exception($"Product {product.ProductId} is out of stock");
                }

                product.Quantity -= item.Quantity;
                product.QuantitySold += item.Quantity;

                _context.TbProducts.Update(product);

                // Create profit
                var profit = new TbProfit
                {
                    OrderDetailId = order.OrderId,
                    Total = item.Total,
                    ShopId = (int)product.ShopId,
                    Orderdate = (DateTime)order.OrderDate,
                };

                _context.TbProfits.Update(profit);
            }

            order.Status = true;
            _context.TbOrders.Update(order);

            // save changes
           

            // send email
            string listProductHtml = "";
            foreach (TbOrderDetail item in order.TbOrderDetails)
            {
                item.ToConfirm = 2;

                listProductHtml += $"<li>{item.Product?.Name} - <del>{item.ProductPrice:n0}</del> VND {item.DiscountPrice:n0} VND - x{item.Quantity}</li>";
            }
            var emailBody = $@"<div><h3>THÔNG TIN ĐƠN HÀNG CỦA BẠN </h3> 
                                    <ul>{listProductHtml} </ul>
                                <div>
                                    <span>Tổng tiền: </span> <strong>{order.TotalPrice:n0} VND</strong>
                                </div>
                                <p>Xin trân trọng cảm ơn</p>
                                </div>";

            var mailRequest = new MailRequest()
            {
                ToEmail = order.User.Email ?? string.Empty,
                Subject = "[BIRD TRADING PALTFORM] THANH TOÁN THÀNH CÔNG",
                Body = emailBody
            };
            _context.SaveChanges();
            await _mailService.SendEmailAsync(mailRequest);

            return order;
        }

        public async Task<TbOrder> CreateOrder(int userId, CreateOrderModel orderModel)
        {

            // validate user
            var user = await _context.TbUsers.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                throw new Exception("User not found");
            }
           
            var items = new List<TbOrderDetail>();
            foreach (var requestItem in orderModel.Items)
            {
                var quantity = requestItem.Quantity;
                var productId = requestItem.ProductId;

                var product = await _context.TbProducts.FirstOrDefaultAsync(p => p.ProductId == productId);
                if (product == null)
                {
                    throw new Exception($"Product {productId} not found");
                }

                if (product.Quantity - product.QuantitySold < quantity)
                {
                    throw new Exception($"Product {productId} is out of stock");
                }

                var orderItem = new TbOrderDetail
                {
                    ProductId = productId,
                    Quantity = quantity,
                    ProductPrice = product.Price,
                    DiscountPrice = (1 - product.DiscountPercent * (decimal)0.01) * product.Price,
                    ToConfirm = 1,
                    DateOrder= DateTime.Now,
                };

                orderItem.Total = orderItem.DiscountPrice * quantity;

                items.Add(orderItem);
            }

            var order = new TbOrder()
            {
                UserId = userId,
                Status = false,
                OrderDate = DateTime.Now,
                Note = orderModel.Note,
                AddressId = (int)orderModel.AddressID,
                TbOrderDetails = items,
                TotalPrice = items.Sum(item => item.Total ?? 0)
            };


            await _context.TbOrders.AddAsync(order);
            await _context.SaveChangesAsync();
            int orderId = order.OrderId;

            // Cập nhật addressId trong bảng tb_Address cho orderId tương ứng
            var address = await _context.TbOrders.FirstOrDefaultAsync(a => a.AddressId == orderModel.AddressID);
            if (address != null)
            {
                order.AddressId = address.AddressId;
                await _context.SaveChangesAsync();
            }

            return order;
        }

        public async Task<TbOrder?> GetOrder(int orderId)
        {
            return await _context.TbOrders
                .Include(order => order.TbOrderDetails)
                .ThenInclude(orderItem => orderItem.Product)
                .Include(order => order.Payment)
                .Include(order => order.User)
                .FirstOrDefaultAsync(order => order.OrderId == orderId);
        }

        public async Task<TbOrder?> GetOrderByPaymentId(int paymentId)
        {
            return await _context.TbOrders.Include(order => order.TbOrderDetails)
                            .ThenInclude(orderItem => orderItem.Product)
                            .Include(order => order.Payment)
                            .Include(order => order.User)
                            .FirstOrDefaultAsync(order => order.PaymentId == paymentId);
        }

        public async Task<string?> PayOrder(TbOrder order, PaymentMethod method)
        {

            var payment = new TbPayment()
            {
                UserId = order.UserId,
                PaymentMethod = method.ToString(),
                PaymentDate = DateTime.Now,
                Amount = order.TotalPrice
               
            };

            order.Payment = payment;
            _context.TbOrders.Update(order);
            await _context.SaveChangesAsync();

            var paymentUrl = PaymentMethod.VnPay.Equals(method)
              ? _vnPayService.CreatePaymentUrl(payment)
              : null;

            return paymentUrl;
        }

        public async Task<TbOrder> UpdateOrder(TbOrder order)
        {
            _context.TbOrders.Update(order);
            await _context.SaveChangesAsync();
            return order;
        }
    }
}
