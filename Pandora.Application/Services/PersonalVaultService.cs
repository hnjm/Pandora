﻿using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Pandora.Application.DTOs.PersonalVaultDTOs;
using Pandora.Application.Interfaces;
using Pandora.Application.Interfaces.Repositories;
using Pandora.Application.Security.Interfaces;
using Pandora.Application.Utilities.Results.Implementations;
using Pandora.Application.Utilities.Results;
using Pandora.Application.Utilities.Results.Interfaces;
using Pandora.Core.Domain.Entities;
using Pandora.Core.Persistence.Paging;
using System.Linq.Expressions;
using Pandora.CrossCuttingConcerns.ExceptionHandling;
using Pandora.Application.Validators.PersonalVaultValidators;
using Microsoft.Extensions.Logging;

namespace Pandora.Application.Services;

public class PersonalVaultService : IPersonalVaultService
{
    private readonly IPersonalVaultRepository _personalVaultRepository;
    private readonly PersonalVaultAddDtoValidator _personalVaultAddDtoValidator;
    private readonly PersonalVaultUpdateDtoValidator _personalVaultUpdateDtoValidator;
    private readonly IMapper _mapper;
    private readonly IHasher _hasher;
    private readonly IEncryption _encryption;
    private readonly ILogger<PersonalVaultService> _logger;

    public PersonalVaultService(IPersonalVaultRepository personalVaultRepository, IMapper mapper, IHasher hasher, IEncryption encryption,
        PersonalVaultAddDtoValidator personalVaultAddDtoValidator, ILogger<PersonalVaultService> logger, PersonalVaultUpdateDtoValidator personalVaultUpdateDtoValidator)
    {
        _personalVaultRepository = personalVaultRepository;
        _mapper = mapper;
        _hasher = hasher;
        _encryption = encryption;
        _personalVaultAddDtoValidator = personalVaultAddDtoValidator;
        _logger = logger;
        _personalVaultUpdateDtoValidator = personalVaultUpdateDtoValidator;
    }

    public async Task<IDataResult<PersonalVaultDto>> AddAsync(PersonalVaultAddDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var validationResult = await _personalVaultAddDtoValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                return new DataResult<PersonalVaultDto>(ResultStatus.Error, "Validation Error: " +
                    string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)), null);
            }

            var personalVault = _mapper.Map<PersonalVault>(dto);

            EncryptFields(personalVault, dto);

            await _personalVaultRepository.AddAsync(personalVault, cancellationToken);

            var resultDto = _mapper.Map<PersonalVaultDto>(personalVault);
            return new DataResult<PersonalVaultDto>(ResultStatus.Success, "Personal vault added successfully", resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {MethodName}. Failed to add personal vault. Details: {ExceptionMessage}", nameof(AddAsync), ex.Message);
            return new DataResult<PersonalVaultDto>(ResultStatus.Error, "An error occurred while adding the personal vault.", null);
        }
    }


    public async Task<IDataResult<PersonalVaultDto>> UpdateAsync(PersonalVaultUpdateDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var validationResult = await _personalVaultUpdateDtoValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                return new DataResult<PersonalVaultDto>(ResultStatus.Error, "Doğrulama hatası: " +
                    string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)), null);
            }

            var personalVault = await _personalVaultRepository.GetAsync(x => x.Id == dto.Id, cancellationToken: cancellationToken);
            if (personalVault == null)
                return new DataResult<PersonalVaultDto>(ResultStatus.Error, "Personal vault not found.", null);

            _mapper.Map(dto, personalVault);

            EncryptFields(personalVault, dto);

            personalVault.LastModifiedDate = DateTime.UtcNow;
            await _personalVaultRepository.UpdateAsync(personalVault, cancellationToken);

            var resultDto = _mapper.Map<PersonalVaultDto>(personalVault);
            return new DataResult<PersonalVaultDto>(ResultStatus.Success, "Personal vault updated successfully", resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {MethodName}. Failed to update personal vault. Details: {ExceptionMessage}", nameof(UpdateAsync), ex.Message);
            return new DataResult<PersonalVaultDto>(ResultStatus.Error, "An error occurred while updating the personal vault.", null);
        }
    }


    public async Task<IResult> DeleteAsync(Guid personalVaultId, CancellationToken cancellationToken)
    {
        try
        {
            var personalVault = await _personalVaultRepository.GetAsync(x => x.Id == personalVaultId, cancellationToken: cancellationToken);

            if (personalVault == null)
                return new Result(ResultStatus.Warning, "Personal vault not found.");

            await _personalVaultRepository.DeleteAsync(personalVault, cancellationToken: cancellationToken);

            return new Result(ResultStatus.Success, "Personal vault successfully deleted.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {MethodName}. Failed to delete personal vault. Details: {ExceptionMessage}", nameof(DeleteAsync), ex.Message);
            return new Result(ResultStatus.Error, "Failed to delete personal vault.", ex);
        }
    }

    public async Task<List<PersonalVaultDto>> GetAllAsync(CancellationToken cancellationToken, bool withDeleted = false)
    {
        try
        {
            var personalVaults = await _personalVaultRepository.GetListAsync(cancellationToken: cancellationToken);
            if (personalVaults == null || !personalVaults.Items.Any())
            {
                _logger.LogError("Error in {MethodName}. Failed to get all personal vaults.", nameof(GetAllAsync));
                return null;
            }

            foreach (var vault in personalVaults.Items)
            {
                DecryptFields(vault);
            }

            var resultDtos = _mapper.Map<List<PersonalVaultDto>>(personalVaults.Items);
            return resultDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {MethodName}. Failed to get all personal vaults. Details: {ExceptionMessage}", nameof(GetAllAsync), ex.Message);
            throw new BusinessException("An error occurred while fetching the personal vaults.");
        }
    }

    public async Task<List<PersonalVaultDto>> GetAllByUserAsync(Guid userId, CancellationToken cancellationToken, bool withDeleted = false)
    {
        try
        {
            var personalVaults = await _personalVaultRepository.GetListAsync(x => x.UserId == userId, cancellationToken: cancellationToken);
            if (personalVaults == null || !personalVaults.Items.Any())
            {
                _logger.LogError("Error in {MethodName}. Failed to get all personal vaults.", nameof(GetAllAsync));
                return null;
            }

            foreach (var vault in personalVaults.Items)
            {
                DecryptFields(vault);
            }

            var resultDtos = _mapper.Map<List<PersonalVaultDto>>(personalVaults.Items);
            return resultDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {MethodName}. Failed to get all personal vaults. Details: {ExceptionMessage}", nameof(GetAllAsync), ex.Message);
            throw new BusinessException("An error occurred while fetching the personal vaults.");
        }
    }

    public async Task<PersonalVaultDto?> GetAsync(
        Expression<Func<PersonalVault, bool>> predicate,
        Func<IQueryable<PersonalVault>, IIncludableQueryable<PersonalVault, object>>? include = null,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var personalVault = await _personalVaultRepository.GetAsync(predicate, include, withDeleted, enableTracking, cancellationToken);

            if (personalVault == null)
            {
                _logger.LogWarning("Personal vault not found.");
                return null;
            }

            DecryptFields(personalVault);

            return _mapper.Map<PersonalVaultDto>(personalVault);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {MethodName}. Failed to get personal vault. Details: {ExceptionMessage}", nameof(GetAsync), ex.Message);
            throw new BusinessException("An error occurred while retrieving the personal vault.");
        }
    }

    public async Task<PersonalVaultDto> GetByIdAsync(Guid personalVaultId, CancellationToken cancellationToken)
    {
        try
        {
            var personalVault = await _personalVaultRepository.GetAsync(x => x.Id == personalVaultId, cancellationToken: cancellationToken);
            if (personalVault == null)
                throw new BusinessException("Personal vault not found.");

            DecryptFields(personalVault);

            var resultDto = _mapper.Map<PersonalVaultDto>(personalVault);
            return resultDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {MethodName}. Failed to get personal vault by Id. Details: {ExceptionMessage}", nameof(GetByIdAsync), ex.Message);
            throw new BusinessException("An error occurred while fetching the personal vault.");
        }
    }

    public async Task<PersonalVaultDto> GetByIdAndUserAsync(Guid personalVaultId, Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var personalVault = await _personalVaultRepository.GetAsync(x => x.Id == personalVaultId && x.UserId == userId, cancellationToken: cancellationToken);
            if (personalVault == null)
                throw new BusinessException("Personal vault not found.");

            DecryptFields(personalVault);

            var resultDto = _mapper.Map<PersonalVaultDto>(personalVault);
            return resultDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {MethodName}. Failed to get personal vault by Id. Details: {ExceptionMessage}", nameof(GetByIdAsync), ex.Message);
            throw new BusinessException("An error occurred while fetching the personal vault.");
        }
    }

    public async Task<Paginate<PersonalVaultDto>?> GetListAsync(
        Expression<Func<PersonalVault, bool>>? predicate = null,
        Func<IQueryable<PersonalVault>, IOrderedQueryable<PersonalVault>>? orderBy = null,
        Func<IQueryable<PersonalVault>, IIncludableQueryable<PersonalVault, object>>? include = null,
        int index = 0,
        int size = 10,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var personalVaults = await _personalVaultRepository.GetListAsync(predicate, orderBy, include, index, size, withDeleted, enableTracking, cancellationToken);

            foreach (var vault in personalVaults.Items)
            {
                DecryptFields(vault);
            }

            return _mapper.Map<Paginate<PersonalVaultDto>>(personalVaults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {MethodName}. Failed to get personal vault list. Details: {ExceptionMessage}", nameof(GetListAsync), ex.Message);
            throw new BusinessException("An error occurred while retrieving the list of personal vaults.");
        }
    }

    private void EncryptFields<T>(PersonalVault personalVault, T dto) where T : IPersonalVaultDto
    {
        personalVault.SecureSummary = _encryption.Encrypt(dto.Summary);
        personalVault.SecureTitle = _encryption.Encrypt(dto.Title);
        personalVault.SecureContent = _encryption.Encrypt(dto.Content);
        personalVault.SecureUrl = _encryption.Encrypt(dto.Url);
        personalVault.SecureMediaFile = _encryption.Encrypt(dto.MediaFile);
        personalVault.SecureTags = dto.Tags.Select(tag => _encryption.Encrypt(tag)).ToList();
    }

    private void DecryptFields(PersonalVault personalVault)
    {
        personalVault.SecureSummary = _encryption.Decrypt(personalVault.SecureSummary);
        personalVault.SecureTitle = _encryption.Decrypt(personalVault.SecureTitle);
        personalVault.SecureContent = _encryption.Decrypt(personalVault.SecureContent);
        personalVault.SecureUrl = _encryption.Decrypt(personalVault.SecureUrl);
        personalVault.SecureMediaFile = _encryption.Decrypt(personalVault.SecureMediaFile);
        personalVault.SecureTags = personalVault.SecureTags.Select(tag => _encryption.Decrypt(tag)).ToList();
    }

}
