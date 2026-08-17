using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SecNetData.Crud;

/// <summary>
/// Validates objects using standard DataAnnotations.
/// </summary>
public sealed class SecValidationService : ISecValidationService
{
    public bool Validate(object entity, out List<string> errors)
    {
        errors = new List<string>();

        if (entity == null)
        {
            errors.Add("Entity cannot be null.");
            return false;
        }

        var context = new ValidationContext(entity, serviceProvider: null, items: null);
        var results = new List<ValidationResult>();

        bool isValid = Validator.TryValidateObject(
            entity,
            context,
            results,
            validateAllProperties: true);

        if (!isValid)
        {
            foreach (var result in results)
            {
                if (result.ErrorMessage != null)
                {
                    errors.Add(result.ErrorMessage);
                }
            }
        }

        return isValid;
    }
}
