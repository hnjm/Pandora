﻿using Microsoft.EntityFrameworkCore.Query;
using Pandora.Application.DTOs.CategoryDTOs;
using Pandora.Application.Utilities.Results.Interfaces;
using Pandora.Core.Domain.Entities;
using Pandora.Core.Persistence.Paging;
using System.Linq.Expressions;

namespace Pandora.Application.Interfaces;

public interface ICategoryService
{
    Task<CategoryDto?> GetAsync(
    Expression<Func<Category, bool>> predicate,
    Func<IQueryable<Category>, IIncludableQueryable<Category, object>>? include = null,
    bool withDeleted = false,
    bool enableTracking = true,
    CancellationToken cancellationToken = default);
    Task<Paginate<CategoryDto>?> GetListAsync(
      Expression<Func<Category, bool>>? predicate = null,
      Func<IQueryable<Category>, IOrderedQueryable<Category>>? orderBy = null,
      Func<IQueryable<Category>, IIncludableQueryable<Category, object>>? include = null,
      int index = 0,
      int size = 10,
      bool withDeleted = false,
      bool enableTracking = true,
      CancellationToken cancellationToken = default);
    Task<IDataResult<CategoryDto>> AddAsync(CategoryAddDto dto, CancellationToken cancellationToken);
    Task<IDataResult<CategoryDto>> UpdateAsync(CategoryUpdateDto dto, CancellationToken cancellationToken);
    Task<IResult> DeleteAsync(Guid categoryId, CancellationToken cancellationToken);
    Task<CategoryDto> GetByIdAsync(Guid categoryId, CancellationToken cancellationToken);
    Task<CategoryDto> GetByIdAndUserAsync(Guid categoryId, Guid userId, CancellationToken cancellationToken);
    Task<List<CategoryDto>> GetAllAsync(CancellationToken cancellationToken, bool withDeleted = false);
    Task<List<CategoryDto>> GetAllByUserAsync(Guid userId, CancellationToken cancellationToken, bool withDeleted = false);
}
