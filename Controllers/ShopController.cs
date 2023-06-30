using BirdPlatForm.UserRespon;
using BirdPlatFormEcommerce.DEntity;
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

namespace BirdPlatFormEcommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShopController : ControllerBase
    {
        private readonly DataswpContext _context;
        private readonly IManageProductService _manageProductService;
        private readonly IWebHostEnvironment _enviroment;
        private readonly IOrderService _oderService;

        public ShopController(DataswpContext swp, IManageProductService manageProductService, IWebHostEnvironment enviroment,IOrderService orderService )
        {
            _context = swp;
            _manageProductService = manageProductService;
            _enviroment = enviroment;
            _oderService = orderService;
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

                UserId = userId,

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
            return Ok(new ErrorRespon
            {

                Message = "Register shop Success",
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
        [HttpGet]
        public async Task<IActionResult> getMyshop()
        {
            var myshop = User.Claims.FirstOrDefault(u => u.Type == "UserId");
            if(myshop == null)
            {
                return NotFound();
            }
            int userid = int.Parse(myshop.Value);
            var shop = _context.TbShops.FirstOrDefault(x => x.UserId == userid);
            if(shop == null)
            {
                return NotFound();
            }
            var isshop = new ViewShop
            {
                Rate = (int)shop.Rate,
                shopName = shop.ShopName,
                Address = shop.Address,
                phone = shop.Phone,
                
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
                    DiscountPercent = request.DiscountPercent,
                    SoldPrice = (int)Math.Round((decimal)(request.Price - request.Price / 100 * (request.DiscountPercent))),
                    Decription = request.Decription,
                    Status = true,
                    //          CreateDate = request.CreateDate,
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
        public async Task<IActionResult> UpdateProduct([FromForm]  UpdateProductViewModel request)
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
            return hosturl + "/user-content/product/" + productId + "/" + fileName ;

        }


        [HttpGet("AllProduct/shop")]
        public async Task<IActionResult> GetallProduct()
        {
            var userid = User.Claims.FirstOrDefault(x => x.Type == "UserId");
            if(userid == null)
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


        //[HttpGet("Revenue_month")]
        //public async Task<IActionResult> GetRevenueMonth()
        //{

        //    var userIdClaim = User.Claims.FirstOrDefault(u => u.Type == "UserId");
        //    if (userIdClaim == null)
        //    {
        //        throw new Exception("User not found");
        //    }
        //    int userid = int.Parse(userIdClaim.Value);
        //    var shop = await _context.TbShops.FirstOrDefaultAsync(s => s.UserId == userid);
        //    if (shop == null)
        //    {
        //        throw new Exception("Shop not found");
        //    }
        //    int shopid = shop.ShopId;

        //    int currentYear = DateTime.Now.Year;
        //    var query = from od in _context.TbOrderDetails
        //                join p in _context.TbProducts on od.ProductId equals p.ProductId
        //                join s in _context.TbShops on p.ShopId equals s.ShopId
        //                where s.ShopId == shopid
        //                select new TbOrder
        //                {
        //                    ShopId = s.ShopId,

        //                };

        //    var data = await query.ToListAsync();

        //    // Khoi tao mang chua kq TotalRevenue của moi thang
        //    decimal[] monthlyRevenue = new decimal[12];

        //    for (int i = 0; i < 12; i++)
        //    {
        //        DateTime currentMonthStart = new DateTime(currentYear, i + 1, 1);
        //        DateTime currentMonthEnd = currentMonthStart.AddMonths(1).AddDays(-1);

                
              

        //        // Tính tổng doanh thu của shop trong tháng hiện tại
        //        decimal totalRevenue = data.Where(p => p.Orderdate >= currentMonthStart && p.Orderdate <= currentMonthEnd).Sum(p => p.Total ?? 0m);

        //        // Gán đối tượng tháng vào mảng monthlyRevenue
        //        monthlyRevenue[i] = totalRevenue;
        //    }

        //    return Ok(monthlyRevenue);
        //}



        //[HttpGet("ToTal_Revenue")]
        //public async Task<IActionResult> GetToTalRevenue()
        //{

        //    var userIdClaim = User.Claims.FirstOrDefault(u => u.Type == "UserId");
        //    if (userIdClaim == null)
        //    {
        //        throw new Exception("User not found");
        //    }
        //    int userid = int.Parse(userIdClaim.Value);
        //    var shop = await _context.TbShops.FirstOrDefaultAsync(s => s.UserId == userid);
        //    if (shop == null)
        //    {
        //        throw new Exception("Shop not found");
        //    }
        //    int shopid = shop.ShopId;


        //    var query = from od in _context.TbOrderDetails
        //                join p in _context.TbProducts on od.ProductId equals p.ProductId
        //                join s in _context.TbShops on p.ShopId equals s.ShopId
        //                where s.ShopId == shopid
        //                select new TbOrder
        //                {
        //                    ShopId = s.ShopId,

        //                };

        //    var data = await query.ToListAsync();

        //        // Tính tổng doanh thu của shop trong tháng hiện tại
        //        decimal totalRevenue = data.Sum(p => p.Total ?? 0m);

               
              
            

        //    return Ok(totalRevenue);
        //}

        //[HttpGet("Revenue_week")]
        //public async Task<IActionResult> GetRevenueWeek()
        //{
        //    //lay shopid theo userid dang login
        //    var userIdClaim = User.Claims.FirstOrDefault(u => u.Type == "UserId");
        //    if (userIdClaim == null)
        //    {
        //        throw new Exception("User not found");
        //    }
        //    int userid = int.Parse(userIdClaim.Value);
        //    var shop = await _context.TbShops.FirstOrDefaultAsync(s => s.UserId == userid);
        //    if (shop == null)
        //    {
        //        throw new Exception("Shop not found");
        //    }
        //    int shopid = shop.ShopId;

        //    DateTime today = DateTime.Today;
        //    int currentYear = today.Year;
        //    int currentWeek = (today.DayOfYear + 6) / 7;

        //    var query = from od in _context.TbOrderDetails
        //                join p in _context.TbProducts on od.ProductId equals p.ProductId
        //                join s in _context.TbShops on p.ShopId equals s.ShopId
        //                where s.ShopId == shopid
        //                select new TbOrder
        //                {
        //                    ShopId = s.ShopId,
                           
        //                };

        //    var data = await query.ToListAsync();

        //    // Khởi tạo mảng chứa kết quả TotalRevenue của mỗi ngày trong tuần
        //    decimal[] dailyRevenue = new decimal[7];

        //    for (int i = 0; i < 7; i++)
        //    {
        //        DateTime currentDate = FirstDateOfWeek(currentYear, currentWeek).AddDays(i);

        //        // Tính tổng doanh thu của shop trong ngày hiện tại
        //        decimal totalRevenue = data.Where(p => p.Orderdate.Date== currentDate.Date).Sum(p => p.Total ?? 0m);

        //        // Gán giá trị tổng doanh thu vào mảng dailyRevenue
        //        dailyRevenue[i] = totalRevenue;
        //    }

        //    return Ok(dailyRevenue);
        //}

        //// Hàm để lấy ngày đầu tiên của tuần dựa trên số tuần và năm
        //public static DateTime FirstDateOfWeek(int year, int week)
        //{
        //    DateTime jan1 = new DateTime(year, 1, 1);
        //    int daysToFirstDayOfWeek = (int)jan1.DayOfWeek - 1;

        //    if (daysToFirstDayOfWeek <= 3)
        //    {
        //        return jan1.AddDays((week - 1) * 7 - daysToFirstDayOfWeek);
        //    }
        //    else
        //    {
        //        return jan1.AddDays(7 - daysToFirstDayOfWeek + (week - 1) * 7);
        //    }
        //}

        //[HttpGet("orders")]
        //public async Task<ActionResult<List<OrderInfo>>> GetOrdersByShopId()
        //{
        //    var userIdclaim = User.Claims.FirstOrDefault(u => u.Type == "UserId");
        //    if (userIdclaim == null)
        //    {
        //        return null;
        //    }
        //    int userid = int.Parse(userIdclaim.Value);
        //    var shop = await _context.TbShops.FirstOrDefaultAsync(s => s.UserId == userid);
        //    if (shop == null)
        //        return null;
        //    int shopid = shop.ShopId;
        //    var orders = await _context.TbOrders
        //        .Where(o => o.TbOrderDetails.Any(od => od.ToConfirm == 2 && _context.TbProducts.Any(p => p.ProductId == od.ProductId && p.ShopId == shopid)))
        //        .Select(o => new OrderInfo
        //        {
        //            orderId = o.OrderId,
                    
        //            OrderDate = (DateTime)o.OrderDate,
        //            UserName = o.User.Name,
        //            Email = o.User.Email,
        //            Status = (bool)o.Status
                    
        //        })
        //        .ToListAsync();

        //    return orders;
        //}


        [HttpGet("Getodershop")]
        public async Task<List<TbOrderDetail>> getoderShop()
        {
            var userIdclaim = User.Claims.FirstOrDefault(u => u.Type == "UserId");
            if(userIdclaim == null)
            {
                return null;
            }
            int userid = int.Parse(userIdclaim.Value);
            var shop = await _context.TbShops.FirstOrDefaultAsync(s => s.UserId == userid);
            if (shop == null)
                return null; 
            var oder = await _oderService.Orderservice(shop.ShopId);
            return oder;
        }
    }
}
