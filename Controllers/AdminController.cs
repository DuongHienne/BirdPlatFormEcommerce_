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
                    birth = (DateTime)(r.Dob != null ? (DateTime?)r.Dob : null),
                    Gender = r.Gender,
                    Username = r.Name,
                    Email = r.Email,
                    Password = r.Password,
                    Phone =r.Phone ?? null,
                    Address =r.Address ?? null,
                    Avatar = r.Avatar ?? null,

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
        public List<ShoptotalAmount> gettotalAmounthighShop()
        {
            var shopTotalAmounts = _context.TbShops
        .Join(_context.TbProducts,
            shop => shop.ShopId,
            product => product.ShopId,
            (shop, product) => new { Shop = shop, Product = product })
        .Join(_context.TbOrderDetails,
            joinResult => joinResult.Product.ProductId,
            orderDetail => orderDetail.ProductId,
            (joinResult, orderDetail) => new { Shop = joinResult.Shop, OrderDetail = orderDetail })
        .GroupBy(result => result.Shop.ShopId)
        .Select(g => new ShoptotalAmount
        {
            shopId = g.Key,
            TotalAmount = (decimal)g.Sum(result => result.OrderDetail.Quantity * result.OrderDetail.Product.Price)
        })
        .OrderByDescending(sta => sta.TotalAmount)
        .ToList();
            return shopTotalAmounts;
        }

        [HttpGet("CountReport")]
        public async Task<IActionResult> Countreport()
        {
            var shopReportCounts = await _context.TbShops
                .Select(s => new ReportModel
                {
                    shopId = s.ShopId,
                    Shopname = s.ShopName,
                    Count = _context.TbReports.Count(r => r.ShopId == s.ShopId)
                })
                .ToListAsync();


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
                user.Status = true;
                _context.TbUsers.Update(user);
                var product =await _context.TbProducts.Where(p => p.ShopId == shop.ShopId).ToListAsync();
                foreach(var products in product)
                {
                    products.IsDelete = false;
                    _context.TbProducts.Update(products);
                }
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
            user.Status = false;
            _context.TbUsers.Update(user);
            var product = await _context.TbProducts.Where(p => p.ShopId == shop.ShopId).ToListAsync();
            foreach(var products in product)
            {
                products.IsDelete = true;
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

    }
}
