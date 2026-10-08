namespace Roster.Plugins.Abstractions;

/// <summary>
/// Represents the result of validating a plugin's configuration.
/// </summary>
public sealed record ValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the configuration is valid.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the list of validation errors, if any.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    private ValidationResult(bool isValid, IReadOnlyList<string> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A successful validation result.</returns>
    public static ValidationResult Success() => new(true, Array.Empty<string>());

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <returns>A failed validation result.</returns>
    public static ValidationResult Failure(IEnumerable<string> errors) => new(false, errors.ToArray());

    /// <summary>
    /// Creates a failed validation result with a single error.
    /// </summary>
    /// <param name="error">The validation error.</param>
    /// <returns>A failed validation result.</returns>
    public static ValidationResult Failure(string error) => new(false, new[] { error });
}
