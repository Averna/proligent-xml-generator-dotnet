using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Proligent.XmlGenerator;

/// <summary>
/// Convenience helpers for building Datawarehouse payloads: time formatting,
/// UUID generation, and XML validation.
/// </summary>
public class Util
{
    private readonly string _schemaPath;

    /// <summary>Shared utility instance used when callers do not supply their own.</summary>
    public static Util Default { get; set; } = new Util();

    /// <summary>Delegate that generates UUID strings; override in tests to get deterministic values.</summary>
    public Func<string> UuidFactory { get; set; } = () => Guid.NewGuid().ToString();

    /// <summary>
    /// Configured time zone for naive DateTime values. When null, the machine time zone is used.
    /// </summary>
    public TimeZoneInfo? TimeZone { get; set; }

    /// <summary>
    /// Default folder where <see cref="Buildable.SaveXml" /> writes files when no destination is provided.
    /// Matches the Integration Service pickup directory used by Proligent deployments.
    /// </summary>
    public string DestinationDirectory { get; set; }

    /// <summary>Optional override path to the Datawarehouse XSD used during validation.</summary>
    public string SchemaPath => _schemaPath;

    /// <summary>
    /// Create a new utility container.
    /// </summary>
    /// <param name="timeZone">Optional time zone for naive DateTime inputs.</param>
    /// <param name="destinationDirectory">Optional default output directory for generated XML.</param>
    /// <param name="schemaPath">Optional path to a custom Datawarehouse XSD.</param>
    /// <param name="timeZoneId">IANA/Windows TimeZone IDs.</param>
    public Util(
        TimeZoneInfo? timeZone = null,
        string? destinationDirectory = null,
        string? schemaPath = null,
        string? timeZoneId = null
    )
    {
        TimeZone =
            timeZone
            ?? (timeZoneId is not null ? TimeZoneInfo.FindSystemTimeZoneById(timeZoneId) : null);
        DestinationDirectory =
            destinationDirectory ?? @"C:\Proligent\IntegrationService\Acquisition";
        _schemaPath =
            schemaPath ?? Path.Combine(AppContext.BaseDirectory, "Xsd", "Datawarehouse.xsd");
    }

    /// <summary>
    /// Convert a <see cref="DateTime" /> into the ISO-8601 string the Datawarehouse schema expects.
    /// Naive timestamps are localized using <see cref="TimeZone" /> or the machine time zone.
    /// </summary>
    /// <param name="dateTime">Time to serialize; defaults to now.</param>
    public string FormatDateTime(DateTime? dateTime = null)
    {
        var instant = dateTime ?? DateTime.Now;
        var targetZone = ResolveTimeZone();
        DateTimeOffset offsetTime;

        if (instant.Kind == DateTimeKind.Unspecified)
        {
            var offset = targetZone.GetUtcOffset(instant);
            offsetTime = new DateTimeOffset(instant, offset);
        }
        else
        {
            offsetTime = new DateTimeOffset(instant);
            offsetTime = TimeZoneInfo.ConvertTime(offsetTime, targetZone);
        }

        return offsetTime.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Convert a <see cref="DateTimeOffset" /> into the ISO-8601 string the Datawarehouse schema expects.
    /// </summary>
    public string FormatDateTime(DateTimeOffset dateTime)
    {
        var targetZone = ResolveTimeZone();
        var converted = TimeZoneInfo.ConvertTime(dateTime, targetZone);
        return converted.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
    }

    /// <summary>Generate a unique identifier suitable for Datawarehouse element IDs.</summary>
    public string Uuid() => UuidFactory();

    /// <summary>
    /// Generate a deterministic GUID from a string using SHA-256 and UTF-8.
    /// This method must stay in sync with the Python implementation.
    /// </summary>
    public static string GetDeterministicGuid(string inputText, Encoding? encoding = null)
    {
        ArgumentNullException.ThrowIfNull(inputText);

        // Kept for backward compatibility with older callers; UTF-8 is always used.
        _ = encoding;

        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(inputText));

        // Use first 16 bytes in network byte order and apply UUID v4/variant bits.
        Span<byte> uuidNetOrder = stackalloc byte[16];
        hashBytes.AsSpan(0, 16).CopyTo(uuidNetOrder);
        uuidNetOrder[6] = (byte)((uuidNetOrder[6] & 0x0F) | 0x40);
        uuidNetOrder[8] = (byte)((uuidNetOrder[8] & 0x3F) | 0x80);

        // Convert network-order UUID bytes to the little-endian layout expected by Guid(byte[]).
        Span<byte> guidBytes = stackalloc byte[16];
        guidBytes[0] = uuidNetOrder[3];
        guidBytes[1] = uuidNetOrder[2];
        guidBytes[2] = uuidNetOrder[1];
        guidBytes[3] = uuidNetOrder[0];
        guidBytes[4] = uuidNetOrder[5];
        guidBytes[5] = uuidNetOrder[4];
        guidBytes[6] = uuidNetOrder[7];
        guidBytes[7] = uuidNetOrder[6];

        for (var i = 8; i < 16; i++)
        {
            guidBytes[i] = uuidNetOrder[i];
        }

        return new Guid(guidBytes).ToString();
    }

    /// <summary>Set the timezone using an IANA or Windows identifier.</summary>
    public void SetTimeZone(string timeZoneId) =>
        TimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

    /// <summary>Validate an XML file against the Datawarehouse schema.</summary>
    public void ValidateXml(string xmlFile) => XmlValidator.ValidateXml(xmlFile, SchemaPath);

    /// <summary>
    /// Validate an XML file against the Datawarehouse schema returning metadata instead of throwing.
    /// </summary>
    public ValidationMetadata ValidateXmlSafe(string xmlFile) =>
        XmlValidator.ValidateXmlSafe(xmlFile, SchemaPath);

    private TimeZoneInfo ResolveTimeZone()
    {
        if (TimeZone != null)
        {
            return TimeZone;
        }

        return TimeZoneInfo.Local;
    }
}