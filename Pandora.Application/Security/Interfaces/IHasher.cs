﻿using System.Security.Authentication;

namespace Pandora.Application.Security.Interfaces;

public interface IHasher
{
    string HashPassword(string password, HashAlgorithmType algorithmType); // SHA256 or SHA512
    bool VerifyHashedPassword(string hashedPassword, string plainTextPassword, HashAlgorithmType algorithmType);
}