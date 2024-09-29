﻿using Pandora.Core.Domain.Entities;

namespace Pandora.Application.DTOs.CategoryDTOs;

public class CategoryUpdateDto : BaseDto<Guid>
{
    public Guid? UserId { get; set; }
    public string Name { get; set; }  // Category name, e.g., Social Media, Work
    public string Description { get; set; }  // Optional category description
}