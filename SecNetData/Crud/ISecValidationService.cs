using System;

namespace SecNetData.Crud;

/// <summary>
/// Service for validating entity objects against DataAnnotations.
/// </summary>
public interface ISecValidationService
{
    /// <summary>
    /// Validates an object instance against all DataAnnotations attributes.
    /// </summary>
    /// <param name="entity">The object to validate.</param>
    /// <param name="errors">List of validation error messages.</param>
    /// <returns>True if valid, false otherwise.</returns>
    bool Validate(object entity, out List<string> errors);
}
