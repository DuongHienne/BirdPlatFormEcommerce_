using BirdPlatForm.UserRespon;
using BirdPlatFormEcommerce.NEntity;
using BirdPlatFormEcommerce.Product;
using BirdPlatFormEcommerce.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using System.Linq;
using MimeKit.Cryptography;
using BirdPlatFormEcommerce.Helper;
using Microsoft.AspNetCore.Mvc.Filters;
using static System.Net.Mime.MediaTypeNames;
using System.Globalization;
using BirdPlatFormEcommerce.Order;
using MailKit.Net.Imap;
using System.ComponentModel;
using BirdPlatFormEcommerce.Order.Responses;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Win32;
using BirdPlatFormEcommerce.Helper.Mail;

namespace BirdPlatFormEcommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShopController : ControllerBase
    {
        private readonly SwpDataBaseContext _context;
        private readonly IManageOrderService _manageOrderService;
        private readonly IWebHostEnvironment _enviroment;
        private readonly IOrderService _oderService;
        private readonly IMailService _mailService;

        public ShopController(SwpDataBaseContext swp, IMailService mailService, IManageOrderService manageOrderService, IWebHostEnvironment enviroment, IOrderService orderService)
        {
            _context = swp;
            _manageOrderService = manageOrderService;
            _enviroment = enviroment;
            _oderService = orderService;
            _mailService = mailService;
        }
        [HttpPost("registerShop")]

        public async Task<IActionResult> RegisterShop(ShopModel shopmodel)
        {
            int userId = getuserIDfromtoken();
            var isShop = await _context.TbShops.FirstOrDefaultAsync(s => s.UserId == userId);
            if (isShop != null)
            {
                return Ok(new ErrorRespon
                {
                    Error = false,
                    Message = "UserId have to shop"
                });
            }
            var shop = new TbShop
            {
                ShopName = shopmodel.shopName,
                Address = shopmodel.Address,
                Phone = shopmodel.Phone,
                AddressDetail = shopmodel.AddressDetail,
                UserId = userId,
                CreateDate =DateTime.Now,


            };
            _context.TbShops.Add(shop);
            await _context.SaveChangesAsync();
            var user = await _context.TbUsers.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user != null)
            {
                user.RoleId = "SP";
                user.IsShop = true;
                await _context.SaveChangesAsync();
            }
            string email = user.Email;
            var mailRequest = new MailRequest()
            {
                ToEmail = email,
                Subject = "[BIRD TRADING PLATFORM] Thông tin đăng kí của bạn",
                Body = $"Thông tin đăng kí Shop của bạn:" +
              $"-ShopName : {shop.ShopName}" +
              $"  -Address : {shop.Address}" +
              $"   -Phone :{shop.Phone}    Hãy đăng nhập và bán hàng"
            };

            await _mailService.SendEmailAsync(mailRequest);
            return Ok(new ErrorRespon
            {

                Message = "Đăng kí shop thành công , hãy check email của bạn ",
                RoleId = user.RoleId
            });
        }

        private int getuserIDfromtoken()
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader != null && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                var accountIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (accountIdClaim != null && int.TryParse(accountIdClaim.Value, out int UserId))
                {
                    return UserId;
                }
            }
            throw new InvalidOperationException("Invalid token or missing accountId claim.");
        }
        [HttpPut("UpdateShop/{shopId:int}")]
        public async Task<IActionResult> UpdateShop(int shopid,ShopModel model)
        {
            var shop = _context.TbShops.Find(shopid);
            if(shop != null)
            {
                shop.ShopId = shopid;
                shop.ShopName = model.shopName;
                shop.Description = model.Description;
                shop.Address = model.Address;
                shop.AddressDetail = model.AddressDetail;
                shop.Phone = model.Phone;
                _context.TbShops.Update(shop);
                _context.SaveChanges();
            }
        
            return Ok("Update shop success");

        }

        [HttpGet]
        public async Task<IActionResult> getMyshop()
        {
            var myshop = User.Claims.FirstOrDefault(u => u.Type == "UserId");
            if (myshop == null)
            {
                return NotFound();
            }
            int userid = int.Parse(myshop.Value);
            var shop = _context.TbShops.FirstOrDefault(x => x.UserId == userid);
            if (shop == null)
            {
                return NotFound();
            }
            var isshop = new ViewShop
            {
                ShopId=(int)shop.ShopId,
                Rate = shop.Rate ?? 0,
                shopName = shop.ShopName,
                Address = shop.Address,
                AddressDetail = shop.AddressDetail, 

                phone = shop.Phone,
                Description = shop.Description ?? null

            };
            return Ok(isshop);
        }
        [HttpGet("getproductshop")]
        public async Task<List<HomeViewProductModel>> getProductShop()
        {
            var userIdclaim = User.Claims.FirstOrDefault(u => u.Type == "UserId");
            if (userIdclaim == null)
            {
                throw new Exception("User not found");
            }
            int userid = int.Parse(userIdclaim.Value);
            var shop = await _context.TbShops.FirstOrDefaultAsync(s => s.UserId == userid);
            if (shop == null)
            {
                throw new Exception("Shop not found");
            }
            int shopid = shop.ShopId;


            var query = from p in _context.TbProducts
                        join s in _context.TbShops on p.ShopId equals s.ShopId
                        join c in _context.TbProductCategories on p.CateId equals c.CateId
                        join img in _context.TbImages on p.ProductId equals img.ProductId into images
                        where p.ShopId == shopid && p.IsDelete == true && p.Status == true
                        select new { p, c, s, Image = images.FirstOrDefault() };

            var data = await query.Select(x => new HomeViewProductModel()
            {
                ProductId = x.p.ProductId,
                ProductName = x.p.Name,
                CateName = x.c.CateName,
                Status = x.p.Status,
                Price = x.p.Price,
                DiscountPercent = x.p.DiscountPercent,
                SoldPrice = (int)Math.Round((decimal)(x.p.Price - x.p.Price / 100 * (x.p.DiscountPercent))),
                QuantitySold = x.p.QuantitySold,
                Rate = x.p.Rate,
                Thumbnail = x.Image != null ? x.Image.ImagePath : "no-image.jpg",
            }).ToListAsync();

            return data;
        }


        [HttpPost]
        [Route("Add_Product")]
        public async Task<IActionResult> AddProduct([FromForm] CreateProductViewModel request)
        {
            try
            {
                //lay shopid dang login
                var userIdClaim = User.Claims.FirstOrDefault(u => u.Type == "UserId");
                if (userIdClaim == null)
                {
                    throw new Exception("User not found");
                }
                int userid = int.Parse(userIdClaim.Value);
                var shop = await _context.TbShops.FirstOrDefaultAsync(s => s.UserId == userid);
                if (shop == null)
                {
                    throw new Exception("Shop not found");
                }
                int shopid = shop.ShopId;

                //add thong tin co ban cua product
                var product = new TbProduct()
                {
                    Name = request.ProductName,

                    Price = request.Price,
                    DiscountPercent = (decimal?)request.DiscountPercent,
                    SoldPrice = (int)Math.Round((decimal)(request.Price - request.Price / 100 * (request.DiscountPercent))),
                    Decription = request.Decription,
                    Status = true,
                    //      CreateDate = request.CreateDate,
                    Quantity = request.Quantity,
                    // ShopId = request.ShopId,
                    ShopId = shopid,
                    CateId = request.CateId
                };

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                _context.TbProducts.Add(product);
                await _context.SaveChangesAsync();


                //add image
                APIResponse response = new APIResponse();
                int passcount = 0;
                int errorcount = 0;
                int maxImageCount = 6;
                try
                {

                    string Filepath = GetFileProductPath(product.ProductId);
                    if (!Directory.Exists(Filepath))
                    {
                        Directory.CreateDirectory(Filepath);
                    }
                    int imageCount = 0;
                    if (request.ImageFile.Length > 0)
                    {
                        foreach (var file in request.ImageFile)
                        {
                            if (imageCount >= maxImageCount)
                            {
                                break;
                            }

                            var image = new TbImage()
                            {

                                Caption = "Image",
                                CreateDate = DateTime.Now,

                                ProductId = product.ProductId,

                                ImagePath = GetImageProductPath(product.ProductId, file.FileName),



                            };
                            string imagepath = Path.Combine(Filepath, file.FileName);
                            if (System.IO.File.Exists(imagepath))
                            {
                                System.IO.File.Delete(imagepath);
                            }
                            using (FileStream stream = System.IO.File.Create(imagepath))
                            {
                                await file.CopyToAsync(stream);
                                passcount++;
                            }
                            _context.Add(image);
                            imageCount++;

                        }
                    }
                    await _context.SaveChangesAsync();

                }
                catch (Exception ex)
                {
                    errorcount++;
                    response.Errormessage = ex.Message;
                }

                response.ResponseCode = 200;
                response.Result = passcount + "File uploaded &" + errorcount + "File failed";
                return Ok(response);

            }
            catch
            {
                return BadRequest("Cannot Add check Again");
            }

        }

        [HttpPut("Update_Product")]
        public async Task<IActionResult> UpdateProduct([FromForm] UpdateProductViewModel request)
        {
            try
            {
                var userIdClaim = User.Claims.FirstOrDefault(u => u.Type == "UserId");
                if (userIdClaim == null)
                {
                    throw new Exception("User not found");
                }
                int userid = int.Parse(userIdClaim.Value);
                var shop = await _context.TbShops.FirstOrDefaultAsync(s => s.UserId == userid);
                if (shop == null)
                {
                    throw new Exception("Shop not found");
                }
                int shopid = shop.ShopId;

                var product = await _context.TbProducts.FindAsync(request.ProductId);

                if (product == null) throw new Exception("Can not found.");
                product.Name = request.Name;
                product.Price = request.Price;
                product.DiscountPercent = request.DiscountPercent;
                product.SoldPrice = (int)Math.Round((decimal)(product.Price - request.Price / 100 * (request.DiscountPercent)));

                product.Decription = request.Decription;
                //          product.Detail = request.Detail;
                //  product.ShopId= request.ShopId;
                product.ShopId = shopid;
                await _context.SaveChangesAsync();


                //delete Image 

                if (request.ImageFile == null || request.ImageFile.Length == 0)
                {
                    await _context.SaveChangesAsync();

                    return Ok("Add product successfully");
                }
                else
                {

                    string Filepath = GetFileProductPath(product.ProductId);
                    if (System.IO.Directory.Exists(Filepath))
                    {
                        DirectoryInfo directoryInfo = new DirectoryInfo(Filepath);
                        FileInfo[] fileInfos = directoryInfo.GetFiles();
                        foreach (FileInfo fileInfo in fileInfos)
                        {
                            fileInfo.Delete();
                        }

                    }


                    var imagesToRemove = await _context.TbImages
              .Where(i => i.ProductId == product.ProductId)
              .ToListAsync();

                    _context.TbImages.RemoveRange(imagesToRemove);
                    await _context.SaveChangesAsync();


                    //                return Ok("pass");

                    //add new image
                    APIResponse response = new APIResponse();
                    int passcount = 0;
                    int errorcount = 0;
                    int maxImageCount = 6;
                    try
                    {

                        string Filepath1 = GetFileProductPath(product.ProductId);
                        if (!Directory.Exists(Filepath1))
                        {
                            Directory.CreateDirectory(Filepath1);
                        }
                        int imageCount = 0;

                        foreach (var file in request.ImageFile)
                        {
                            if (imageCount >= maxImageCount)
                            {
                                break;
                            }

                            var image = new TbImage()
                            {

                                Caption = "Image",
                                CreateDate = DateTime.Now,

                                ProductId = product.ProductId,

                                ImagePath = GetImageProductPath(product.ProductId, file.FileName),



                            };
                            string imagepath = Path.Combine(Filepath, file.FileName);
                            if (System.IO.File.Exists(imagepath))
                            {
                                System.IO.File.Delete(imagepath);
                            }
                            using (FileStream stream = System.IO.File.Create(imagepath))
                            {
                                await file.CopyToAsync(stream);
                                passcount++;
                            }
                            _context.Add(image);
                            imageCount++;

                        }

                        await _context.SaveChangesAsync();

                    }
                    catch (Exception ex)
                    {
                        errorcount++;
                        response.Errormessage = ex.Message;
                    }

                    response.ResponseCode = 200;
                    response.Result = passcount + "File uploaded &" + errorcount + "File failed";
                    return Ok(response);


                }
            }


            catch
            {
                return BadRequest("CanNot update check again");
            }
        }


        [HttpDelete("Delete_Product")]
        public async Task<IActionResult> DeleteProduct(int productId)
        {

            var userIdClaim = User.Claims.FirstOrDefault(u => u.Type == "UserId");
            if (userIdClaim == null)
            {
                return BadRequest("Can not find User");
            }
            int userId = int.Parse(userIdClaim.Value);
            var shop = await _context.TbShops.FirstOrDefaultAsync(x => x.UserId == userId);
            //       var pro = await _context.TbProducts.FirstOrDefaultAsync(x => x.ShopId == shop.ShopId);
            if (shop == null)
            {
                throw new Exception("Shop not found");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            var product = await _context.TbProducts.FindAsync(productId);
            if (product == null) throw new Exception("Can not find product.");


            product.IsDelete = false;

            await _context.SaveChangesAsync();
            return Ok("Delete Product Success");

        }


        private string GetFileProductPath(int productId)
        {
            return this._enviroment.WebRootPath + "\\user-content\\product\\" + productId.ToString();
        }


        private string GetImageProductPath(int productId, string fileName)
        {
            string hosturl = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}";
            return hosturl + "/user-content/product/" + productId + "/" + fileName;

        }


        [HttpGet("AllProduct/shop")]
        public async Task<IActionResult> GetallProduct()
        {
            var userid = User.Claims.FirstOrDefault(x => x.Type == "UserId");
            if (userid == null)
            {
                return Unauthorized();
            }
            int user = int.Parse(userid.Value);
            var product = await _context.TbProducts.CountAsync(a => a.ShopId == user);
            if (product == null) return BadRequest("No Product");
            return Ok(product);
        }



        [HttpGet("Detail_Product")]
        public async Task<IActionResult> GetProductById(int productId)
        {
            var userIdclaim = User.Claims.FirstOrDefault(u => u.Type == "UserId");
            if (userIdclaim == null)
            {
                throw new Exception("User not found");
            }
            int userid = int.Parse(userIdclaim.Value);
            var shop = await _context.TbShops.FirstOrDefaultAsync(s => s.UserId == userid);
            if (shop == null)
            {
                throw new Exception("Shop not found");
            }
            int shopid = shop.ShopId;
            //find product by ProductId
            var product = await _context.TbProducts.FindAsync(productId);
            var image = await _context.TbImages.Where(x => x.ProductId == productId).Select(x => x.ImagePath).ToArrayAsync();

            var cate = await (from c in _context.TbProductCategories
                              join p in _context.TbProducts on c.CateId equals p.CateId
                              where p.ProductId == productId && p.IsDelete == true && p.Status == true
                              select c).FirstOrDefaultAsync();


            var shopManagementProductDetailVM = new ShopManagementProductDetailVM()
            {
                ProductId = productId,
                ProductName = product.Name,
                Price = product.Price,
                DiscountPercent = (int)product.DiscountPercent,
                SoldPrice = (int)Math.Round((decimal)(product.Price - product.Price / 100 * product.DiscountPercent)),
                Decription = product != null ? product.Decription : null,

                Quantity = product.Quantity,
                ShopId = shopid,


                CateId = product.CateId,



                Images = image.Length > 0 ? image.ToList() : new List<string> { "no-image.jpg" },


            };
            return Ok(shopManagementProductDetailVM);
        }


        [HttpGet("Revenue_month")]
        public async Task<IActionResult> GetRevenueMonth()
        {

            var userIdClaim = User.Claims.FirstOrDefault(u => u.Type == "UserId");
            if (userIdClaim == null)
            {
                throw new Exception("User not found");
            }
            int userid = int.Parse(userIdClaim.Value);
            var shop = await _context.TbShops.FirstOrDefaultAsync(s => s.UserId == userid);
            if (shop == null)
            {
                throw new Exception("Shop not found");
            }
            int shopid = shop.ShopId;

            int currentYear = DateTime.Now.Year;
            var query = await _context.TbOrders.Where(x => x.ShopId == shopid && x.ToConfirm == 3).Select(p => new
            {
                ShopId = shopid,
                Orderdate = (DateTime)p.OrderDate,
                OrderId = p.OrderId,
                TotalPrice = (decimal?)p.TotalPrice
            }).ToListAsync();



            // Khoi tao mang chua kq TotalRevenue của moi thang
            decimal[] monthlyRevenue = new decimal[12];

            for (int i = 0; i < 12; i++)
            {
                DateTime currentMonthStart = new DateTime(currentYear, i + 1, 1);
                DateTime currentMonthEnd = currentMonthStart.AddMonths(1).AddDays(-1);




                // Tính tổng doanh thu của shop trong tháng hiện tại
                decimal totalRevenue = query.Where(p => p.Orderdate >= currentMonthStart && p.Orderdate <= currentMonthEnd).Sum(p => p.TotalPrice ?? 0m);

                // Gán đối tượng tháng vào mảng monthlyRevenue
                monthlyRevenue[i] = totalRevenue;
            }

            return Ok(monthlyRevenue);
        }



        [HttpGet("ToTal_Revenue")]
        public async Task<IActionResult> GetToTalRevenue()
        {

            var userIdClaim = User.Claims.FirstOrDefault(u => u.Type == "UserId");
            if (userIdClaim == null)
            {
                throw new Exception("User not found");
            }
            int userid = int.Parse(userIdClaim.Value);
            var shop = await _context.TbShops.FirstOrDefaultAsync(s => s.UserId == userid);
            if (shop == null)
            {
                throw new Exception("Shop not found");
            }
            int shopid = shop.ShopId;


            var query = await _context.TbOrders.Where(x => x.ShopId == shopid && x.ToConfirm == 3).Select(p => new
            {
                ShopId = shopid,
                Orderdate = (DateTime)p.OrderDate,
                OrderId = p.OrderId,
                TotalPrice = (decimal?)p.TotalPrice
            }).ToListAsync();



            // Tính tổng doanh thu của shop trong tháng hiện tại
            decimal totalRevenue = query.Sum(p => p.TotalPrice ?? 0m);





            return Ok(totalRevenue);
        }

        [HttpGet("Revenue_week")]
        public async Task<IActionResult> GetRevenueWeek()
        {
            //lay shopid theo userid dang login
            var userIdClaim = User.Claims.FirstOrDefault(u => u.Type == "UserId");
            if (userIdClaim == null)
            {
                throw new Exception("User not found");
            }
            int userid = int.Parse(userIdClaim.Value);
            var shop = await _context.TbShops.FirstOrDefaultAsync(s => s.UserId == userid);
            if (shop == null)
            {
                throw new Exception("Shop not found");
            }
            int shopid = shop.ShopId;

            DateTime today = DateTime.Today;
            DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
            Calendar cal = dfi.Calendar;

            int currentWeek = cal.GetWeekOfYear(today, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);
            int currentYear = today.Year;

            var query = await _context.TbOrders.Where(x => x.ShopId == shopid && x.ToConfirm == 3).Select(p => new
            {
                ShopId = shopid,
                Orderdate = (DateTime)p.OrderDate,
                OrderId = p.OrderId,
                TotalPrice = (decimal?)p.TotalPrice
            }).ToListAsync();



            // Khởi tạo mảng chứa kết quả TotalRevenue của mỗi ngày trong tuần
            decimal[] dailyRevenue = new decimal[7];
            string[] weekdays = new string[7];

            for (int i = 0; i < 7; i++)
            {
                DateTime currentDate = FirstDateOfWeek(currentYear, currentWeek).AddDays(i);

                // Tính tổng doanh thu của shop trong ngày hiện tại
                decimal totalRevenue = query.Where(p => p.Orderdate.Date == currentDate.Date).Sum(p => p.TotalPrice ?? 0m);

                // Gán giá trị tổng doanh thu vào mảng dailyRevenue
                dailyRevenue[i] = totalRevenue;
                weekdays[i] = currentDate.ToString();
            }
            RevenueData revenueData = new RevenueData
            {
                DailyRevenue = dailyRevenue,
                Weekdays = weekdays
            };


            return Ok(revenueData);
        }

        // Hàm để lấy ngày đầu tiên của tuần dựa trên số tuần và năm
        public static DateTime FirstDateOfWeek(int year, int week)
        {
            DateTime jan1 = new DateTime(year, 1, 1);
            int daysToFirstDayOfWeek = (int)jan1.DayOfWeek - (int)CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;

            if (daysToFirstDayOfWeek < 0)
            {
                daysToFirstDayOfWeek += 7;
            }
            int firstWeekDay = 7 * (week - 1) - daysToFirstDayOfWeek;
            return jan1.AddDays(firstWeekDay);
        }

        [HttpGet("orders")]
        public async Task<ActionResult<List<OrderInfo>>> GetOrdersByShopId()
        {
            var userIdclaim = User.Claims.FirstOrDefault(u => u.Type == "UserId");
            if (userIdclaim == null)
            {
                return null;
            }
            int userid = int.Parse(userIdclaim.Value);
            var shop = await _context.TbShops.FirstOrDefaultAsync(s => s.UserId == userid);
            if (shop == null)
                return null;
            int shopid = shop.ShopId;

            var query = await (from o in _context.TbOrders
                               join pay in _context.TbPayments on o.PaymentId equals pay.PaymentId
                               join ad in _context.TbAddressReceives on o.AddressId equals ad.AddressId
                               where o.ShopId == shopid && o.ToConfirm > 1
                               select new OrderInfo
                               {
                                   orderId = o.OrderId,
                                   OrderDate = (DateTime)o.OrderDate,
                                   UserName = o.User.Name,
                                   Email = o.User.Email,
                                   Status = (bool)o.Status,
                                   TotalPrice = (decimal?)o.TotalPrice,
                                   ToConfirm = o.ToConfirm,
                                   PaymentDate = (DateTime)pay.PaymentDate,
                                   //   Address = ad.Address,
                                   //   PaymentMethod = pay.PaymentMethod

                               }).ToListAsync();

            return Ok(query);


        }


        [HttpGet("Detail_Order")]
        public async Task<IActionResult> getDetailOrder(int orderId)
        {


            var userIdclaim = User.Claims.FirstOrDefault(u => u.Type == "UserId");
            if (userIdclaim == null)
            {
                return null;
            }
            int userid = int.Parse(userIdclaim.Value);
            var shop = await _context.TbShops.FirstOrDefaultAsync(s => s.UserId == userid);
            if (shop == null)
                return null;
            int shopid = shop.ShopId;

            var order = await _context.TbProducts.FindAsync(orderId);

            var product = await (from odt in _context.TbOrderDetails
                                 join p in _context.TbProducts on odt.ProductId equals p.ProductId
                                 join ig in _context.TbImages on p.ProductId equals ig.ProductId into images
                                 where odt.OrderId == orderId
                                 select new
                                 {
                                     p,
                                     odt,
                                     Image = images.FirstOrDefault()
                                 }).ToArrayAsync();


            var query = await (from o in _context.TbOrders

                               join ad in _context.TbAddressReceives on o.AddressId equals ad.AddressId
                               join pay in _context.TbPayments on o.PaymentId equals pay.PaymentId
                               join u in _context.TbUsers on o.UserId equals u.UserId
                               where o.OrderId == orderId && o.ShopId == shopid
                               select new { ad, pay, u, o }).FirstOrDefaultAsync();


            var oderDetailInfo = new OrderDetailInfo()

            {
                UserName = query.u.Name,
                Email = query.u.Email,

                Address = query.ad.Address,
                AddressDetail = query.ad.AddressDetail,
                Phone = query.ad.Phone,
                NameRg = query.ad.NameRg,
                PaymentDate = query.pay.PaymentDate,
                PaymentMethod = query.pay.PaymentMethod,
                Status = (bool)query.o.Status,

                DateOrder = (DateTime?)query.o.OrderDate,
                ConfirmDate = (DateTime?)query.o.ConfirmDate,
                CancleDate = (DateTime?)query.o.CancleDate,
                OrderId = orderId,
                ToConfirm = query.o.ToConfirm,

                TotalAll = (decimal?)query.o.TotalPrice,
                ProductDetails = product.Select(x => new ProductDetail
                {
                    NameProduct = x.p.Name,
                    SoldPrice = (decimal?)x.odt.ProductPrice,
                    DiscountPrice = (decimal?)x.odt.DiscountPrice,
                    ImagePath = x.Image != null ? x.Image.ImagePath : "no-image.jpg",

                    Quantity = x.odt.Quantity,
                    TotalDetail = (decimal?)x.odt.Total
                }).ToList()

            };


            return Ok(oderDetailInfo);
        }

        [HttpPut("Confim_Order")]
        public async Task<IActionResult> ChangeToConfirm(int orderId)
        {
            var userIdClaim = User.Claims.FirstOrDefault(u => u.Type == "UserId");
            if (userIdClaim == null)
            {
                return BadRequest("Can not find User");
            }
            int userId = int.Parse(userIdClaim.Value);
            var shop = await _context.TbShops.FirstOrDefaultAsync(x => x.UserId == userId);

            if (shop == null)
            {
                throw new Exception("Shop not found");
            }
            int shopid = shop.ShopId;
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            var order = await _context.TbOrders.FindAsync(orderId);
            if (order == null) throw new Exception("Can not find order.");


            order.ToConfirm = 3;
            order.ConfirmDate = DateTime.Now;

            _context.TbOrders.Update(order);
            await _context.SaveChangesAsync();


            var orderDetail = await _context.TbOrderDetails.Where(x => x.OrderId == orderId).ToListAsync();
            foreach (var item in orderDetail)
            {

                item.ToConfirm = 3;
                _context.TbOrderDetails.Update(item);
            }
            await _context.SaveChangesAsync();
            return Ok("Confirm successfully!");
        }

        [HttpPut("Cancle_Order")]
        public async Task<IActionResult> CancleToConfirm(int orderId)
        {
            var userIdClaim = User.Claims.FirstOrDefault(u => u.Type == "UserId");
            if (userIdClaim == null)
            {
                return BadRequest("Can not find User");
            }
            int userId = int.Parse(userIdClaim.Value);
            var shop = await _context.TbShops.FirstOrDefaultAsync(x => x.UserId == userId);

            if (shop == null)
            {
                throw new Exception("Shop not found");
            }
            int shopid = shop.ShopId;
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            var order = await _context.TbOrders.FindAsync(orderId);
            if (order == null) throw new Exception("Can not find order.");


            order.ToConfirm = 4;
            order.CancleDate = DateTime.Now;

            _context.TbOrders.Update(order);

            await _context.SaveChangesAsync();


            var orderDetail = await _context.TbOrderDetails.Where(x => x.OrderId == orderId).ToListAsync();
            foreach (var item in orderDetail)
            {
                item.ToConfirm = 4;
                _context.TbOrderDetails.Update(item);
                var productId = await _context.TbProducts.FindAsync(item.ProductId);
                productId.Quantity += item.Quantity;
                _context.TbProducts.Update(productId);
            }
            await _context.SaveChangesAsync();
            return Ok("Cancle successfully!");
        }

        [HttpPut("Confim_To_Feedback/{orderId:int}")]
        public async Task<IActionResult> ConfirmToFeedack(int orderId)
        {
            var userIdClaim = User.Claims.FirstOrDefault(u => u.Type == "UserId");
            if (userIdClaim == null)
            {
                return BadRequest("Can not find User");
            }
            int userId = int.Parse(userIdClaim.Value);

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            var order = await _context.TbOrders.FindAsync(orderId);
            if (order == null) throw new Exception("Can not find order.");

            //var query = await( from od in _context.TbOrderDetails
            //                        join o in _context.TbOrders on od.OrderId equals o.OrderId
            //                        where od.OrderId == orderId
            //                        select od).FirstOrDefaultAsync();

            //query.RecievedStatus = true;
            order.ReceivedDate = DateTime.Now;



            _context.TbOrders.Update(order);
            await _context.SaveChangesAsync();


            var orderDetail = await _context.TbOrderDetails.Where(x => x.OrderId == orderId).ToListAsync();
            foreach (var item in orderDetail)
            {

                item.RecievedStatus = true;

                _context.TbOrderDetails.Update(item);

                var productId = await _context.TbProducts.FindAsync(item.ProductId);
                if (productId.QuantitySold == null)
                {
                    productId.QuantitySold = item.Quantity;
                }
                else
                {
                    productId.QuantitySold += item.Quantity;
                }
                _context.TbProducts.Update(productId);


            }
            await _context.SaveChangesAsync();



            return Ok("Confirm successfully!");
        }


        [HttpGet("Product_To_Feedback")]
        public async Task<IActionResult> GetPreoductToFeedback()
        {


            var userIdclaim = User.Claims.FirstOrDefault(u => u.Type == "UserId");
            if (userIdclaim == null)
            {
                return null;
            }
            int userid = int.Parse(userIdclaim.Value);




            var query = from odt in _context.TbOrderDetails
                        join p in _context.TbProducts on odt.ProductId equals p.ProductId
                        join s in _context.TbShops on p.ShopId equals s.ShopId
                        join o in _context.TbOrders on odt.OrderId equals o.OrderId
                        join ig in _context.TbImages on p.ProductId equals ig.ProductId into images
                        where odt.RecievedStatus == true && o.UserId == userid && odt.ToFeedback == null
                        select new
                        {
                            p,
                            odt,
                            s,
                            o,
                            Image = images.FirstOrDefault()
                        };

            var product = await query.Select(x => new ProductFeedBackInfo()

            {
                OrderDetailId = x.odt.Id,
                ShopName = x.s.ShopName,
                ProductId = x.p.ProductId,
                NameProduct = x.p.Name,
                SoldPrice = (decimal?)x.odt.ProductPrice,
                DiscountPrice = (decimal?)x.odt.DiscountPrice,
                Quantity = x.odt.Quantity,
                ImagePath = x.Image != null ? x.Image.ImagePath : "no-image.jpg",
                ReceivedDate = (DateTime?)x.o.ReceivedDate,

                TotalDetail = (decimal?)x.odt.Total
            }).ToListAsync();
            return Ok(product);
        }



    }





}
