
using BirdPlatForm.UserRespon;
using BirdPlatForm.ViewModel;

using BirdPlatFormEcommerce.DEntity;
using BirdPlatFormEcommerce.Helper;
using BirdPlatFormEcommerce.Product;

using BirdPlatFormEcommerce.ViewModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using BirdPlatFormEcommerce.Helper.Mail;

namespace BirdPlatForm.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly DataswpContext _context;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _enviroment;
        private readonly IMailService _mailService;

        public UserController(DataswpContext bird, IConfiguration config, IWebHostEnvironment enviroment,IMailService mailService)
        {
            _context = bird;
            _config = config;
            _enviroment = enviroment;
            _mailService = mailService;
        }
        [HttpPost("Login")]
        public async Task<IActionResult> Login(UserView model)
        
        {
            var user =  _context.TbUsers.Include(x => x.Role).SingleOrDefault(o => o.Email == model.Email && model.Password == o.Password);
            if (user == null) return Ok(new ErrorRespon
            {
                Error = false,
                Message = "Email Or Password Incorrect :("
            });
            var token = await GeneratToken(user);
            return Ok(new ErrorRespon
            {
                Error = true,
                Message = "Login Success",
                Data = token
            });
        }
        [HttpPost]
        [Route("logout")]

        public async Task<IActionResult> Logout()
        {

            var userClaims = User.Claims.ToList();


            var tokenClaim = userClaims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);
            if (tokenClaim != null)
            {
                userClaims.Remove(tokenClaim);
            }



            return Ok();
        }

        private async Task<TokenRespon> GeneratToken(TbUser user)
        {
            var jwttokenHandler = new JwtSecurityTokenHandler();
            var secretkey = Encoding.UTF8.GetBytes(_config["JWT:Key"]);

            var claims = new List<Claim>
    {
         new Claim(ClaimTypes.Name, user.Name),
         new Claim("UserId", user.UserId.ToString()),
         new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
         new Claim("Email", user.Email),
         new Claim("Gender", user.Gender.ToString()),        
         new Claim("Username", user.Name.ToString()),        
         new Claim(ClaimTypes.Role, user.RoleId),       
    };
           if (user.Avatar != null)
            {
                claims.Add(new Claim("Avatar", user.Avatar));
            }
            else
            {
                claims.Add(new Claim("Avatar", "null"));
            }
            if(user.Address != null)
            {
                claims.Add(new Claim("Address", user.Address.ToString()));
            }
            else
            {
                claims.Add(new Claim("Address", "null"));
            }
            if(user.Phone != null)
            {
                claims.Add(new Claim("Phone", user.Phone.ToString()));
            }
            else
            {
                claims.Add(new Claim("Phone", "null"));
            }
            if(user.Dob != null)
            {
                claims.Add(new Claim("Dob", user.Dob.ToString()));
            }
            else
            {
                claims.Add(new Claim("Dob", "null"));
            }
            if(user.IsShop != null)
            {
                claims.Add(new Claim("IsShop", user.IsShop.ToString()));
            }else
            {
                claims.Add(new Claim("IsShop", "null"));
            }

            var tokendescription = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = _config["JWT:Issuer"],
                Audience = _config["JWT:Audience"],
                Expires = DateTime.UtcNow.AddHours(24),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretkey), SecurityAlgorithms.HmacSha512)
            };

            var token = jwttokenHandler.CreateToken(tokendescription);
            var accessToken = jwttokenHandler.WriteToken(token);

            return new TokenRespon
            {
                AccessToken = accessToken,
            };
        }

        ////var refreshToken = GenerateRefreshToken();

        ////Lưu database  
        //var refreshTokenEntity = new TbToken
        //{

        //    Id = user.UserId,
        //    UserId = user.UserId,
        //    Token = refreshToken,
        //    ExpiryDate = DateTime.UtcNow.AddSeconds(10),
        //};

        //await _context.AddAsync(refreshTokenEntity);
        //await _context.SaveChangesAsync();

        //return new TokenRespon
        //{
        //    AccessToken = accessToken,
        //    RefreshToken = refreshToken
        //};
    

    private string GenerateRefreshToken()
        {

            var random = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(random);

                return Convert.ToBase64String(random);
            }
        }
        [HttpPost("Register")]
        public async Task<ActionResult<TbUser>> Register(RegisterModel register)
        {
            var regis = await _context.TbUsers.FirstOrDefaultAsync(o => o.Email == register.Email);
            if(regis != null)
            {
                return Ok(new ErrorRespon
                {
                    Error = false,
                    Message ="Email đã tồn tại "
                });
            }
            var user = new TbUser
            {
                
                Gender = register.Gender,
                Email = register.Email,
                Name = register.Name,
                Password = register.Password,
                RoleId = register.RoleId,
               

            };
            await _context.TbUsers.AddAsync(user);
            await _context.SaveChangesAsync();
            var mailRequest = new MailRequest()
            {
                ToEmail = register.Email,
                Subject = "[BIRD TRADING PLATFORM] Thông tin đăng kí của bạn",
                Body = $"Thông tin đăng kí tài khoản của bạn:" +
               $"-Email: {user.Email} " +
               $" -Password: {user.Password} " +
               $" -Name: {user.Name} " +
               $" -Gender: {user.Gender} , Hãy đăng nhập và trải nghiệm"
            };

            await _mailService.SendEmailAsync(mailRequest);
            return Ok(new ErrorRespon
            {
                Error = false,
                Message = "Regiter Success, Please Check your email"
            });
        }



        [HttpPut]
        [Route("Add_User_Image")]

        public async Task<IActionResult> AddUserImage([FromForm] CreateAvatarUserVm request, int userId)
        {
            APIResponse response = new APIResponse();
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {


                string Filepath = GetFileUserPath(userId);
                if (!System.IO.Directory.Exists(Filepath))
                {
                    System.IO.Directory.CreateDirectory(Filepath);
                }

                string imagepath = Path.Combine(Filepath, userId + ".png");
                if (System.IO.File.Exists(imagepath))
                {
                    System.IO.File.Delete(imagepath);
                }
                using (FileStream stream = System.IO.File.Create(imagepath))
                {
                    await request.Avatar.CopyToAsync(stream);
                    response.ResponseCode = 200;
                    response.Result = "pass";

                }
                var user = await _context.TbUsers.FindAsync(userId);
                if (user != null)
                {
                    string avatarPath = GetImageUserPath(userId);

                    user.Avatar = avatarPath;
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                    response.Result = avatarPath;
                }
                else
                {
                    response.Errormessage = "User not found.";
                    return NotFound(response);
                }

            }
            catch (Exception ex)
            {
                response.Errormessage = ex.Message;

            }
            return Ok(response);
        }


        [HttpGet("Image_ShopID")]
        public async Task<IActionResult> GetImageByUserId(int userId)
        {
            //   var image = await _context.TbImages.FindAsync(productId);
            var tb_User = await _context.TbUsers.Where(x => x.UserId == userId).FirstOrDefaultAsync();
            var shop = await _context.TbShops.Where(x => x.UserId == userId).FirstOrDefaultAsync();
            //      if (image == null)

            //          throw new Exception("can not find an image with id");

            string hosturl = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}";
            try
            {

                string Filepath = GetFileUserPath(userId);
                string imageUrl = Path.Combine(Filepath, userId + ".png");
                if (System.IO.File.Exists(imageUrl))
                {
                    var DetailShop = new DetailShopViewProduct()
                    {

                        ShopId = shop.ShopId,
                        ShopName = shop.ShopName,
                        Avatar = hosturl + "/user-content/user/" + userId + "/" + userId + ".png"

                    };
                    return Ok(DetailShop);
                }



            }
            catch (Exception ex)
            {

            }
            return NotFound();


        }

        private string GetImageUserPath(int userId)
        {
            string hosturl = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}";
            return hosturl + "/user-content/user/" + userId + "/" + userId + ".png";

        }

        private string GetFileUserPath(int userId)
        {
            return this._enviroment.WebRootPath + "\\user-content\\user\\" + userId.ToString();
        }
        [HttpGet("Meee")]
        public async Task<IActionResult> GetMyyy()
        {

            var userClaim = User.Claims.FirstOrDefault(x => x.Type == "Username");
            var emailClaim = User.Claims.FirstOrDefault(z => z.Type == "Email");
            var genderClaim = User.Claims.FirstOrDefault(g => g.Type == "Gender");
            var addressClaim = User.Claims.FirstOrDefault(a => a.Type == "Address");
            var phoneClaim = User.Claims.FirstOrDefault(p => p.Type == "Phone");
            var avatarClaim = User.Claims.FirstOrDefault(av => av.Type == "Avatar");
            if (userClaim == null || emailClaim == null)
            {
                return Ok(new ErrorRespon
                {
                    Message = "Token invalid"
                });
            }

            string username = userClaim.Value;
            string email = emailClaim.Value;
            string gender = genderClaim.Value;
            string address = addressClaim.Value;
            string phone = phoneClaim.Value;
            string avatar = avatarClaim.Value;
            var account = getAccount(email);
            return Ok(account);

        }
        private async Task<ViewAccount> getAccount(string Email)
        {
            var user = _context.TbUsers.FirstOrDefault(x => x.Email == Email);
            if (user == null)
            {
                return null;
            }
            var accountModel = new ViewAccount
            {
                Dob = (DateTime)user.Dob.GetValueOrDefault(),
                Email = user.Email,
                userName = user.Name,
                Gender = user.Gender,
                Address = user.Address ?? "",
                Phone = user.Phone ?? "",
                avatar = user.Avatar ?? "",
                

            };

            return accountModel;
        }
        [HttpPut("UpdateMee")]
        public async Task<IActionResult> UpdateMee([FromForm]UserModel model)
        {
            var userIdClaim = User.Claims.FirstOrDefault(x => x.Type == "UserId");
            if (userIdClaim == null)
            {
                return Unauthorized();
            }
            int userId = int.Parse(userIdClaim.Value);
            var user = await _context.TbUsers.FirstOrDefaultAsync(x => x.UserId == userId);
            if (user == null)
            {
                return Ok("User Not Login");
            }
            if (model.Dob.HasValue)
            {
                user.Dob = model.Dob.Value;
            }
            if (!string.IsNullOrEmpty(model.Name))
            {
                user.Name = model.Name;
            }
            if (!string.IsNullOrEmpty(model.Address))
            {
                user.Address = model.Address;
            }
            if (!string.IsNullOrEmpty(model.Phone))
            {
                user.Phone = model.Phone;
            }
            if (!string.IsNullOrEmpty(model.Gender))
            {
                user.Gender = model.Gender;
            }
            if(model.avatar != null && model.avatar.Length > 0)
            {
                string avatarPath = await SaveAvatarFile(userId, model.avatar);
                user.Avatar = avatarPath;
            }
            try
            {
                await _context.SaveChangesAsync();
                return Ok("Success");
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "Failed to update account" });
            }
        }
        private async Task<string> SaveAvatarFile(int userId, IFormFile avatarFile)
        {
            string fileDirectory = GetFileUserPath(userId);
            if (!Directory.Exists(fileDirectory))
            {
                Directory.CreateDirectory(fileDirectory);
            }
            string filePath = Path.Combine(fileDirectory, userId + ".png");           
            if (System.IO.Directory.Exists(filePath))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(filePath);
                FileInfo[] fileInfos = directoryInfo.GetFiles();
                foreach (FileInfo fileInfo in fileInfos)
                {
                    fileInfo.Delete();
                }

            }            
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            {
                await avatarFile.CopyToAsync(stream);
            }

            return GetImageUserPath(userId);
        }
        [HttpPost("ChangePassword")]
        public async Task<IActionResult> ChangePassword(PasswordModel model)
        {
          
                var userIdClaim = User.Claims.FirstOrDefault(x => x.Type == "UserId");
                if (userIdClaim == null)
                {
                    return Unauthorized();
                }
                int userId = int.Parse(userIdClaim.Value);
                var user = await _context.TbUsers.FirstOrDefaultAsync(x => x.UserId == userId);
                if (user == null)
                {
                    return Ok("User Not Logged In");
                }
                if(model.OldPassword != user.Password)
            {
                return BadRequest(new { Message = "Incorrect old password" });
            }
                if(model.NewPassword != model.ConfirmNewPassword)
            {
                return BadRequest(new { Message = "New passwords do not match" });
            }
            user.Password = model.NewPassword;
                try
                {
                    await _context.SaveChangesAsync();
                    return Ok(new { Message = "Password changed successfully" });
                }
                catch (Exception ex)
                {
                    return BadRequest(new { Message = "Failed to change password" });
                }
            

        }
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPassword reset)
        {
            try
            {
                var user = await _context.TbUsers.FirstOrDefaultAsync(u => u.Email == reset.Email);
                if (user == null)
                {
                    return BadRequest("Địa chỉ email không hợp lệ.");
                }

                string newPassword = "abc12345";
                user.Password = newPassword;
                await _context.SaveChangesAsync();

                var mailRequest = new MailRequest()
                {
                    ToEmail = reset.Email,
                    Subject = "[BIRD TRADING PLATFORM] Thông tin mật khẩu mới",
                    Body = $"Mật khẩu mới của bạn là: {newPassword}, Vui lòng đăng nhập và đổi mật khẩu"
                };

                await _mailService.SendEmailAsync(mailRequest);

                return Ok("Mật khẩu đã được đặt lại. Vui lòng kiểm tra email để lấy mật khẩu mới.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Đã xảy ra lỗi trong quá trình xử lý yêu cầu.");
            }
        }


    }

}

