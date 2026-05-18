using FluentValidation;
using KuaforumAPI.Application.Constants;
using KuaforumAPI.Application.DTOs.Auth;
using Microsoft.Extensions.Logging;
using KuaforumAPI.Application.Exceptions;
using AppValidationException = KuaforumAPI.Application.Exceptions.ValidationException;
using KuaforumAPI.Application.Interfaces.Repositories;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Domain.Entities;
using KuaforumAPI.Domain.Enums;
using KuaforumAPI.Persistence.Contexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace KuaforumAPI.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IDateTimeService _dateTimeService;
        private readonly IImageService _imageService;
        private readonly IValidator<UpdateProfileDto> _updateProfileValidator;
        private readonly IValidator<ChangePasswordDto> _changePasswordValidator;
        private readonly ApplicationDbContext _context;
        private readonly ISmsService _smsService;
        private readonly ILogger<AuthService> _logger;

        private const int OtpExpiryMinutes = 3;
        private const int OtpMaxAttempts = 5;
        private const int OtpRateLimitCount = 3;
        private const int OtpRateLimitWindowMinutes = 10;

        public AuthService(UserManager<ApplicationUser> userManager,
                           SignInManager<ApplicationUser> signInManager,
                           IConfiguration configuration,
                           IDateTimeService dateTimeService,
                           IImageService imageService,
                           IValidator<UpdateProfileDto> updateProfileValidator,
                           IValidator<ChangePasswordDto> changePasswordValidator,
                           ApplicationDbContext context,
                           ISmsService smsService,
                           ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _dateTimeService = dateTimeService;
            _imageService = imageService;
            _updateProfileValidator = updateProfileValidator;
            _changePasswordValidator = changePasswordValidator;
            _context = context;
            _smsService = smsService;
            _logger = logger;
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            ApplicationUser user = null;

            // Check if Identifier is Email
            if (request.Identifier.Contains("@"))
            {
                user = await _userManager.FindByEmailAsync(request.Identifier);
            }
            else
            {
                // Check if Identifier is Phone
                // Normalization for UX: 
                // DB stores as "05XXXXXXXXX" (11 digits).
                // User might enter: "5XXXXXXXXX" (10 digits) or "905XXXXXXXXX" (12 digits).
                
                var potentialPhones = new List<string> { request.Identifier };
                
                // If 10 digits (e.g. 532...), try adding '0'
                if (request.Identifier.Length == 10 && !request.Identifier.StartsWith("0"))
                {
                    potentialPhones.Add("0" + request.Identifier);
                }
                
                // If 12 digits (e.g. 90532...), try removing '9' and replace with '0' -> actually just substring
                if (request.Identifier.Length == 12 && request.Identifier.StartsWith("90"))
                {
                     potentialPhones.Add("0" + request.Identifier.Substring(2));
                }

                // Check against any of these variations
                user = _userManager.Users.FirstOrDefault(u => potentialPhones.Contains(u.PhoneNumber));
            }

            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);

            if (!result.Succeeded)
            {
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            var refreshToken = await CreateRefreshTokenAsync(user.Id);

            return new AuthResponse
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                ProfileImageUrl = user.ProfileImageUrl,
                Token = await GenerateJwtToken(user),
                RefreshToken = refreshToken
            };
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // Uniqueness Check for Phone
            var existingUserWithPhone = _userManager.Users.FirstOrDefault(u => u.PhoneNumber == request.PhoneNumber);
            if (existingUserWithPhone != null)
            {
                throw new AppValidationException("Bu telefon numarası zaten kullanımda.");
            }

            var isSalonOwner = !string.IsNullOrEmpty(request.Role) && request.Role == KuaforumAPI.Application.Constants.Roles.SalonOwner;

            if (isSalonOwner && string.IsNullOrWhiteSpace(request.Email))
            {
                throw new AppValidationException("Salon sahipleri için e-posta zorunludur.");
            }

            var user = new ApplicationUser
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                UserName = request.PhoneNumber, // Map PhoneNumber as UserName
                PhoneNumber = request.PhoneNumber
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new AppValidationException($"Kayıt başarısız: {errors}");
            }

            // Assign Role
            var roleToAssign = isSalonOwner ? KuaforumAPI.Application.Constants.Roles.SalonOwner : KuaforumAPI.Application.Constants.Roles.Customer;
            await _userManager.AddToRoleAsync(user, roleToAssign);

            var refreshToken = await CreateRefreshTokenAsync(user.Id);

            return new AuthResponse
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                ProfileImageUrl = user.ProfileImageUrl,
                Token = await GenerateJwtToken(user),
                RefreshToken = refreshToken
            };
        }

        public async Task<AuthResponse> RefreshAsync(string refreshToken)
        {
            var token = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (token == null || token.IsRevoked || token.ExpiresAt <= _dateTimeService.Now)
                throw new UnauthorizedAccessException("Geçersiz veya süresi dolmuş oturum. Lütfen tekrar giriş yapın.");

            // Rotate: eski token'ı iptal et, yenisini oluştur
            token.IsRevoked = true;
            var newRefreshToken = await CreateRefreshTokenAsync(token.UserId);
            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                Id = token.User.Id,
                Email = token.User.Email,
                UserName = token.User.UserName,
                FirstName = token.User.FirstName,
                LastName = token.User.LastName,
                PhoneNumber = token.User.PhoneNumber,
                ProfileImageUrl = token.User.ProfileImageUrl,
                Token = await GenerateJwtToken(token.User),
                RefreshToken = newRefreshToken
            };
        }

        private async Task<string> CreateRefreshTokenAsync(string userId)
        {
            // Kullanıcının eski aktif token'larını iptal et
            var existingTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                .ToListAsync();
            foreach (var t in existingTokens) t.IsRevoked = true;

            var tokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var refreshTokenDays = Convert.ToDouble(_configuration["Jwt:RefreshTokenDurationInDays"] ?? "30");

            var newToken = new RefreshToken
            {
                Token = tokenValue,
                UserId = userId,
                ExpiresAt = _dateTimeService.Now.AddDays(refreshTokenDays),
                CreatedAt = _dateTimeService.Now
            };

            _context.RefreshTokens.Add(newToken);
            await _context.SaveChangesAsync();
            return tokenValue;
        }

        private async Task<string> GenerateJwtToken(ApplicationUser user)
        {
            var userRoles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.MobilePhone, user.PhoneNumber ?? "")
            };

            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = _dateTimeService.Now.AddDays(Convert.ToDouble(_configuration["Jwt:DurationInDays"] ?? "1"));

            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims,
                expires: expiry,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<AuthResponse> UpdateProfileAsync(string userId, UpdateProfileDto request)
        {
            var validation = await _updateProfileValidator.ValidateAsync(request);
            if (!validation.IsValid) throw new FluentValidation.ValidationException(validation.Errors);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new NotFoundException("Kullanıcı bulunamadı.");

            // Email uniqueness check (only if email is provided and changed)
            if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
            {
                var userWithEmail = await _userManager.FindByEmailAsync(request.Email);
                if (userWithEmail != null && userWithEmail.Id != userId)
                {
                    throw new AppValidationException("Bu e-posta adresi zaten kullanımda.");
                }
            }

            // Phone number uniqueness check
            if (request.PhoneNumber != user.PhoneNumber)
            {
                 var userWithPhone = _userManager.Users.FirstOrDefault(u => u.PhoneNumber == request.PhoneNumber && u.Id != userId);
                 if (userWithPhone != null)
                 {
                     throw new AppValidationException("Bu telefon numarası zaten kullanımda.");
                 }
            }

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.PhoneNumber = request.PhoneNumber;
            user.UserName = request.PhoneNumber; // UserName always mirrors PhoneNumber
            user.Email = request.Email;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new AppValidationException($"Profil güncellenemedi: {errors}");
            }

            return new AuthResponse
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                ProfileImageUrl = user.ProfileImageUrl,
                Token = await GenerateJwtToken(user)
            };
        }

        public async Task ChangePasswordAsync(string userId, ChangePasswordDto request)
        {
            var validation = await _changePasswordValidator.ValidateAsync(request);
            if (!validation.IsValid) throw new FluentValidation.ValidationException(validation.Errors);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new NotFoundException("Kullanıcı bulunamadı.");

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new AppValidationException($"Şifre değiştirilemedi: {errors}");
            }

            try
            {
                if (user.PhoneNumber != null)
                    await _smsService.SendSmsAsync(user.PhoneNumber, SmsTemplates.PasswordChanged());
            }
            catch (Exception ex) { _logger.LogWarning(ex, "SMS gönderilemedi (ana işlem etkilenmedi)."); }
        }



        public async Task DeleteAccountAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new NotFoundException("Kullanıcı bulunamadı.");

            if (!string.IsNullOrEmpty(user.ProfileImageUrl))
            {
                await _imageService.DeleteImageAsync(user.ProfileImageUrl);
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new AppValidationException($"Hesap silinemedi: {errors}");
            }
        }

        public async Task<string> UpdateProfileImageAsync(string userId, Microsoft.AspNetCore.Http.IFormFile image)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new NotFoundException("Kullanıcı bulunamadı.");

            // Delete old image if exists
            if (!string.IsNullOrEmpty(user.ProfileImageUrl))
            {
                await _imageService.DeleteImageAsync(user.ProfileImageUrl);
            }

            var imageUrl = await _imageService.UploadImageAsync(image, "profile_images");
            user.ProfileImageUrl = imageUrl;
            
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded) throw new AppValidationException("Profil fotoğrafı güncellenemedi.");

            return imageUrl;
        }

        public async Task DeleteProfileImageAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new NotFoundException("Kullanıcı bulunamadı.");

            if (!string.IsNullOrEmpty(user.ProfileImageUrl))
            {
                await _imageService.DeleteImageAsync(user.ProfileImageUrl);
                user.ProfileImageUrl = null;
                await _userManager.UpdateAsync(user);
            }
        }

        // ─── OTP: Login ───────────────────────────────────────────────────────────

        public async Task<SendOtpResponse> SendLoginOtpAsync(SendLoginOtpRequest request)
        {
            // Önce kimlik bilgilerini doğrula (telefon/şifre yanlışsa OTP bile gönderme)
            var user = await FindUserByPhoneAsync(request.PhoneNumber);
            if (user == null)
                throw new UnauthorizedAccessException("Telefon numarası veya şifre hatalı.");

            var passwordValid = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (!passwordValid.Succeeded)
                throw new UnauthorizedAccessException("Telefon numarası veya şifre hatalı.");

            await CheckOtpRateLimitAsync(request.PhoneNumber, OtpPurpose.Login);
            await InvalidateExistingOtpsAsync(request.PhoneNumber, OtpPurpose.Login);

            var code = GenerateOtpCode();
            var otpEntry = new OtpCode
            {
                PhoneNumber = request.PhoneNumber,
                CodeHash = HashOtp(code),
                Purpose = OtpPurpose.Login,
                ExpiresAt = _dateTimeService.Now.AddMinutes(OtpExpiryMinutes)
            };
            _context.OtpCodes.Add(otpEntry);
            await _context.SaveChangesAsync();

            await _smsService.SendSmsAsync(request.PhoneNumber,
                $"SALONBİR giriş kodunuz: {code}. Kod {OtpExpiryMinutes} dakika geçerlidir. Kimseyle paylaşmayın.");

            return new SendOtpResponse
            {
                Message = $"Doğrulama kodu {MaskPhone(request.PhoneNumber)} numarasına gönderildi.",
                ExpiresInSeconds = OtpExpiryMinutes * 60
            };
        }

        public async Task<AuthResponse> VerifyLoginOtpAsync(VerifyLoginOtpRequest request)
        {
            // Kimlik bilgilerini yeniden doğrula
            var user = await FindUserByPhoneAsync(request.PhoneNumber);
            if (user == null)
                throw new UnauthorizedAccessException("Telefon numarası veya şifre hatalı.");

            var passwordValid = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (!passwordValid.Succeeded)
                throw new UnauthorizedAccessException("Telefon numarası veya şifre hatalı.");

            await ValidateAndConsumeOtpAsync(request.PhoneNumber, request.OtpCode, OtpPurpose.Login);

            var refreshToken = await CreateRefreshTokenAsync(user.Id);
            return BuildAuthResponse(user, await GenerateJwtToken(user), refreshToken);
        }

        // ─── OTP: Register ────────────────────────────────────────────────────────

        public async Task<SendOtpResponse> SendRegisterOtpAsync(SendRegisterOtpRequest request)
        {
            // Telefon zaten kayıtlıysa hata ver
            var existing = await FindUserByPhoneAsync(request.PhoneNumber);
            if (existing != null)
                 throw new AppValidationException("Bu telefon numarası zaten kullanımda.");

            var isSalonOwner = !string.IsNullOrEmpty(request.Role) &&
                               request.Role == KuaforumAPI.Application.Constants.Roles.SalonOwner;
            if (isSalonOwner && string.IsNullOrWhiteSpace(request.Email))
                throw new AppValidationException("Salon sahipleri için e-posta zorunludur.");

            // Şifre gücü ön kontrolü (Identity kuralları çerçevesinde)
            var tempUser = new ApplicationUser { UserName = request.PhoneNumber };
            foreach (var validator in _userManager.PasswordValidators)
            {
                var result = await validator.ValidateAsync(_userManager, tempUser, request.Password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new AppValidationException($"Şifre gereksinimleri karşılanmıyor: {errors}");
                }
            }

            await CheckOtpRateLimitAsync(request.PhoneNumber, OtpPurpose.Register);
            await InvalidateExistingOtpsAsync(request.PhoneNumber, OtpPurpose.Register);

            var code = GenerateOtpCode();
            var otpEntry = new OtpCode
            {
                PhoneNumber = request.PhoneNumber,
                CodeHash = HashOtp(code),
                Purpose = OtpPurpose.Register,
                ExpiresAt = _dateTimeService.Now.AddMinutes(OtpExpiryMinutes)
            };
            _context.OtpCodes.Add(otpEntry);
            await _context.SaveChangesAsync();

            await _smsService.SendSmsAsync(request.PhoneNumber,
                $"SALONBİR kayıt kodunuz: {code}. Kod {OtpExpiryMinutes} dakika geçerlidir. Kimseyle paylaşmayın.");

            return new SendOtpResponse
            {
                Message = $"Doğrulama kodu {MaskPhone(request.PhoneNumber)} numarasına gönderildi.",
                ExpiresInSeconds = OtpExpiryMinutes * 60
            };
        }

        public async Task<AuthResponse> VerifyRegisterOtpAsync(VerifyRegisterOtpRequest request)
        {
            // Telefon hâlâ boşta mı kontrol et (race condition güvenliği)
            var existing = await FindUserByPhoneAsync(request.PhoneNumber);
            if (existing != null)
                throw new AppValidationException("Bu telefon numarası zaten kullanımda.");

            await ValidateAndConsumeOtpAsync(request.PhoneNumber, request.OtpCode, OtpPurpose.Register);

            // Kullanıcıyı oluştur
            var isSalonOwner = !string.IsNullOrEmpty(request.Role) &&
                               request.Role == KuaforumAPI.Application.Constants.Roles.SalonOwner;
            var user = new ApplicationUser
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                UserName = request.PhoneNumber,
                PhoneNumber = request.PhoneNumber,
                PhoneNumberConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new AppValidationException($"Kayıt başarısız: {errors}");
            }

            var roleToAssign = isSalonOwner
                ? KuaforumAPI.Application.Constants.Roles.SalonOwner
                : KuaforumAPI.Application.Constants.Roles.Customer;
            await _userManager.AddToRoleAsync(user, roleToAssign);

            var refreshToken = await CreateRefreshTokenAsync(user.Id);
            return BuildAuthResponse(user, await GenerateJwtToken(user), refreshToken);
        }

        // ─── OTP: Şifre Sıfırlama ─────────────────────────────────────────────────

        public async Task<SendOtpResponse> SendForgotPasswordOtpAsync(SendForgotPasswordOtpRequest request)
        {
            var user = await FindUserByPhoneAsync(request.PhoneNumber);
            if (user == null)
                throw new AppValidationException("Bu telefon numarasına kayıtlı hesap bulunamadı.");

            await CheckOtpRateLimitAsync(request.PhoneNumber, OtpPurpose.PasswordReset);
            await InvalidateExistingOtpsAsync(request.PhoneNumber, OtpPurpose.PasswordReset);

            var code = GenerateOtpCode();
            var otpEntry = new OtpCode
            {
                PhoneNumber = request.PhoneNumber,
                CodeHash = HashOtp(code),
                Purpose = OtpPurpose.PasswordReset,
                ExpiresAt = _dateTimeService.Now.AddMinutes(OtpExpiryMinutes)
            };
            _context.OtpCodes.Add(otpEntry);
            await _context.SaveChangesAsync();

            await _smsService.SendSmsAsync(request.PhoneNumber,
                $"Şifre sıfırlama kodunuz: {code}. Kod {OtpExpiryMinutes} dakika geçerlidir. Kimseyle paylaşmayın.");

            return new SendOtpResponse
            {
                Message = $"Sıfırlama kodu {MaskPhone(request.PhoneNumber)} numarasına gönderildi.",
                ExpiresInSeconds = OtpExpiryMinutes * 60
            };
        }

        public async Task ResetPasswordWithOtpAsync(ResetPasswordWithOtpRequest request)
        {
            var user = await FindUserByPhoneAsync(request.PhoneNumber);
            if (user == null)
                throw new AppValidationException("Bu telefon numarasına kayıtlı hesap bulunamadı.");

            await ValidateAndConsumeOtpAsync(request.PhoneNumber, request.OtpCode, OtpPurpose.PasswordReset);

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new AppValidationException($"Şifre belirlenemedi: {errors}");
            }

            try
            {
                await _smsService.SendSmsAsync(user.PhoneNumber, SmsTemplates.PasswordChanged());
            }
            catch { }
        }

        // ─── Logout ───────────────────────────────────────────────────────────────

        public async Task LogoutAsync(string userId)
        {
            var tokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                .ToListAsync();
            foreach (var t in tokens) t.IsRevoked = true;
            if (tokens.Any())
                await _context.SaveChangesAsync();
        }

        // ─── OTP Yardımcı Metodlar ────────────────────────────────────────────────

        private static string GenerateOtpCode()
            => Random.Shared.Next(100000, 1000000).ToString("D6");

        private static string HashOtp(string code)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));

        private static string MaskPhone(string phone)
        {
            if (phone.Length < 7) return phone;
            return phone[..4] + "***" + phone[^4..];
        }

        private async Task<ApplicationUser?> FindUserByPhoneAsync(string phoneNumber)
        {
            var potentialPhones = new List<string> { phoneNumber };
            if (phoneNumber.Length == 10 && !phoneNumber.StartsWith("0"))
                potentialPhones.Add("0" + phoneNumber);
            if (phoneNumber.Length == 12 && phoneNumber.StartsWith("90"))
                potentialPhones.Add("0" + phoneNumber[2..]);

            return _userManager.Users.FirstOrDefault(u => potentialPhones.Contains(u.PhoneNumber));
        }

        private async Task CheckOtpRateLimitAsync(string phoneNumber, OtpPurpose purpose)
        {
            var windowStart = _dateTimeService.Now.AddMinutes(-OtpRateLimitWindowMinutes);
            var recentCount = await _context.OtpCodes
                .CountAsync(o => o.PhoneNumber == phoneNumber
                              && o.Purpose == purpose
                              && o.CreatedAt >= windowStart);

            if (recentCount >= OtpRateLimitCount)
                throw new AppValidationException(
                    $"Çok fazla OTP isteği. {OtpRateLimitWindowMinutes} dakika sonra tekrar deneyin.");
        }

        private async Task InvalidateExistingOtpsAsync(string phoneNumber, OtpPurpose purpose)
        {
            var active = await _context.OtpCodes
                .Where(o => o.PhoneNumber == phoneNumber
                         && o.Purpose == purpose
                         && !o.IsUsed
                         && o.ExpiresAt > _dateTimeService.Now)
                .ToListAsync();
            foreach (var otp in active) otp.IsUsed = true;
        }

        private async Task ValidateAndConsumeOtpAsync(string phoneNumber, string code, OtpPurpose purpose)
        {
            var otpEntry = await _context.OtpCodes
                .Where(o => o.PhoneNumber == phoneNumber
                         && o.Purpose == purpose
                         && !o.IsUsed
                         && o.ExpiresAt > _dateTimeService.Now)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpEntry == null)
                throw new AppValidationException("Geçerli bir doğrulama kodu bulunamadı. Lütfen yeni kod isteyin.");

            otpEntry.AttemptCount++;

            if (otpEntry.AttemptCount > OtpMaxAttempts)
            {
                otpEntry.IsUsed = true;
                await _context.SaveChangesAsync();
                throw new AppValidationException("Çok fazla hatalı deneme. Lütfen yeni kod isteyin.");
            }

            if (otpEntry.CodeHash != HashOtp(code))
            {
                await _context.SaveChangesAsync();
                var remaining = OtpMaxAttempts - otpEntry.AttemptCount;
                throw new AppValidationException(
                    remaining > 0
                        ? $"Doğrulama kodu hatalı. {remaining} deneme hakkınız kaldı."
                        : "Çok fazla hatalı deneme. Lütfen yeni kod isteyin.");
            }

            // Başarılı — kodu kullanıldı olarak işaretle
            otpEntry.IsUsed = true;
            await _context.SaveChangesAsync();
        }

        private static AuthResponse BuildAuthResponse(ApplicationUser user, string jwt, string refreshToken)
            => new AuthResponse
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                ProfileImageUrl = user.ProfileImageUrl,
                Token = jwt,
                RefreshToken = refreshToken
            };
    }
}
