using AutoMapper;
using BirdPlatFormEcommerce.NEntity;
using BirdPlatFormEcommerce.Helper.Mail;
using BirdPlatFormEcommerce.Order;
using BirdPlatFormEcommerce.Order.Requests;
using BirdPlatFormEcommerce.Order.Responses;
using BirdPlatFormEcommerce.Payment;
using BirdPlatFormEcommerce.Payment.Requests;
using BirdPlatFormEcommerce.Payment.Responses;
using BirdPlatFormEcommerce.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.X9;
using System.Net;
using System.Numerics;

namespace BirdPlatFormEcommerce.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IMapper _mapper;
        private readonly ILogger<OrderController> _logger;
        private readonly IVnPayService _vnPayService;
        private readonly IConfiguration _configuration;
        private readonly IMailService _mailService;
        private readonly SwpDataBaseContext _context;

        public OrderController(IOrderService orderService, IMapper mapper, ILogger<OrderController> logger, IVnPayService vnPayService, IConfiguration configuration, IMailService mailService, SwpDataBaseContext swp)
        {
            _orderService = orderService;
            _mapper = mapper;
            _logger = logger;
            _vnPayService = vnPayService;
            _configuration = configuration;
            _mailService = mailService;
            _context = swp;
        }

        [HttpPost("Create")]

        public async Task<ActionResult<List<OrderRespponse>>> CreateOrder([FromBody] CreateOrderModel request)
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            var order = await _orderService.CreateOrder(Int32.Parse(userId), request);
            var response = _mapper.Map<List<OrderRespponse>>(order);

            return Ok(response);
        }

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<ActionResult<OrderRespponse>> GetOrder([FromRoute] int id)
        {
            var order = await _orderService.GetOrder(id);
            if (order == null)
            {
                return NotFound();
            }

            var response = _mapper.Map<OrderRespponse>(order);
            return Ok(response);
        }

        [HttpPost("Pay")]
        [Authorize]
        public async Task<ActionResult<PaymentResponse>> CreatePayment([FromBody] PayOrderModel request)
        {
            var orders = await _orderService.GetOrders(request.OrderIds);
            if (orders == null || orders.Any(o => o.Status == true))
            {
                return NotFound("Order(s) not found");
            }
            var processedOrderIds = new List<int>();
            string listProductHtml = "";
            decimal total = 0;

            foreach (var order in orders)
            {
                if (request.Method.ToString() == "Cash")
                {
                    order.ToConfirm = 2;
                }

                if (!processedOrderIds.Contains(order.OrderId))
                {
                    total = (decimal)(total + order.TotalPrice);
                    processedOrderIds.Add(order.OrderId);
                    foreach (TbOrderDetail item in order.TbOrderDetails)
                    {
                        listProductHtml += $"<li>{item.Product?.Name} - <del>{item.ProductPrice:n0}</del> $ {item.DiscountPrice:n0} $ - x{item.Quantity}</li>";

                    }
                }
                if (processedOrderIds.Count == orders.Count)
                {

                    var toEmail = order.User?.Email ?? string.Empty;
                    var fullName = order.Address?.NameRg ?? string.Empty;
                    var toPhone = order.Address?.Phone ?? string.Empty;
                    var address = order.Address?.Address ?? string.Empty;
                    var addressDetail = order.Address?.AddressDetail ?? string.Empty;
                    var emailBody = $@"<div><h3>THÔNG TIN ĐƠN HÀNG CỦA BẠN </h3> 
                        <div>
                            <h3>Thông tin nhận hàng</h3> 
                              <span>Tên người nhận: </span> <strong>{fullName}</strong><br>
                            <span>Số Điện thoại: </span> <strong>{toPhone:n0}</strong><br>
                            <span>Địa Chỉ Nhận hàng: </span> <strong>{addressDetail}, {address}</strong>
                        </div>
                        <ul>{listProductHtml} </ul>
                        <div>
                            <span>Tổng tiền: </span> <strong>{total:n0} VND</strong>
                        </div>
                           
                        <p>Xin trân trọng cảm ơn</p>
                    </div>";

                    var mailRequest = new MailRequest()
                    {
                        ToEmail = order.User.Email ?? string.Empty,
                        Subject = "[BIRD TRADING PLATFORM] XÁC NHẬN ĐƠN HÀNG",
                        Body = emailBody
                    };


                    await _mailService.SendEmailAsync(mailRequest);

                }



                if (processedOrderIds.Count == orders.Count)
                {
                    _context.SaveChanges();
                    var paymentUrl = await _orderService.PayOrders(processedOrderIds, request.Method);
                    var response = _mapper.Map<PaymentResponse>(order.Payment);
                    response.PaymentUrl = paymentUrl;
                    return Ok(response);
                }
            }

            return NotFound("Order(s) not found");
        }
        [HttpGet("PaymentCallback/{paymentId:int}")]
        public async Task<ActionResult> PaymentCallback([FromRoute] int paymentId, [FromQuery] VnPaymentCallbackModel request)
        {
            var orders = await _orderService.GetOrderByPaymentId(paymentId);
            if (!request.Success)
            {
                return Redirect(_configuration["Payment:Failed"]);
            }
            var processedOrderIds = new List<int>();
            foreach (var order in orders)
            {
                processedOrderIds.Add(order.OrderId);
                if (order == null || order.Status == true)
                {
                    return NotFound("Order not found");
                }
            }


            await _orderService.CompleteOrder(processedOrderIds);

            return Redirect(_configuration["Payment:SuccessUrl"]);

        }
        [HttpGet("OrderFailed")]
        public IActionResult GetOrdersByUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(x => x.Type == "UserId");
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var orders = _context.TbOrders
                .Where(o => o.UserId == userId && o.Payment.PaymentMethod == "Vnpay" && o.Status == false)
                .Include(o => o.TbOrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.Shop)
                .Include(o => o.Payment)
                .ToList();

            var response = orders
                .GroupBy(o => o.PaymentId) // Gom nhóm các đơn hàng theo PaymentId
                .Select(g => new
                {
                    PaymentId = g.Key,
                    Amount = _context.TbPayments
                    .Where(p => p.PaymentId == g.Key)
                    .Select(p => p.Amount),
                    Orders = g.Select(o => new
                    {
                        OrderId = o.OrderId,
                        Status = o.Status,
                        UserId = o.UserId,
                        Note = o.Note,
                        TotalPrice = o.TotalPrice,
                        OrderDate = o.OrderDate,
                        ShopId = o.ShopId,
                        ShopName = o.Shop.ShopName,
                        Items = o.TbOrderDetails.Select(od => new
                        {
                            Id = od.Id,
                            ProductId = od.ProductId,
                            ProductName = od.Product.Name,
                            Quantity = od.Quantity,
                            Discount = od.Discount,
                            ProductPrice = od.ProductPrice,
                            DiscountPrice = od.DiscountPrice,
                           
                            Total = od.Total,
                            FirstImagePath = _context.TbImages
                                .Where(d => d.ProductId == od.ProductId)
                                .OrderBy(d => d.SortOrder)
                                .Select(d => d.ImagePath)
                                .FirstOrDefault()
                        })
                    })
                })
                .ToList();

            return Ok(response);
        }

        [HttpGet("ToReceived/{ToConfirm:int}")]
        public IActionResult GetOrdersByToReceived(int ToConfirm)
        {
            var userIdClaim = User.Claims.FirstOrDefault(x => x.Type == "UserId");
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var orders = _context.TbOrders
                .Where(o => o.UserId == userId && o.ToConfirm == ToConfirm && o.ReceivedDate != null)
                .Include(o => o.TbOrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.Shop)
                .Include(o => o.Payment)
                .Include(o => o.Address)
                .ToList();

            var response = orders
                .Select(o => new
                {
                    OrderId = o.OrderId,
                    Status = o.Status,
                    UserId = o.UserId,
                    Note = o.Note,
                    ToConfirm = o.ToConfirm,
                    PaymentMethod = o.Payment.PaymentMethod,
                    TotalPrice = o.TotalPrice,
                    OrderDate = o.OrderDate,
                    ShopId = o.ShopId,
                    ShopName = o.Shop.ShopName,
                    AddressId = o.AddressId,
                    Address = o.Address.Address,
                    AddressDetail = o.Address.AddressDetail,
                    CancelDate=o.CancleDate,
                    ReceivedDate= o.ReceivedDate,
                    Phone = o.Address.Phone,
                    NameRg = o.Address.NameRg,
                    Items = o.TbOrderDetails.Select(od => new
                    {
                        Id = od.Id,
                        ProductId = od.ProductId,
                        OrderDetailID = od.Id,
                        ProductName = od.Product.Name,
                        Quantity = od.Quantity,
                        Discount = od.Discount,
                        ProductPrice = od.ProductPrice,
                        DiscountPrice = od.DiscountPrice,
                        ToFeedback = od.ToFeedback,
                        Total = od.Total,
                        FirstImagePath = _context.TbImages
                                .Where(d => d.ProductId == od.ProductId)
                                .OrderBy(d => d.SortOrder)
                                .Select(d => d.ImagePath)
                                .FirstOrDefault()
                    })

                })
                .ToList();

            return Ok(response);
        }
        [HttpGet("ToConFirmOfuserId/{ToConfirm:int}")]
        public IActionResult GetOrdersByToConfirm(int ToConfirm )
        {
            var userIdClaim = User.Claims.FirstOrDefault(x => x.Type == "UserId");
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);

            var orders = _context.TbOrders
                .Where(o => o.UserId == userId && o.ToConfirm == ToConfirm && o.ReceivedDate== null)
                .Include(o => o.TbOrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.Shop)
                .Include(o => o.Payment)
                .Include(o => o.Address)
                .ToList();

            var response = orders
                .Select(o => new
                {
                    OrderId = o.OrderId,
                    Status = o.Status,
                    UserId = o.UserId,
                    Note = o.Note,
                    ToConfirm = o.ToConfirm,
                    PaymentMethod = o.Payment.PaymentMethod,
                    TotalPrice = o.TotalPrice,
                    OrderDate = o.OrderDate,
                    ShopId = o.ShopId,
                    ShopName = o.Shop.ShopName,
                    AddressId = o.AddressId,
                    Address = o.Address.Address,
                    AddressDetail = o.Address.AddressDetail,
                    Phone = o.Address.Phone,
                    NameRg = o.Address.NameRg,
                    Items = o.TbOrderDetails.Select(od => new
                    {
                        Id = od.Id,
                        ProductId = od.ProductId,
                        ProductName = od.Product.Name,
                        Quantity = od.Quantity,
                        Discount = od.Discount,
                        ProductPrice = od.ProductPrice,
                        DiscountPrice = od.DiscountPrice,
                        Total = od.Total,
                        FirstImagePath = _context.TbImages
                                .Where(d => d.ProductId == od.ProductId)
                                .OrderBy(d => d.SortOrder)
                                .Select(d => d.ImagePath)
                                .FirstOrDefault()
                    })

                })
                .ToList();

            return Ok(response);
        }
        [HttpPost("AddressOder")]
        public async Task<IActionResult> AddressOder(AddressModel add)
        {
            var useridClaim = User.Claims.FirstOrDefault(u => u.Type == "UserId");
            if (useridClaim == null)
            {
                return Unauthorized();
            }
            int userid = int.Parse(useridClaim.Value);
            var address = new TbAddressReceive
            {
                UserId = userid,
                Address = add.Address,
                AddressDetail = add.AddressDetail,
                Phone = add.Phone,
                NameRg = add.NameRg


            };
            await _context.TbAddressReceives.AddAsync(address);
            await _context.SaveChangesAsync();
            return Ok(address);
        }
        [HttpGet("GetAddressOder")]
        public async Task<IActionResult> GetAddressOder()
        {
            var useridClaim = User.Claims.FirstOrDefault(x => x.Type == "UserId");
            if (useridClaim == null) return Unauthorized();
            int userid = int.Parse(useridClaim.Value);
            var address = _context.TbAddressReceives.Where(a => a.UserId == userid).ToList();
            return Ok(address);
        }
        [HttpGet("confirmed")]
        public async Task<ActionResult<List<OrderResult>>> GetConfirmedOrdersByUser(int toConfirm)
        {

            var useridClaim = User.Claims.FirstOrDefault(u => u.Type == "UserId");
            if (useridClaim == null)
            {
                return Unauthorized();
            }
            int userid = int.Parse(useridClaim.Value);

            var orders = await _orderService.GetConfirmedOrdersByUser(userid, toConfirm);

            List<OrderResult> orderResults = new List<OrderResult>();

            foreach (var order in orders)
            {
                var group = order.TbOrderDetails
                    .Where(d => d.ToConfirm == toConfirm) // Lọc chỉ những OrderDetail có ToConfirm=2
                    .GroupBy(d => new
                    {
                        d.Product.Shop.ShopId,
                        d.Order.Payment.PaymentMethod,
                        d.ProductId,
                        d.Order.Note,
                        DateOrder = d.DateOrder.Value,
                        d.Product.Shop.ShopName,
                        d.Total,
                        d.Order.AddressId,
                        d.Order.Address.Address,
                        d.Order.Address.AddressDetail,
                        d.Order.Address.Phone,
                        d.Order.Address.NameRg

                    })
                    .Select(g => new ShopOrder
                    {
                        ShopID = g.Key.ShopId,
                        PaymentMethod = g.Key.PaymentMethod,
                        ShopName = g.Key.ShopName,
                        DateOrder = (DateTime)g.Key.DateOrder,
                        Note = g.Key.Note,
                        AddressId = g.Key.AddressId,
                        Address = g.Key.Address,
                        AddressDetail = g.Key.AddressDetail,
                        Phone = g.Key.Phone,
                        NameRg = g.Key.NameRg,
                        Items = g.Select(d => new OrderItem
                        {
                            Id = d.Id,
                            ProductId = d.ProductId,
                            ProductName = d.Product.Name,
                            Quantity = (int)d.Quantity,
                            ProductPrice = (decimal)d.ProductPrice,
                            DiscountPrice = (decimal)d.DiscountPrice,
                            Total = (decimal)d.Total,
                            FirstImagePath = _context.TbImages
                                .Where(i => i.ProductId == d.ProductId)
                                .OrderBy(i => i.SortOrder)
                                .Select(i => i.ImagePath)
                                .FirstOrDefault()
                        }).ToList()
                    })
                    .GroupBy(s => s.ShopID)
                    .Select(g => new ShopOrder
                    {
                        ShopID = g.Key,
                        PaymentMethod = g.First().PaymentMethod,
                        ShopName = g.First().ShopName,
                        DateOrder = g.First().DateOrder,
                        Note = g.First().Note,
                        Items = g.SelectMany(s => s.Items).ToList()
                    })
                    .ToList();

                orderResults.Add(new OrderResult
                {
                    OrderID = order.OrderId,
                    Shops = group
                });
            }

            return orderResults;
        }
        [HttpGet("getoderofuser")]
        public async Task<ActionResult<List<OrderResult>>> GetConfirmedOrdersByShop()
        {
            var useridClaim = User.Claims.FirstOrDefault(u => u.Type == "UserId");
            if (useridClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(useridClaim.Value);
            var shop = await _context.TbShops.FirstOrDefaultAsync(x => x.UserId == userId);
            if (shop == null) return BadRequest("No shop");
            int shopid = shop.ShopId;
            var orders = await _orderService.GetConfirmedOrdersByShop(userId, shopid);

            List<OrderResult> orderResults = new List<OrderResult>();

            foreach (var order in orders)
            {
                var group = order.TbOrderDetails
                    .Where(d => d.ToConfirm == 2)
                    .GroupBy(d => new
                    {
                        d.Product.Shop.ShopId,
                        d.Order.Payment.PaymentMethod,
                        d.ProductId,
                        d.Order.Note,
                        DateOrder = d.DateOrder.Value,
                        d.Product.Shop.ShopName,
                        d.Total,
                        d.Order.AddressId,
                        d.Order.Address.Address,
                        d.Order.Address.AddressDetail,
                        d.Order.Address.Phone,
                        d.Order.Address.NameRg

                    })
                    .Select(g => new ShopOrder
                    {
                        ShopID = g.Key.ShopId,
                        PaymentMethod = g.Key.PaymentMethod,
                        ShopName = g.Key.ShopName,
                        DateOrder = (DateTime)g.Key.DateOrder,
                        Note = g.Key.Note,
                        AddressId = g.Key.AddressId,
                        Address = g.Key.Address,
                        AddressDetail = g.Key.AddressDetail,
                        Phone = g.Key.Phone,
                        NameRg = g.Key.NameRg,
                        Items = g.Select(d => new OrderItem
                        {
                            Id = d.Id,
                            ProductId = d.ProductId,
                            ProductName = d.Product.Name,
                            Quantity = (int)d.Quantity,
                            ProductPrice = (decimal)d.ProductPrice,
                            DiscountPrice = (decimal)d.DiscountPrice,
                            Total = (decimal)d.Total,
                            FirstImagePath = _context.TbImages
                                .Where(i => i.ProductId == d.ProductId)
                                .OrderBy(i => i.SortOrder)
                                .Select(i => i.ImagePath)
                                .FirstOrDefault()
                        }).ToList()
                    })
                    .GroupBy(s => s.ShopID)
                    .Select(g => new ShopOrder
                    {
                        ShopID = g.Key,
                        PaymentMethod = g.First().PaymentMethod,
                        ShopName = g.First().ShopName,
                        DateOrder = g.First().DateOrder,
                        Note = g.First().Note,
                        Items = g.SelectMany(s => s.Items).ToList()
                    })
                    .ToList();

                orderResults.Add(new OrderResult
                {
                    OrderID = order.OrderId,
                    Shops = group
                });
            }

            return orderResults;
        }

    }
}


