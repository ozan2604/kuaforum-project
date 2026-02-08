using KuaforumAPI.Application.DTOs;
using KuaforumAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace KuaforumAPI.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CoreExamplesController : ControllerBase
    {
        private readonly ICoreExampleService _service;

        public CoreExamplesController(ICoreExampleService service)
        {
            _service = service;
        }



        [HttpGet]
        public async Task<ActionResult<IEnumerable<CoreExampleDto>>> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CoreExampleDto>> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<CoreExampleDto>> Create(CreateCoreExampleDto createDto)
        {
            var result = await _service.CreateAsync(createDto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, CreateCoreExampleDto updateDto)
        {
            await _service.UpdateAsync(id, updateDto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
    }
}
