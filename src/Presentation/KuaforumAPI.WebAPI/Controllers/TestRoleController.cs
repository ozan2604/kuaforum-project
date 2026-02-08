using KuaforumAPI.Application.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KuaforumAPI.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestRoleController : ControllerBase
    {
        [HttpGet("admin")]
        [Authorize(Roles = Roles.Admin)]
        public IActionResult GetAdminContent()
        {
            return Ok("You are seeing this because you are an Admin.");
        }

        [HttpGet("customer")]
        [Authorize(Roles = Roles.Customer)]
        public IActionResult GetCustomerContent()
        {
            return Ok("You are seeing this because you are a Customer.");
        }
        
        [HttpGet("everyone")]
        [Authorize]
        public IActionResult GetEveryoneContent()
        {
            return Ok("You are logged in.");
        }

        [HttpGet("salonowner")]
        [Authorize(Roles = Roles.SalonOwner)]
        public IActionResult GetSalonOwnerContent()
        {
            return Ok("You are seeing this because you are a Salon Owner.");
        }

        [HttpGet("employee")]
        [Authorize(Roles = Roles.Employee)]
        public IActionResult GetEmployeeContent()
        {
            return Ok("You are seeing this because you are an Employee.");
        }
    }
}
