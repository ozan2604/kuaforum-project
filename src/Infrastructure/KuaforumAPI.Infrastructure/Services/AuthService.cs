using FluentValidation;
using KuaforumAPI.Application.DTOs.Auth;
using KuaforumAPI.Application.Exceptions;
using AppValidationException = KuaforumAPI.Application.Exceptions.ValidationException;
using KuaforumAPI.Application.Interfaces.Repositories;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using KuaforumAPI.Infrastructure.Services;
using KuaforumAPI.Application.Interfaces.Services;

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

        public AuthService(UserManager<ApplicationUser> userManager,
                           SignInManager<ApplicationUser> signInManager,
                           IConfiguration configuration,
                           IDateTimeService dateTimeService,
                           IImageService imageService,
                           IValidator<UpdateProfileDto> updateProfileValidator,
                           IValidator<ChangePasswordDto> changePasswordValidator)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _dateTimeService = dateTimeService;
            _imageService = imageService;
            _updateProfileValidator = updateProfileValidator;
            _changePasswordValidator = changePasswordValidator;
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

            return new AuthResponse
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber, // Map PhoneNumber
                ProfileImageUrl = user.ProfileImageUrl,
                Token = await GenerateJwtToken(user)
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
            var expiry = _dateTimeService.Now.AddDays(Convert.ToDouble(_configuration["Jwt:DurationInDays"]));

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
        }



        public async Task DeleteAccountAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new NotFoundException("Kullanıcı bulunamadı.");

            // Delete profile image if exists
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
    }
}
