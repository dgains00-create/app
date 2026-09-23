using DMO.Domain.Tools;

namespace DMO.Domain.Controlo;

/// <summary>
/// The closed set of the Beta's three document output families for email templates.
/// </summary>
/// <remarks>
/// Authority: P2-T05 contract §14.2 and <c>DOCUMENTS_AND_FILES.md</c> §2. <c>document_type</c> is
/// <c>NULL</c> (= template not bound to a specific output, generic) or one of the three families;
/// any other value is refused with <c>DOCUMENT_TYPE_UNKNOWN</c> (Q-DOCTYPE).
/// <para>
/// The tokens are configuration values, not identity facts: they never become a document identity
/// and never activate a generation/send behavior (P2-T08 owns generation and transport).
/// </para>
/// </remarks>
public enum EmailTemplateDocumentType
{
    /// <summary>Peso output family.</summary>
    Peso,

    /// <summary>Pegamentos output family.</summary>
    Pegamentos,

    /// <summary>Resumo output family.</summary>
    Resumo,
}

/// <summary>
/// The exact stored ASCII tokens of the email-template document types.
/// </summary>
public static class EmailTemplateDocumentTypeTokens
{
    /// <summary>The stored <c>document_type</c> token of a document type.</summary>
    public static string ToToken(EmailTemplateDocumentType type) => type switch
    {
        EmailTemplateDocumentType.Peso => "peso",
        EmailTemplateDocumentType.Pegamentos => "pegamentos",
        EmailTemplateDocumentType.Resumo => "resumo",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown document type."),
    };

    /// <summary>Parses a stored <c>document_type</c> token, or <c>null</c> when it is not settled.</summary>
    public static EmailTemplateDocumentType? Parse(string? token) => token switch
    {
        "peso" => EmailTemplateDocumentType.Peso,
        "pegamentos" => EmailTemplateDocumentType.Pegamentos,
        "resumo" => EmailTemplateDocumentType.Resumo,
        _ => null,
    };
}