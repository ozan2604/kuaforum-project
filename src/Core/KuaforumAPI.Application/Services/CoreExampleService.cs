using KuaforumAPI.Application.DTOs;
using FluentValidation;
using KuaforumAPI.Application.Interfaces.Repositories;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KuaforumAPI.Application.Services
{
    public class CoreExampleService : ICoreExampleService
    {
        private readonly ICoreExampleRepository _repository;
        private readonly IValidator<CreateCoreExampleDto> _validator;

        public CoreExampleService(ICoreExampleRepository repository, IValidator<CreateCoreExampleDto> validator)
        {
            _repository = repository;
            _validator = validator;
        }

        public async Task<CoreExampleDto> CreateAsync(CreateCoreExampleDto createDto)
        {
            await _validator.ValidateAndThrowAsync(createDto);

            var entity = new CoreExample
            {
                Name = createDto.Name,
                Description = createDto.Description
            };

            await _repository.AddAsync(entity);

            return new CoreExampleDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity != null)
            {
                await _repository.DeleteAsync(entity);
            }
        }

        public async Task<IEnumerable<CoreExampleDto>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return entities.Select(e => new CoreExampleDto
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            });
        }

        public async Task<CoreExampleDto> GetByIdAsync(Guid id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return null;

            return new CoreExampleDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        public async Task UpdateAsync(Guid id, CreateCoreExampleDto updateDto)
        {
            await _validator.ValidateAndThrowAsync(updateDto);

            var entity = await _repository.GetByIdAsync(id);
            if (entity != null)
            {
                entity.Name = updateDto.Name;
                entity.Description = updateDto.Description;
                await _repository.UpdateAsync(entity);
            }
        }
    }
}
