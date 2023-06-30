using BirdPlatFormEcommerce.DEntity;
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
        private readonly DataswpContext _context;
        private readonly IVnPayService _vnPayService;
        private readonly IMailService _mailService;

        public OrderService(DataswpContext context, IVnPayService vnPayService, IMailService mailService)
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

        private TbOrderDetail CreateOrderItem(TbProduct product, int quantity)
        {
            var orderItem = new TbOrderDetail
            {
                ProductId = product.ProductId,
                Quantity = quantity,
                ProductPrice = product.Price,
                DiscountPrice = (1 - product.DiscountPercent * (decimal)0.01) * product.Price,
                ToConfirm = 1,
                
            };

            orderItem.Total = orderItem.DiscountPrice * quantity;

            return orderItem;
        }


        public async Task<List<TbOrder>> CreateOrder(int userId, CreateOrderModel orderModel)
        {
            // validate user
            var user = await _context.TbUsers.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                throw new Exception("User not found");
            }

            var orders = new List<TbOrder>(); // Lưu trữ danh sách các đơn hàng đã tạo

            // Tạo đơn hàng cho từng sản phẩm
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

                // Kiểm tra xem đã có đơn hàng cho shop tương ứng chưa
                var existingOrder = orders.FirstOrDefault(o => o.ShopId == product.ShopId);

                if (existingOrder != null)
                {
                    // Nếu đã có đơn hàng cho shop này, thêm sản phẩm vào đơn hàng đã có
                    var orderItem = CreateOrderItem(product, quantity);
                    existingOrder.TbOrderDetails.Add(orderItem);
                    existingOrder.TotalPrice += orderItem.Total ?? 0;
                }
                else
                {
                    // Nếu chưa có đơn hàng cho shop này, tạo đơn hàng mới
                    var orderItem = CreateOrderItem(product, quantity);
                    var order = CreateNewOrder(userId, orderModel, orderItem, product.ShopId);
                    orders.Add(order);
                }
            }

            // Lưu các đơn hàng vào cơ sở dữ liệu
            foreach (var order in orders)
            {
                await _context.TbOrders.AddAsync(order);
            }

            await _context.SaveChangesAsync();

            return orders;
        }

        private TbOrder CreateNewOrder(int userId, CreateOrderModel orderModel, TbOrderDetail orderItem, int? shopId)
        {
            var order = new TbOrder()
            {
                UserId = userId,
                Status = false,
                OrderDate = DateTime.Now,
                Note = orderModel.Note,
                AddressId = (int)orderModel.AddressID,
                TbOrderDetails = new List<TbOrderDetail> { orderItem },
                TotalPrice = orderItem.Total ?? 0,
                ShopId = (int)shopId
            };

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
        public async Task<List<TbOrder>> GetConfirmedOrdersByUser(int userId, int toConfirm)
        {

            return await _context.TbOrders
         .Include(order => order.TbOrderDetails)
             .ThenInclude(orderItem => orderItem.Product)
             .ThenInclude(product => product.Shop) // Nạp thông tin bảng Shop vào Product
         .Include(order => order.Payment)
         .Include(order => order.User)
         .Include(addr =>  addr.Address)
         .Where(order => order.UserId == userId && order.TbOrderDetails.Any(item => item.ToConfirm == toConfirm))
         .ToListAsync();
        }

        public Task<List<TbOrderDetail>> Orderservice(int shopid)
        {
            var orders = _context.TbOrderDetails.                         
                Where(o => o.ToConfirm == 2)
                .Select(od => new
                {
                    TbOrderDetail = od,
                    ShopId = _context.TbProducts
                    .Where(p => p.ProductId == od.ProductId)
                    .Select(p => p.ShopId).FirstOrDefault()
                }) 
                .Where(shop => shop.ShopId == shopid)
                .Select(shop => shop.TbOrderDetail).ToListAsync();
            return orders;
        }

        public async Task<List<TbOrder>> GetConfirmedOrdersByShop(int userId, int shopId)
        {
            return await _context.TbOrders
                .Include(order => order.TbOrderDetails)
                    .ThenInclude(orderItem => orderItem.Product)
                    .ThenInclude(product => product.Shop)
                .Include(order => order.Payment)
                .Include(order => order.User)
                .Include(addr => addr.Address)
                .Where(order => order.User.UserId == userId && order.TbOrderDetails.Any(item => item.ToConfirm == 2 && item.Product.Shop.ShopId == shopId))
                .ToListAsync();
        }

    }
}
