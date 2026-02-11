using KuaforumAPI.Application.DTOs.Auth;
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

namespace KuaforumAPI.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IUserAddressRepository _userAddressRepository;

        public AuthService(UserManager<ApplicationUser> userManager, 
                           SignInManager<ApplicationUser> signInManager,
                           IConfiguration configuration,
                           IUserAddressRepository userAddressRepository)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _userAddressRepository = userAddressRepository;
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
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
                Token = await GenerateJwtToken(user)
            };
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            var user = new ApplicationUser
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                UserName = request.UserName
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Registration failed: {errors}");
            }

            // Assign Default Role
            await _userManager.AddToRoleAsync(user, KuaforumAPI.Application.Constants.Roles.Customer);

            return new AuthResponse
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Token = await GenerateJwtToken(user)
            };
        }

        private async Task<string> GenerateJwtToken(ApplicationUser user)
        {
            var userRoles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.Now.AddDays(Convert.ToDouble(_configuration["Jwt:DurationInDays"]));

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
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new Exception("User not found");

            // Check Uniqueness
            if (request.Email != user.Email)
            {
                var userWithEmail = await _userManager.FindByEmailAsync(request.Email);
                if (userWithEmail != null && userWithEmail.Id != userId)
                {
                    throw new Exception("Email is already taken.");
                }
            }

            if (request.UserName != user.UserName)
            {
                var userWithUserName = await _userManager.FindByNameAsync(request.UserName);
                if (userWithUserName != null && userWithUserName.Id != userId)
                {
                    throw new Exception("Username is already taken.");
                }
            }

            // Phone number uniqueness check - Identity doesn't have FindByPhoneNumberAsync by default
            // We can scan or trust Identity's validation if configured, but let's check manually if possible.
            // Since we don't have direct access to users DbContext here easily (userManager abstracts it), 
            // we rely on UserManager.Users IQueryable if available, or just skip if expensive.
            // But user explicitly asked for it. 
            // _userManager.Users is IQueryable.
            if (request.PhoneNumber != user.PhoneNumber)
            {
                 // Assuming _userManager.Users works with EF Core provider
                 var userWithPhone = _userManager.Users.FirstOrDefault(u => u.PhoneNumber == request.PhoneNumber && u.Id != userId);
                 if (userWithPhone != null)
                 {
                     throw new Exception("Phone number is already taken.");
                 }
            }

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.PhoneNumber = request.PhoneNumber;
            user.UserName = request.UserName;
            user.Email = request.Email;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Profile update failed: {errors}");
            }

            return new AuthResponse
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Token = await GenerateJwtToken(user)
            };
        }

        public async Task ChangePasswordAsync(string userId, ChangePasswordDto request)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new Exception("User not found");

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Password change failed: {errors}");
            }
        }

        public async Task<List<AddressDto>> GetAddressesAsync(string userId)
        {
            var addresses = await _userAddressRepository.GetByUserIdAsync(userId);
            return addresses.Select(a => new AddressDto
            {
                Id = a.Id.ToString(), // GenericRepository uses Guid Id from BaseEntity
                Title = a.Title,
                City = a.City,
                District = a.District,
                OpenAddress = a.OpenAddress,
                Latitude = a.Latitude,
                Longitude = a.Longitude,
                IsDefault = a.IsDefault
            }).ToList();
        }

        public async Task<AddressDto> AddAddressAsync(string userId, CreateAddressDto request)
        {
            var address = new UserAddress
            {
                UserId = userId,
                Title = request.Title,
                City = request.City,
                District = request.District,
                OpenAddress = request.OpenAddress,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                IsDefault = false // Default handling logic can be added later
            };

            await _userAddressRepository.AddAsync(address);

            return new AddressDto
            {
                Id = address.Id.ToString(),
                Title = address.Title,
                City = address.City,
                District = address.District,
                OpenAddress = address.OpenAddress,
                Latitude = address.Latitude,
                Longitude = address.Longitude,
                IsDefault = address.IsDefault
            };
        }

        public async Task DeleteAddressAsync(string userId, string addressId)
        {
            if (!Guid.TryParse(addressId, out var guidAddressId))
            {
                 throw new Exception("Invalid address ID format.");
            }

            var address = await _userAddressRepository.GetByIdAsync(guidAddressId);
            if (address == null) throw new Exception("Address not found");

            if (address.UserId != userId)
            {
                throw new UnauthorizedAccessException("You can only delete your own addresses.");
            }

            await _userAddressRepository.DeleteAsync(address);
        }

        public async Task DeleteAccountAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new Exception("User not found");

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Account deletion failed: {errors}");
            }
        }
    }
}
