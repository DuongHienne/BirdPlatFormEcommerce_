
using Azure.Core;
using BirdPlatFormEcommerce.DEntity;
using BirdPlatFormEcommerce.Helper;
using BirdPlatFormEcommerce.Product;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System.Diagnostics.Eventing.Reader;
using static System.Net.Mime.MediaTypeNames;


namespace BirdPlatFormEcommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly DataswpContext _context;
        private readonly IHomeViewProductService _homeViewProductService;
        private readonly IManageProductService _manageProductService;
        private readonly IWebHostEnvironment _enviroment;
      

        public ProductsController(DataswpContext context, IHomeViewProductService homeViewProductService, IManageProductService manageProductService,
            IWebHostEnvironment environment)
        {
            _context = context;
            _homeViewProductService = homeViewProductService;
            _manageProductService = manageProductService;
            _enviroment = environment;
         
        }

      
        [HttpGet("Hot_Product")]
        public async Task<IActionResult> GetProductByRateAndQuantitySold()
        {
            {
                var product = await _homeViewProductService.GetProductByRateAndQuantitySold();
                return Ok(product);
            }

        }

        [HttpGet("Product_ShopId")]
        public async Task<IActionResult> GetProductByShopId(int shopId)
        {
            var product = await _homeViewProductService.GetProductByShopId(shopId);
            if (product == null)

                return BadRequest("Cannot find product");

            return Ok(product);
        }

        [HttpGet("All_Product")]
        public async Task<IActionResult> GetAllProduct()
        {
            var product = await _homeViewProductService.GetAllProduct();
            return Ok(product);
        }

        [HttpGet("detail_product")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _homeViewProductService.GetProductById(id);
            if (product == null)

                return BadRequest("cannot find product");

            return Ok(product);
        }

        [HttpGet("Shop_Detail_Product")]
        public async Task<IActionResult> GetShopById(int id)
        {
            var shop = await _homeViewProductService.GetShopById(id);
            if (shop == null)

                return BadRequest("cannot find product");

            return Ok(shop);
        }


      





        [HttpPost]
        [Route("Add_Product_Image")]
        public async Task<IActionResult> AddProductImage([FromForm] ProductImageCreateRequest request, int productId)
        {
            APIResponse response = new APIResponse();
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var image = new TbImage()
                {
                   
                    Caption = "Image",
                    CreateDate = DateTime.Now,
                    IsDefault = request.IsDefault,
                    SortOrder = request.SortOrder,
                    ProductId = productId,

                    ImagePath = GetImageProductPath(productId),

                };  
                string Filepath = GetFileProductPath(productId);
                if (!System.IO.Directory.Exists(Filepath))
                {
                    System.IO.Directory.CreateDirectory(Filepath);
                }

                string imagepath = Path.Combine(Filepath, productId + ".png");
                if (System.IO.File.Exists(imagepath))
                {
                    System.IO.File.Delete(imagepath);
                }
                using (FileStream stream = System.IO.File.Create(imagepath))
                {
                    await request.ImageFile.CopyToAsync(stream);
                    response.ResponseCode = 200;
                    response.Result = "pass";

                }
                _context.Add(image);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                response.Errormessage= ex.Message;

            }
            return Ok(response);
        }

        [HttpPut("List_Upload_Image")]
        public async Task<IActionResult> ListUploadImage(IFormFile[] filecollection, int productId, [FromForm] ListProductImageCreateRequest request)
        {
            APIResponse response = new APIResponse();
            int passcount = 0;
            int errorcount = 0;
            try
            {

                string Filepath = GetFileProductPath(productId);
                if(!Directory.Exists(Filepath))
                {
                    Directory.CreateDirectory(Filepath);
                }

                if(filecollection.Length > 0)
                {
                    foreach (var file in filecollection)
                    {

                        var image = new TbImage()
                        {

                            Caption = "Image",
                            CreateDate = DateTime.Now,
                            IsDefault = request.IsDefault,
                            SortOrder = request.SortOrder,
                            ProductId = productId,

                            ImagePath = GetImageProductPath(productId),



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

                    }
                    await _context.SaveChangesAsync();
                }
               

            }
            catch (Exception ex)
            {
                errorcount++;
                response.Errormessage= ex.Message;
            }

            response.ResponseCode = 200;
            response.Result = passcount + "File uploaded &" + errorcount + "File failed";
            return Ok(response);
        }



        [HttpPost]
        [Route("Add_Image_ThanhCho")]
        public async Task<IActionResult> AddImage( IFormFile[] fileimages)
        {
            // string Filepath = GetFileProductPath(productId);
            string pictures = "";
            if (fileimages.Length > 0)
            {
                foreach (var file in fileimages)
                {
                    string imagepath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "user-content","product" ,file.FileName);
                    if (System.IO.File.Exists(imagepath))
                    {
                        System.IO.File.Delete(imagepath);
                    }
                    using (FileStream stream = System.IO.File.Create(imagepath))
                    {
                        await file.CopyToAsync(stream);
                    }
                    pictures += "/user-content/product" + file.FileName + ";";
                   
                }
                return Ok(pictures);

            }
            else
            {
                return Ok("khong add dc anh");
            }
        }


        [HttpGet("Image_ProductID")]
        public async Task<IActionResult> GetImageById(int productId)
        {
         //   var image = await _context.TbImages.FindAsync(productId);
            var tb_image = await _context.TbImages.Where(x => x.ProductId == productId && x.IsDefault == true).FirstOrDefaultAsync();

      //      if (image == null)
      
      //          throw new Exception("can not find an image with id");

            string hosturl = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}";
            try
            {

                string Filepath = GetFileProductPath(productId);
                string imageUrl = Path.Combine(Filepath, productId + ".png");
                if (System.IO.File.Exists(imageUrl))
                {
                    var productImageVM = new ProductImageVM()
                    {
                        ImageId = tb_image.Id,
                        ProductId = productId,
                        FileSize = tb_image.FileSize,
                        IsDefault = tb_image.IsDefault,
                        SortOrder = tb_image.SortOrder,
                        ImagePath = hosturl + "/user-content/product/" + productId + "/" + productId + ".png"

                    };
                    return Ok(productImageVM);
                }



            }
            catch (Exception ex)
            {

            }
            return NotFound();


        }

        [HttpGet("List_Image_ProductID")]
        public async Task<IActionResult> GetListImageById(int productId)
        {
            List<string> Imageurl = new List<string>();
            string hosturl = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}";


            try
            {

                string Filepath = GetFileProductPath(productId);

                if (System.IO.Directory.Exists(Filepath))
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(Filepath);
                    FileInfo[] fileInfos = directoryInfo.GetFiles();
                    foreach (FileInfo fileInfo in fileInfos)
                    {
                        string filename = fileInfo.Name;
                        string imagepath = Filepath + "\\" + filename;
                        if (System.IO.File.Exists(imagepath))
                        {
                            string _Imageurl = hosturl + "/user-content/product/" + productId + "/" + filename;
                            Imageurl.Add(_Imageurl);
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return Ok(Imageurl);
         
        }


     
   
        private string GetFileProductPath(int productId)
        {
            return this._enviroment.WebRootPath + "\\user-content\\product\\" + productId.ToString();
        }


        private string GetImageProductPath(int productId)
        {
            string hosturl = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}";
            return hosturl + "/user-content/product/" + productId + "/" + productId + ".png";
        
        }

        
    }
    

}

