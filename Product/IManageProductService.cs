

using BirdPlatFormEcommerce.DEntity;

namespace BirdPlatFormEcommerce.Product
{
    public interface IManageProductService
    {

        Task<int> RemoveImages(int imageId);

        Task<int> UpdateImages(int imageId, string caption, bool isDefault);

        Task<List<TbImage>> GetListImage(int productId);
    }
}




//string picture = "";
//if (fileimages.Length > 0)
//{
//    foreach (var file in fileimages)
//    {
//        var image = new TbImage
//        {
//            ProductId = product.ProductId,
//            FileSize = fileimages.Length,
//            Caption = "other image",
//            IsDefault = true,
//            ImagePath = Path.Combine(Directory.GetCurrentDirectory(), USER_CONTENT_FOLDER_NAME, file.FileName);
//        using (var stream = System.IO.File.Create(image.ImagePath))
//        {
//            await file.CopyToAsync(stream);
//        }
//        picture += "/" + USER_CONTENT_FOLDER_NAME + "/" + file.FileName;
//    }
//    return picture;
