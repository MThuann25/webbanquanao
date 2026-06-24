using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClothingShop.Domain.Entities;
using ClothingShop.Domain.Interfaces;
using ClothingShop.Application.Services;

namespace ClothingShop.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthApiController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly CartService _cartService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly EmailService _emailService;

        public AuthApiController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            CartService cartService,
            IUnitOfWork unitOfWork,
            EmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _cartService = cartService;
            _unitOfWork = unitOfWork;
            _emailService = emailService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                var roles = await _userManager.GetRolesAsync(user);

                // Migrate cart if items were passed in request
                if (model.CartItems != null && model.CartItems.Any())
                {
                    var itemsToMigrate = model.CartItems.Select(i => (i.ProductVariantId, i.Quantity)).ToList();
                    await _cartService.MigrateSessionCartToDbAsync(user.Id, itemsToMigrate);
                }

                return Ok(new
                {
                    message = "Đăng nhập thành công",
                    user = new
                    {
                        id = user.Id,
                        email = user.Email,
                        fullName = user.FullName,
                        avatarUrl = user.AvatarUrl,
                        points = user.Points,
                        roles = roles
                    }
                });
            }

            return BadRequest(new { message = "Email hoặc mật khẩu không hợp lệ." });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (model.Password != model.ConfirmPassword)
            {
                return BadRequest(new { message = "Mật khẩu xác nhận không khớp." });
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                CreatedDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");
                await _signInManager.SignInAsync(user, isPersistent: false);

                // Migrate cart
                if (model.CartItems != null && model.CartItems.Any())
                {
                    var itemsToMigrate = model.CartItems.Select(i => (i.ProductVariantId, i.Quantity)).ToList();
                    await _cartService.MigrateSessionCartToDbAsync(user.Id, itemsToMigrate);
                }

                return Ok(new
                {
                    message = "Đăng ký thành công",
                    user = new
                    {
                        id = user.Id,
                        email = user.Email,
                        fullName = user.FullName,
                        avatarUrl = user.AvatarUrl,
                        points = user.Points,
                        roles = new[] { "User" }
                    }
                });
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(new { message = errors });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new { message = "Đăng xuất thành công" });
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    return Ok(new
                    {
                        isAuthenticated = true,
                        user = new
                        {
                            id = user.Id,
                            email = user.Email,
                            fullName = user.FullName,
                            phoneNumber = user.PhoneNumber,
                            address = user.Address,
                            points = user.Points,
                            avatarUrl = user.AvatarUrl,
                            roles = roles
                        }
                    });
                }
            }

            return Ok(new { isAuthenticated = false });
        }

        [HttpPost("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] ProfileUpdateDto model)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound(new { message = "Không tìm thấy người dùng." });

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;
            if (!string.IsNullOrEmpty(model.AvatarUrl))
            {
                user.AvatarUrl = model.AvatarUrl;
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return Ok(new { message = "Cập nhật thông tin cá nhân thành công!" });
            }

            return BadRequest(new { message = "Không thể cập nhật hồ sơ cá nhân." });
        }

        [HttpGet("orders")]
        [Authorize]
        public async Task<IActionResult> GetOrders()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var orders = await _unitOfWork.Repository<Order>().GetQueryable()
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new
                {
                    id = o.Id,
                    orderDate = o.OrderDate.ToString("dd/MM/yyyy HH:mm"),
                    totalAmount = o.TotalAmount,
                    status = o.Status,
                    paymentMethod = o.PaymentMethod,
                    paymentStatus = o.PaymentMethod.Contains("Đã thanh toán") ? "Paid" : "Pending"
                })
                .ToListAsync();

            return Ok(orders);
        }

        [HttpGet("orders/{id}")]
        [Authorize]
        public async Task<IActionResult> GetOrderDetail(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var order = await _unitOfWork.Repository<Order>().GetQueryable()
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                            .ThenInclude(p => p.ProductImages)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null) return NotFound(new { message = "Không tìm thấy đơn hàng." });

            // Lấy danh sách reviews đã có cho đơn hàng này
            var reviews = await _unitOfWork.Repository<Review>().GetQueryable()
                .Where(r => r.OrderId == id && r.UserId == userId)
                .ToListAsync();

            var result = new
            {
                id = order.Id,
                orderDate = order.OrderDate.ToString("dd/MM/yyyy HH:mm"),
                totalAmount = order.TotalAmount,
                discountAmount = order.DiscountAmount,
                status = order.Status,
                shippingAddress = order.ShippingAddress,
                phoneNumber = order.User != null ? order.User.PhoneNumber : "",
                receiverName = order.User != null ? (order.User.FullName ?? order.User.UserName) : "",
                paymentMethod = order.PaymentMethod,
                paymentStatus = order.PaymentMethod.Contains("Đã thanh toán") ? "Paid" : "Pending",
                voucherCode = order.VoucherCode,
                items = order.OrderDetails.Select(od => {
                    var prodId = od.ProductVariant != null ? od.ProductVariant.ProductId : 0;
                    var isReviewed = reviews.Any(r => r.ProductId == prodId);
                    return new
                    {
                        id = od.Id,
                        productId = prodId,
                        productName = od.ProductVariant != null && od.ProductVariant.Product != null ? od.ProductVariant.Product.Name : "",
                        size = od.ProductVariant != null ? od.ProductVariant.Size : "",
                        color = od.ProductVariant != null ? od.ProductVariant.Color : "",
                        quantity = od.Quantity,
                        unitPrice = od.UnitPrice,
                        imageUrl = od.ProductVariant != null && od.ProductVariant.Product != null && od.ProductVariant.Product.ProductImages != null && od.ProductVariant.Product.ProductImages.Any(pi => pi.IsMain) 
                            ? od.ProductVariant.Product.ProductImages.First(pi => pi.IsMain).ImageUrl 
                            : "https://images.unsplash.com/photo-1523381210434-271e8be1f52b?w=400",
                        isReviewed = isReviewed
                    };
                }).ToList()
            };

            return Ok(result);
        }

        [HttpGet("externallogin")]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLogin([FromQuery] string provider, [FromQuery] string? returnUrl = null)
        {
            var schemes = await _signInManager.GetExternalAuthenticationSchemesAsync();
            if (!schemes.Any(s => s.Name.Equals(provider, StringComparison.OrdinalIgnoreCase)))
            {
                var targetUrl = returnUrl ?? "";
                var loginUrl = targetUrl.Contains("/profile.html") ? targetUrl.Replace("/profile.html", "/login.html") : "login.html";
                return Redirect($"{loginUrl}?error={Uri.EscapeDataString("Đăng nhập bằng Google hiện chưa hoạt động do thiếu Client ID và Client Secret thực tế trong appsettings.json ở Backend.")}");
            }

            var redirectUrl = Url.Action("ExternalLoginCallback", "AuthApi", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet("externallogincallback")]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback([FromQuery] string? returnUrl = null, [FromQuery] string? remoteError = null)
        {
            if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = "http://localhost:5500/profile.html"; // Default fallback
            }

            if (remoteError != null)
            {
                return Redirect($"{returnUrl}?error={Uri.EscapeDataString($"Lỗi từ Google: {remoteError}")}");
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return Redirect($"{returnUrl}?error={Uri.EscapeDataString("Không thể lấy thông tin đăng nhập Google.")}");
            }

            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                return Redirect($"{returnUrl}?loginSuccess=true&userId={user.Id}");
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
            {
                return Redirect($"{returnUrl}?error={Uri.EscapeDataString("Không tìm thấy email từ tài khoản Google của bạn.")}");
            }

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                var linkResult = await _userManager.AddLoginAsync(existingUser, info);
                if (linkResult.Succeeded)
                {
                    await _signInManager.SignInAsync(existingUser, isPersistent: false);
                    return Redirect($"{returnUrl}?loginSuccess=true&userId={existingUser.Id}");
                }
                return Redirect($"{returnUrl}?error={Uri.EscapeDataString("Không thể liên kết tài khoản Google với email hiện có.")}");
            }

            var newUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email,
                Address = "", // Tránh lỗi NOT NULL constraint của Postgres
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(newUser);
            if (createResult.Succeeded)
            {
                await _userManager.AddToRoleAsync(newUser, "User");
                var addLoginResult = await _userManager.AddLoginAsync(newUser, info);
                if (addLoginResult.Succeeded)
                {
                    await _signInManager.SignInAsync(newUser, isPersistent: false);
                    return Redirect($"{returnUrl}?loginSuccess=true&userId={newUser.Id}");
                }
            }

            return Redirect($"{returnUrl}?error={Uri.EscapeDataString("Có lỗi xảy ra khi tạo tài khoản từ Google.")}");
        }

        [HttpPost("redeem-voucher")]
        [Authorize]
        public async Task<IActionResult> RedeemVoucher([FromBody] RedeemVoucherDto model)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound(new { message = "Không tìm thấy người dùng." });

            int pointsCost = 0;
            decimal discountAmount = 0;
            string valueLabel = "";

            if (model.VoucherType == "50K")
            {
                pointsCost = 500;
                discountAmount = 50000;
                valueLabel = "50K";
            }
            else if (model.VoucherType == "100K")
            {
                pointsCost = 1000;
                discountAmount = 100000;
                valueLabel = "100K";
            }
            else
            {
                return BadRequest(new { message = "Loại quà tặng không hợp lệ." });
            }

            if (user.Points < pointsCost)
            {
                return BadRequest(new { message = $"Bạn không đủ điểm để đổi quà tặng này. Cần {pointsCost} điểm, bạn hiện có {user.Points} điểm." });
            }

            user.Points -= pointsCost;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return BadRequest(new { message = "Lỗi khi cập nhật điểm tích lũy." });
            }

            var randomCode = "DMT-" + valueLabel + "-" + Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
            
            var voucher = new Voucher
            {
                Code = randomCode,
                DiscountPercent = null,
                DiscountAmount = discountAmount,
                ExpiryDate = DateTime.UtcNow.AddDays(30),
                UsageLimit = 1,
                UsedCount = 0
            };

            await _unitOfWork.Repository<Voucher>().AddAsync(voucher);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new
            {
                message = $"Đổi quà tặng thành công! Mã Voucher của bạn là: {randomCode}",
                voucherCode = randomCode,
                points = user.Points
            });
        }

        [HttpPost("upload-avatar")]
        [Authorize]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Không tìm thấy file hợp lệ." });

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "avatars");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var url = $"/images/avatars/{uniqueFileName}";

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.AvatarUrl = url;
                await _userManager.UpdateAsync(user);
            }

            return Ok(new { url = url, message = "Tải ảnh đại diện lên thành công!" });
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return BadRequest(new { message = "Email không tồn tại trong hệ thống." });
            }

            // Sinh mã OTP 6 số
            var otpCode = new Random().Next(100000, 999999).ToString();
            user.OtpCode = otpCode;
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(5); // OTP có hiệu lực trong 5 phút

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new { message = "Không thể tạo mã xác thực." });
            }

            // Gửi email
            var emailBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #eee; border-radius: 5px;'>
                    <h2 style='color: #4F46E5; text-align: center;'>Yêu Cầu Đổi Mật Khẩu</h2>
                    <p>Chào bạn,</p>
                    <p>Chúng tôi nhận được yêu cầu thay đổi mật khẩu cho tài khoản của bạn. Vui lòng sử dụng mã OTP dưới đây để hoàn tất quá trình:</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <span style='font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #1E293B; background: #F1F5F9; padding: 10px 20px; border-radius: 5px;'>{otpCode}</span>
                    </div>
                    <p style='color: #6B7280; font-size: 14px;'>Mã xác nhận này sẽ hết hạn sau <b>5 phút</b>.</p>
                    <p style='color: #6B7280; font-size: 14px;'>Nếu bạn không yêu cầu đổi mật khẩu, vui lòng bỏ qua email này.</p>
                    <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;' />
                    <p style='text-align: center; color: #9CA3AF; font-size: 12px;'>Hệ thống cửa hàng thời trang Clothing Shop</p>
                </div>";

            var sendResult = await _emailService.SendEmailAsync(user.Email!, "Mã xác thực OTP đổi mật khẩu", emailBody);
            if (!sendResult)
            {
                return BadRequest(new { message = "Lỗi khi gửi email xác thực." });
            }

            return Ok(new { message = "Mã OTP đã được gửi về email của bạn (vui lòng kiểm tra hộp thư hoặc mục spam)." });
        }

        [HttpPost("reset-password-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPasswordOtp([FromBody] ResetPasswordOtpDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return BadRequest(new { message = "Email không tồn tại trong hệ thống." });
            }

            if (string.IsNullOrEmpty(user.OtpCode) || user.OtpCode != model.OtpCode)
            {
                return BadRequest(new { message = "Mã OTP không hợp lệ." });
            }

            if (!user.OtpExpiry.HasValue || user.OtpExpiry.Value < DateTime.UtcNow)
            {
                return BadRequest(new { message = "Mã OTP đã hết hạn." });
            }

            // Xoá OTP sau khi xác nhận thành công
            user.OtpCode = null;
            user.OtpExpiry = null;
            await _userManager.UpdateAsync(user);

            // Đổi mật khẩu
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (result.Succeeded)
            {
                return Ok(new { message = "Đổi mật khẩu thành công! Bạn có thể đăng nhập bằng mật khẩu mới." });
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(new { message = $"Không thể đổi mật khẩu: {errors}" });
        }
    }

    public class ForgotPasswordRequestDto
    {
        public string Email { get; set; } = null!;
    }

    public class ResetPasswordOtpDto
    {
        public string Email { get; set; } = null!;
        public string OtpCode { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }

    public class RedeemVoucherDto
    {
        public string VoucherType { get; set; } = null!;
    }

    public class LoginDto
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public bool RememberMe { get; set; }
        public List<CartItemInput>? CartItems { get; set; }
    }

    public class RegisterDto
    {
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string ConfirmPassword { get; set; } = null!;
        public List<CartItemInput>? CartItems { get; set; }
    }

    public class ProfileUpdateDto
    {
        public string FullName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string? AvatarUrl { get; set; }
    }

    public class CartItemInput
    {
        public int ProductVariantId { get; set; }
        public int Quantity { get; set; }
    }
}
