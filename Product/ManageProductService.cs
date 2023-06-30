

using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Net.Http.Headers;
using System.Security.AccessControl;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.Data.SqlClient;
using System;
using BirdPlatFormEcommerce.DEntity;
using Azure.Core;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;

namespace BirdPlatFormEcommerce.Product
{
    public class ManageProductService : IManageProductService
    {
        private readonly DataswpContext _context;

      


        public ManageProductService(DataswpContext context)
        {
            _context = context;
          
         
        }

      //  public async Task<int> AddImages(int productId, ProductImageCreateRequest request)
      //  {
      //     var image = new TbImage()
      //          {
                    
      //                  Caption = "Image",
      //                  CreateDate = DateTime.Now,
      //                  IsDefault = request.IsDefault,
      //                  SortOrder = request.SortOrder,
      //                 ProductId = productId,
                        
                    
      //          };
      //      if(request.ImageFile != null)
      //      {
      ////          image.ImagePath = await this.SaveFile(request.ImageFile);
      //          image.FileSize = request.ImageFile.Length;
      //      }
      //      //}
      //      _context.TbImages.Add(image);
      //      await _context.SaveChangesAsync();
      //      return image.ProductId;

      //  }

       
        public Task<List<TbImage>> GetListImage(int productId)
        {
            throw new NotImplementedException();
        }

        public Task<int> RemoveImages(int imageId)
        {
            throw new NotImplementedException();
        }

      

        public Task<int> UpdateImages(int imageId, string caption, bool isDefault)
        {
            throw new NotImplementedException();
        }

        
    }
}