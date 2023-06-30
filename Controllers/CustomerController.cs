using BirdPlatFormEcommerce.DEntity;
using BirdPlatFormEcommerce.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BirdPlatFormEcommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly DataswpContext _context;

        public CustomerController(DataswpContext swp)
        {
            _context = swp;
        }

        //[HttpPost("OderProduct")]
        //public async Task<IActionResult> OderProduct(TbOrder oder)
        //{
        //    var useridClaim = User.Claims.FirstOrDefault(u => u.Type == "UserId");
        //    if (useridClaim == null)
        //    {
        //        throw new Exception("Please Login");
        //    }
        //    int userid = int.Parse(useridClaim.Value);
        //    //kiem tra xem null hay khong
        //    if (oder == null || oder.TbOrderDetails == null || oder.TbOrderDetails.Count == 0)
        //    {
        //        return BadRequest("Invalid order data");
        //    }
        //    //kiem tra thong tin va ngay mua hang 
        //    oder.UserId = userid;
        //    oder.OrderDate = DateTime.Now;

        //    //tinh tien mua hang 
        //    foreach (var oderDetail in oder.TbOrderDetails)
        //    {
        //        var product = await _context.TbProducts.FirstOrDefaultAsync(x => x.ProductId == oderDetail.ProductId);
        //        if (product == null)
        //        {
        //            return BadRequest($"Invalid product id: {oderDetail.ProductId}");
        //        }
        //        oderDetail.SubTotal = product.Price * oderDetail.Quantity;
        //    }
        //    try
        //    {
        //        await _context.TbOrders.AddAsync(oder);
        //        await _context.SaveChangesAsync();

        //    }
        //    catch (Exception e)
        //    {
        //        return StatusCode(500, $"Error creating order: {e.Message}");

        //    }
        //    return Ok("Success");

        //}
       


    }
}
