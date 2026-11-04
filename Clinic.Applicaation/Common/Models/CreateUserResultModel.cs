using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clinic.Application.Common.Models;

/// <summary>
/// Represents the result of an Identity user creation operation.
/// </summary>
public class CreateUserResultModel
{
    /// <summary>
    /// Gets or sets a value indicating whether user creation succeeded.
    /// </summary>
    public bool Succeeded { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the newly created user.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the list of errors encountered during user creation if it failed.
    /// </summary>
    public List<string> Errors { get; set; } = new();
}
