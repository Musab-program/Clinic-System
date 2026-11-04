using Clinic.Application.Common.Models;
using System;
using System.Collections.Generic;

namespace Clinic.Application.Common.Interfaces;


public interface ITokenService
{
    GenerateTokenModel GenerateToken(
        Guid userId,
        string firstName,
        string accountStatus,
        IEnumerable<string> roles);

}
