using SecNetData.Configuration;

namespace SecNetData.Services;

/// <summary>
/// Validates SecOptions configuration at application startup.
/// Ensures all required configuration is present before the application continues.
/// </summary>
public sealed class SecOptionsValidator
{
    /// <summary>
    /// Validates the SecOptions configuration.
    /// Throws InvalidOperationException with clear error messages if validation fails.
    /// </summary>
    /// <param name="options">The SecOptions to validate.</param>
    /// <exception cref="InvalidOperationException">When required configuration is missing or invalid.</exception>
    public static void Validate(SecOptions options)
    {
        if (options == null)
            throw new InvalidOperationException("SecOptions cannot be null.");

        var errors = new List<string>();

        // Validate ApplicationName
        if (string.IsNullOrWhiteSpace(options.ApplicationName))
        {
            errors.Add("ApplicationName is required and cannot be empty.");
        }

        // Validate Database configuration
        if (options.Database == null)
        {
            errors.Add("Database configuration is required.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(options.Database.Server))
            {
                errors.Add("Database.Server is required and cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(options.Database.Username))
            {
                errors.Add("Database.Username is required and cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(options.Database.DatabaseName))
            {
                errors.Add("Database.DatabaseName is required and cannot be empty.");
            }

            // Port should have a sensible default (3306), so we don't require it
            // Password may be empty in some MySQL configurations, so we don't require it
        }

        // If there are any errors, throw with a formatted message
        if (errors.Count > 0)
        {
            var message = "SecOptions validation failed:\n" + string.Join("\n", errors.Select(e => "  - " + e));
            throw new InvalidOperationException(message);
        }
    }
}
