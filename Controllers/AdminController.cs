﻿using BirdPlatForm.UserRespon;
using BirdPlatForm.ViewModel;
using BirdPlatFormEcommerce.Helper.Mail;
using BirdPlatFormEcommerce.NEntity;
using BirdPlatFormEcommerce.ViewModel;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using System;
using System.Diagnostics.Contracts;

namespace BirdPlatFormEcommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "AD")]
    public class AdminController : ControllerBase
    {
        private readonly SwpDataBaseContext _context;
        private readonly IMailService _mailService;

        public AdminController(SwpDataBaseContext swp, IMailService mailService)
        {
            _context = swp;
            _mailService = mailService;
        }
        [HttpGet]
        public async Task<IActionResult> getAlluser()
        {
            var user = _context.TbUsers.ToList();
            return Ok(user);
        }
        [HttpPut("UpdateUser/{id}")]
        public async Task<IActionResult> UpdateUser(int id, UserUpdate user)

        {
            var update = await _context.TbUsers.FindAsync(id);
            if (update != null)
            {
                update.Dob = user.Dob;
                update.Gender = user.Gender;
                update.Name = user.Name;
                update.CreateDate = user.CreateDate;
                update.UpdateDate = user.UpdateDate;
                update.Avatar = user.Avatar;
                update.Phone = user.Phone;
                update.Address = user.Address;
                await _context.SaveChangesAsync();
                return Ok(update);
            }
            return BadRequest("Faill ");
        }
        [HttpPut("Bandaccount")]
        public async Task<IActionResult> Bandaccount(int userid)
        {
            var user = await _context.TbUsers.FindAsync(userid);
            string Email = user.Email;

            if (user != null)
            {
                user.Status = true;
                _context.TbUsers.Update(user);


                var mailRequest = new MailRequest()
                {
                    ToEmail = Email,
                    Subject = "[BIRD TRADING PLATFORM] Tài khoản của bạn đã bị khóa",
                    Body =  "  Tài khoản của bạn đã bị khóa do vi phạm quy định của chúng tôi. Mọi thắc mắc hãy liên hệ với chúng tôi." +
                    "Email: longnhatlekk@gmail.com"

                };


                await _mailService.SendEmailAsync(mailRequest);
            }
            await _context.SaveChangesAsync();
            return Ok("band User Success");

        }
        [HttpPut("OpenAccount")]
        public async Task<IActionResult> OpenAccount(int userid)
        {
            var user = await _context.TbUsers.FindAsync(userid);
            string Email = user.Email;

            if (user != null)
            {
                user.Status = false;
                _context.TbUsers.Update(user);


                var mailRequest = new MailRequest()
                {
                    ToEmail = Email,
                    Subject = "[BIRD TRADING PLATFORM] Tài khoản của bạn đã được mở  khóa",
                    Body = "  Tài khoản của bạn được chúng tôi xem xét và mở khóa . Mọi thắc mắc hãy liên hệ với chúng tôi." +
                    "Email: longnhatlekk@gmail.com"

                };


                await _mailService.SendEmailAsync(mailRequest);
            }
            await _context.SaveChangesAsync();
            return Ok("Open User Success");

        }
        [HttpGet("TopUsers")]
        public async Task<IActionResult> GetTopUsers()
        {
            var topUsers = await _context.TbUsers
                .Join(_context.TbOrders, u => u.UserId, o => o.UserId, (u, o) => new { User = u, Order = o })
                .Where(uo => uo.Order.ToConfirm == 3 && uo.Order.ReceivedDate != null)
                .GroupBy(uo => new { uo.User.UserId, uo.User.Name })
                .Select(g => new
                {
                    UserId = g.Key.UserId,
                    UserName = g.Key.Name,
                    TotalAmount = g.Sum(uo => uo.Order.TotalPrice)
                })
                .OrderByDescending(u => u.TotalAmount)
                .Take(5)
                .ToListAsync();

            List<decimal> totalAmounts = topUsers.Select(x => x.TotalAmount).ToList();
            List<string> UserName = topUsers.Select(x => x.UserName).ToList();

            var response = new
            {
                TotalAmounts = totalAmounts,
                Name = UserName
            };

            return Ok(response);

        }


        [HttpGet("GetUser/{id}")]
        public async Task<IActionResult> GetUserByid(int id)
        {
            var user = await _context.TbUsers.FindAsync(id);
            if (user == null)
            {
                return Ok(new ErrorRespon
                {
                    Error = false,
                    Message = "No User :("
                });
            }
            return Ok(user);
        }
        [HttpDelete("{Id}")]
        public async Task<IActionResult> Deleteacount(int Id)
        {
            var tokens = _context.TbTokens.Where(t => t.Id == Id).ToList();
            if (tokens == null)
            {
                return null;
            }

            _context.TbTokens.RemoveRange(tokens);
            var user = await _context.TbUsers.FindAsync(Id);


            if (user != null)
            {
                _context.TbUsers.Remove(user);
            }

            _context.SaveChanges();

            return Ok("Delete Success");

        }
        [HttpGet("CountSellingProducts")]
        public async Task<IActionResult> CountSellingProducts()
        {
            var count = await countProduct();
            return Ok(count);

        }
        private async Task<int> countProduct()
        {
            var count = await _context.TbProducts.CountAsync(x => x.Status.HasValue && x.Status.Value == true);

            return count;
        }
        [HttpGet("GetCustomer")]
        public async Task<IActionResult> GetCustomer()
        {
            var countCus = await CountCus();
            return Ok(countCus);
        }
        [HttpGet("detailcus")]
        public async Task<IActionResult> getdetailCus()
        {
            var customers =  _context.TbUsers
                .Where(r => r.RoleId == "CUS")
                .Select(r => new Customer
                {
                    UserId = r.UserId,
                    birth = (DateTime)(r.Dob != null ? (DateTime?)r.Dob : null),
                    Gender = r.Gender,
                    Username = r.Name,
                    Email = r.Email,
                    
                    Phone =r.Phone ?? null,
                    Address =r.Address ?? null,
                    Avatar = r.Avatar ?? null,
                    Status = r.Status,
                }).ToList();
            return Ok(customers);
        }
        private async Task<int> CountCus()
        {
            var countcus = await _context.TbUsers.CountAsync(x => x.RoleId == "CUS");
            return countcus;
        }
        [HttpGet("GetShop")]
        public async Task<IActionResult> GetShop()
        {
            var countshop = await CountShop();
            return Ok(countshop);
        }
        [HttpGet("DetailShop")]
        public async Task<IActionResult> getDetailShop()
        {
           

            var query = from  s in _context.TbShops                
                       
                        join u in _context.TbUsers on s.UserId equals u.UserId
                     
                        where  u.IsShop == true 
                        select new { s, u};

            var totalRevenueQuery = from s in _context.TbShops
                                      join o in _context.TbOrders on s.ShopId equals o.ShopId 
                                    //where o != null
                                    //&& o.ReceivedDate != null 
                                    group o by s.ShopId into g
                                    select new
                                    {
                                        ShopId = g.Key,
                                        TotalRevenue = (decimal?)g.Sum(x => (decimal?)x.LastTotalPrice) ?? null
                                    };


            var results = await query
                  .GroupBy(x => x.s.ShopId)
                  .Select(x => new ShopAdmin()
                  {

                      ShopId = x.Key,
                      ShopName = x.FirstOrDefault().s.ShopName,
                      UserId = x.FirstOrDefault().u.UserId,
                      birth = (DateTime)(x.FirstOrDefault().u.Dob != null ? (DateTime?)x.FirstOrDefault().u.Dob : null),
                      Gender = x.FirstOrDefault().u.Gender,
                      Username = x.FirstOrDefault().u.Name,
                      
                      Email = x.FirstOrDefault().u.Email,
                      IsActive = (bool)x.FirstOrDefault().s.IsVerified ,
                      PhoneHome = x.FirstOrDefault().u.Phone ?? null,
                      AddressHome = x.FirstOrDefault().u.Address ?? null,
                      Avatar = x.FirstOrDefault().u.Avatar ?? null,
                      
                      AddressShop = x.FirstOrDefault().s.Address ?? null,
                      PhoneShop = x.FirstOrDefault().s.Phone ?? null,
                      Status = x.FirstOrDefault().u.Status,
                      RateShop = x.FirstOrDefault().s.Rate ?? null,
                      
                      TotalRevenue = totalRevenueQuery.Where(t => t.ShopId == x.Key).Select(t => t.TotalRevenue).FirstOrDefault() ?? null,
                     
                      


                  }).ToListAsync();
          
           
            return Ok(results);
        }


        private async Task<int> CountShop()
        {
            var countcus = await _context.TbUsers.CountAsync(x => x.RoleId == "SP");
            return countcus;
        }
        [HttpGet("Product/shop")]
        public async Task<IActionResult> GetProductShop(int shopId)
        {
            var pro = await _context.TbProducts.CountAsync(x => x.ShopId == shopId);
            return Ok(pro);
        }
        [HttpGet("TotalAmount/HighShop")]
        public async Task<IActionResult> gettotalAmounthighShop()
        {
                var userIdClaim = User.Claims.FirstOrDefault(u => u.Type == "UserId");
                if (userIdClaim == null)
                {
                    throw new Exception("User not found");
                }
                int userid = int.Parse(userIdClaim.Value);

                int currentMonth = DateTime.Now.Month;
                int currentYear = DateTime.Now.Year;

                var query = from o in _context.TbOrders
                            join s in _context.TbShops on o.ShopId equals s.ShopId
                            where o.ToConfirm == 3 && o.OrderDate.Month == currentMonth && o.OrderDate.Year == currentYear
                            select new { s, o };

            var results = await query
                  .GroupBy(x => x.s.ShopId )
                  .Select(x => new ShoptotalAmount()
             {
                  ShopId= x.Key,
                  shopName = x.FirstOrDefault().s.ShopName,
                  Total =(decimal?) x.Sum(p => (decimal?)p.o.TotalPrice ?? 0m)
                 })
                  .OrderByDescending(x => x.Total)
                  .Take(10)
                  .ToListAsync();

                List<decimal> totalAmounts = results.Select(x =>(decimal) x.Total).ToList();
                List<string> shopNames = results.Select(x => x.shopName).ToList();

                var response = new
                {
                    TotalAmounts = totalAmounts,
                    ShopNames = shopNames
                };

                return Ok(response);
            
        }

        [HttpGet("CountReport")]
        public async Task<IActionResult> Countreport()
        {
            var shopReportCounts = await _context.TbReports
                .Select(x => x.ShopId)
                .Distinct()
                .CountAsync();
            


            return Ok(shopReportCounts);


        }
        
        [HttpGet("getreport")]
        public async Task<IActionResult> getreportShop(int shopid)
        {
            var shop = await _context.TbShops.FindAsync(shopid);
            if (shop == null)
            {
                return BadRequest("No shop");
            }
            var report = await _context.TbReports.Include(r => r.CateRp)
                .Where(r => r.ShopId == shopid)
                .Select(r => new ShopreportModel
                {
                    reportID = r.ReportId,
                    detail = r.Detail,
                    DetailCate = r.CateRp.Detail

                })
                .ToListAsync();
            var shopreport = new Shopreport
            {
                shopId = shop.ShopId,
                shopname = shop.ShopName,
                IsVerifi = (bool)shop.IsVerified,
                reports = report
            };

            return Ok(shopreport);


        }
        [HttpPost("Sendwarning")]
        public async Task<IActionResult> SendwarningShop(int shopid)
        {
            var shop = _context.TbShops.Find(shopid);
            if (shop == null) { return BadRequest("Shop not found"); }

            var user = await _context.TbUsers.FindAsync(shop.UserId);
            if (user == null) { return NotFound(); }

            string email = user.Email;

            var reports = await _context.TbReports
                .Include(r => r.CateRp)
                .Where(r => r.ShopId == shop.ShopId)
                .ToListAsync();

            if (reports.Count >= 1 && reports.Count <= 3)
            {
                var emailBody = $"Shop Name: {shop.ShopName}\n\n";
                emailBody += " Cảnh báo lần đầu tiên dành cho shop của nếu quá 3 lần report tài khoản của bạn sẽ bị khóa:\n" +
                    "Mọi thắc mắc hãy liên hệ với chúng tôi.\n";
                shop.IsVerified = false;
                foreach (var report in reports)
                {
                    
                    emailBody += $"  Detail: {report.CateRp.Detail} , {report.Detail}\n";
                    
                }

                var mailRequest = new MailRequest()
                {
                    ToEmail = email,
                    Subject = "[BIRD TRADING PLATFORM] Cảnh cáo tới shop của bạn",
                    Body = emailBody
                };

                await _mailService.SendEmailAsync(mailRequest);
            }

            if (reports.Count > 3)
            {
                var emailBody = $"Shop Name: {shop.ShopName}\n\n";
                emailBody += "Dưới đây là những báo cáo của người dùng";

                foreach (var report in reports)
                {

                    emailBody += $"  Detail: {report.CateRp.Detail} , {report.Detail}\n";

                }
                user.RoleId = "CUS" ;
                shop.IsVerified = false;
                _context.TbUsers.Update(user);
                var product =await _context.TbProducts.Where(p => p.ShopId == shopid).ToListAsync();
                foreach(var products in product)
                {
                    products.IsDelete = true;
                    _context.TbProducts.Update(products);
                }
                _context.TbReports.RemoveRange(reports);
                await _context.SaveChangesAsync();

                var mailRequest = new MailRequest()
                {
                    ToEmail = email,
                    Subject = "[BIRD TRADING PLATFORM] Tài khoản của bạn đã bị khóa",
                    Body = emailBody + "  Tài khoản của bạn đã bị khóa do vi phạm quy định của chúng tôi. Mọi thắc mắc hãy liên hệ với chúng tôi." +
                    "Email: longnhatlekk@gmail.com"

                };
               
              
                await _mailService.SendEmailAsync(mailRequest);
            }

            return Ok("Warning email sent successfully.");
        }
        [HttpPost("Openaccountshop")]
        public async Task<IActionResult> openAccount(int shopid)
        {
            var shop = _context.TbShops.Find(shopid);
            if (shop == null) { return BadRequest("No shop"); };
            var user = await _context.TbUsers.FindAsync(shop.UserId);
            if (user == null) { return BadRequest("No user"); }
            string email = user.Email;
            user.RoleId = "SP";
            _context.TbUsers.Update(user);
            var product = await _context.TbProducts.Where(p => p.ShopId == shopid).ToListAsync();
            foreach(var products in product)
            {
                products.IsDelete = false;
                _context.TbProducts.Update(products);
            }
            await _context.SaveChangesAsync();

            var emailBody = $"Shop Name: {shop.ShopName}\n\n";
            
            var mailRequest = new MailRequest()
            {

                ToEmail = email,
                Subject = "[BIRD TRADING PLATFORM] Tài khoản và sản phẩm của bạn đã được mở lại \n\n",
                Body =emailBody + " Tài khoản và sản phẩm của bạn đã được mở lại.\n\n Xin chào mừng bạn quay trở lại sử dụng dịch vụ của chúng tôi."
            };

            await _mailService.SendEmailAsync(mailRequest);

            return Ok("Account and products reopened successfully. Check your Email");
        }
       
        [HttpGet("GettopFeedback")]
        public async Task<IActionResult> getFeedBack()
        {
            var feedback = _context.TbProducts
                .Include(u => u.TbImages)
                .Include(u => u.Shop)
                .Include(u => u.TbFeedbacks)
                
                .Select(u => new FeedbackAdmin
                {
                    ProductId = u.ProductId,
                    Rate = (int)u.Rate,
                    productName = u.Name,
                    Price = u.Price,
                    ImageProduct = u.TbImages.FirstOrDefault().ImagePath,
                    shopName = u.Shop.ShopName,
                    FeedbackCount = u.TbFeedbacks.Count()


                })
                .OrderByDescending(u => u.FeedbackCount)
                .Take(5)
                .ToList();

            return Ok(feedback);
        }
        [HttpGet("GetquantitySold")]
        public async Task<IActionResult> getQuantitysold()
        {
            var feedback = _context.TbProducts
                .Include(u => u.TbImages)
                .Include(u => u.Shop)
                

                .Select(u => new QuantitySold
                {
                    ProductId = u.ProductId,
                    Rate = (int)u.Rate,
                    productName = u.Name,
                    Price = u.Price,
                    ImageProduct = u.TbImages.FirstOrDefault().ImagePath,
                    shopName = u.Shop.ShopName,
                    Quantitysold = (int)u.QuantitySold,


                })
                .OrderByDescending(u => u.Quantitysold)
                .Take(5)
                .ToList();

            return Ok(feedback);
        }
        [HttpGet("totalOrder")]
        public async Task<IActionResult> totalOrderCha()
        {
            var refund = _context.TbOrders.Where(x => x.ParentOrderId != null).Sum(o => o.LastTotalPrice);
            var profit = _context.TbOrders.Where(x => x.ParentOrderId == null).Sum(o => o.LastTotalPrice);
            var amount = _context.TbOrders.Where(x => x.ParentOrderId == null).Sum(o => o.TotalPrice);
            var countoderCha = _context.TbOrders.Where(x => x.ParentOrderId == null).Count();
            var totalorderCha = _context.TbOrders.Where(x => x.ParentOrderId == null).Sum(x => x.TotalPrice);
            decimal averagePriceForParentIdNull = countoderCha > 0
        ? totalorderCha / countoderCha
             :    0;

            var view = new ListtotalOrder()
            {
                TotalOrderCha = 
                
                    new totalOrderCha
                    {
                    countOrder = countoderCha,
                totalPriceOrdercha = totalorderCha,
                price1Ordercha = averagePriceForParentIdNull,
                    }
                ,
                Profit = 
                
                    new ProfitAdmin
                    {
                        profitAdmin = profit
                    }
                ,
                AmountSystem = 
                
                    new TotalAmountSystem
                    {
                        totalAmountSystem = amount
                    }
                ,
                refundAmout = 
                
                    new refundAmountShop
                    {
                        refundamout = refund
                    }
                
                
                
            };
            return Ok(view);
        }

    }
}
