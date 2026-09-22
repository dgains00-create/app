using System.Text.RegularExpressions;

namespace DMO.Application.Access;

/// <summary>
/// Strong, immutable, code-owned identity of one assignable Module.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="ModuleId"/> is the stable access identity persisted in
/// <c>template_modules.module_id</c>. It is never derived from display text: equality is
/// ordinal over <see cref="Value"/>, and <see cref="ModuleCatalog"/> is the single source of
/// the canonical vocabulary.
/// </para>
/// <para>
/// IDs are stable ASCII kebab-case (<c>controlo-approve</c>, <c>job-on-view</c>, …);
/// accents/spaces exist only in presentation names. There is no implicit conversion from an
/// arbitrary <see cref="string"/> so identities cannot be created by accident:
/// <see cref="From"/> (code-owned constants) and <see cref="TryParse"/> (persisted values)
/// are the only construction paths.
/// </para>
/// </remarks>
/// <param name="Value">The canonical ASCII kebab-case Module identity.</param>
public readonly record struct ModuleId(string Value)
{
    private static readonly Regex KebabCase = new(
        "^[a-z0-9]+(-[a-z0-9]+)*$",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    /// <summary>
    /// Creates a <see cref="ModuleId"/> from a canonical kebab-case value.
    /// </summary>
    /// <param name="value">The Module identity value.</param>
    /// <exception cref="ArgumentException">
    /// When <paramref name="value"/> is not a non-empty ASCII kebab-case string.
    /// </exception>
    public static ModuleId From(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        // Strict, exact-match validation: identity values are code-owned constants; a value
        // that does not match the canonical shape is rejected, never silently normalized.
        if (!KebabCase.IsMatch(value))
        {
            throw new ArgumentException(
                $"Module id '{value}' is not a valid kebab-case identity ([a-z0-9]+(-[a-z0-9]+)*).",
                nameof(value));
        }

        return new ModuleId(value);
    }

    /// <summary>
    /// Attempts to parse a persisted value into a <see cref="ModuleId"/>.
    /// </summary>
    /// <param name="value">The persisted Module identity value, or <c>null</c>.</param>
    /// <param name="id">The parsed identity when the value is valid.</param>
    /// <returns><c>true</c> when the value is a valid kebab-case identity; otherwise <c>false</c>.</returns>
    public static bool TryParse(string? value, out ModuleId id)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            id = default;
            return false;
        }

        // Strict, exact-match validation: a malformed persisted value fails closed and is
        // reported to the resolver as Unknown — never silently normalized into an identity.
        if (!KebabCase.IsMatch(value))
        {
            id = default;
            return false;
        }

        id = new ModuleId(value);
        return true;
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}