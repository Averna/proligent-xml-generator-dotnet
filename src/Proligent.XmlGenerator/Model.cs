using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Proligent.XmlGenerator;

/// <summary>Common XML namespace constants used by the Proligent Datawarehouse schema.</summary>
public static class XmlNamespaces
{
    /// <summary>The Datawarehouse XML namespace.</summary>
    public const string Datawarehouse = 
        "http://www.averna.com/products/proligent/analytics/DIT/6.85";

    /// <summary>The <see cref="XNamespace" /> instance for the Datawarehouse schema.</summary>
    public static readonly XNamespace Dw = Datawarehouse;
}

/// <summary>Execution status values used across process, operation, sequence, step, and measure elements.</summary>
public enum ExecutionStatusKind
{
    /// <summary>
    /// The execution completed successfully.
    /// </summary>
    PASS,

    /// <summary>
    /// The execution completed and failures were detected.
    /// </summary>
    FAIL,

    /// <summary>
    /// The execution is not completed.  It is still in progress.  In that situation, the end time of the execution must be omitted.
    /// </summary>
    NOT_COMPLETED,

    /// <summary>
    /// The execution was aborted.
    /// </summary>
    ABORTED
}

/// <summary>Value types supported by the Measure element.</summary>
public enum MeasureKind
{
    /// <summary>
    /// Represents a real (floating-point) numeric value, typically used for continuous data.
    /// </summary>
    REAL,

    /// <summary>
    /// Represents a logical Boolean value (True or False).
    /// </summary>
    BOOL,

    /// <summary>
    /// Represents a whole number (integer) value.
    /// </summary>
    INTEGER,

    /// <summary>
    /// Represents textual data or a sequence of characters.
    /// </summary>
    STRING,

    /// <summary>
    /// Represents a specific point in time, including both date and time components.
    /// </summary>
    DATETIME
}

/// <summary>Expressions describing how numeric limits should be interpreted.</summary>
public enum LimitExpression
{
    /// <summary> Closed interval: LowerBound &lt;= x &lt;= HigherBound </summary>
    LOWERBOUND_LEQ_X_LEQ_HIGHER_BOUND,

    /// <summary> Left-open interval: LowerBound &lt; x &lt;= HigherBound </summary>
    LOWERBOUND_LE_X_LEQ_HIGHER_BOUND,

    /// <summary> Right-open interval: LowerBound &lt;= x &lt; HigherBound </summary>
    LOWERBOUND_LEQ_X_LE_HIGHER_BOUND,

    /// <summary> Open interval: LowerBound &lt; x &lt; HigherBound </summary>
    LOWERBOUND_LE_X_LE_HIGHER_BOUND,

    /// <summary> Greater than or equal to: LowerBound &lt;= x </summary>
    LOWERBOUND_LEQ_X,

    /// <summary> Strictly greater than: LowerBound &lt; x </summary>
    LOWERBOUND_LE_X,

    /// <summary> Less than or equal to: x &lt;= HigherBound </summary>
    X_LEQ_HIGHER_BOUND,

    /// <summary> Strictly less than: x &lt; HigherBound </summary>
    X_LE_HIGHER_BOUND,

    /// <summary> Equal to: x == HigherBound </summary>
    X_EQ_HIGHER_BOUND,

    /// <summary> Not equal to: x != HigherBound </summary>
    X_NEQ_HIGHER_BOUND,

    /// <summary> Outside closed interval: x &lt;= LowerBound OR HigherBound &lt;= x </summary>
    X_LEQ_LOWERBOUND_OR_HIGHERBOUND_LEQ_X,

    /// <summary> Outside half-open interval: x &lt; LowerBound OR HigherBound &lt;= x </summary>
    X_LE_LOWERBOUND_or_HIGHERBOUND_LEQ_X,

    /// <summary> Outside half-open interval: x &lt;= LowerBound OR HigherBound &lt; x </summary>
    X_LEQ_LOWERBOUND_or_HIGHERBOUND_LE_X,

    /// <summary> Outside open interval: x &lt; LowerBound OR HigherBound &lt; x </summary>
    X_LE_LOWERBOUND_or_HIGHERBOUND_LE_X
}

/// <summary>Metadata describing the result of XML schema validation.</summary>
public record ValidationMetadata(
    bool IsValid,
    string Message,
    string? Reason = null,
    string? Path = null,
    int? Line = null,
    int? Column = null
);

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
        string? timeZoneId = null)
    {
        TimeZone = timeZone ?? (timeZoneId is not null ? TimeZoneInfo.FindSystemTimeZoneById(timeZoneId) : null);
        DestinationDirectory = destinationDirectory ?? @"C:\Proligent\IntegrationService\Acquisition";
        _schemaPath = schemaPath ?? Path.Combine(AppContext.BaseDirectory, "Xsd", "Datawarehouse.xsd");
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

    /// <summary>Set the timezone using an IANA or Windows identifier.</summary>
    public void SetTimeZone(string timeZoneId) => TimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

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

/// <summary>Base class for objects that can produce Datawarehouse XML payloads.</summary>
public abstract class Buildable
{
    /// <summary>
    /// Build the XML element that mirrors the Datawarehouse schema element for this object.
    /// </summary>
    public abstract XElement Build(Util? util = null);

    /// <summary>Serialize the built element to an XML string.</summary>
    public virtual string ToXml(Util? util = null)
    {
        var element = Build(util ?? Util.Default);
        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), element);
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true,
            IndentChars = "  ",
            OmitXmlDeclaration = false,
        };

        using var writer = new Utf8StringWriter();
        using (var xmlWriter = XmlWriter.Create(writer, settings))
        {
            doc.Save(xmlWriter);
        }

        return writer.ToString();
    }

    /// <summary>
    /// Writes the XML representation to disk. When <paramref name="destinationFolder" />
    /// is not provided, the output is written to <see cref="Util.DestinationDirectory" />.
    /// When <paramref name="fileName" /> is not provided, the method generates a file name
    /// in the format Proligent_{uuid}.xml.
    /// </summary>
    /// <param name="destinationFolder">Optional destination file path.</param>
    /// <param name="fileName">Optional destination file name.</param>
    /// <param name="util">Optional utility instance to use for configuration.</param>
    /// <returns>The resulting file path.</returns>
    public virtual string SaveXml(string? destinationFolder=null, 
        string? fileName = null, 
        Util? util = null)
    {
        util ??= Util.Default;
        string uuid = util.Uuid();
        var targetFileName = string.IsNullOrWhiteSpace(fileName)
            ? $"Proligent_{uuid}.xml"
            : fileName;

        var targetPath = string.IsNullOrWhiteSpace(destinationFolder)
            ? Path.Combine(util.DestinationDirectory, targetFileName)
            : Path.Combine(destinationFolder, targetFileName);
        
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(targetPath))!);
        var xml = ToXml(util);
        File.WriteAllText(targetPath, xml, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return targetPath;
    }

    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    }
}

/// <summary>Numeric boundaries that accompany a measurement.</summary>
public sealed class Limit
{
    private static readonly Dictionary<LimitExpression, string> ExpressionTemplates = new()
    {
        { LimitExpression.LOWERBOUND_LEQ_X_LEQ_HIGHER_BOUND, "LOWERBOUND <= X <= HIGHERBOUND" },
        { LimitExpression.LOWERBOUND_LE_X_LEQ_HIGHER_BOUND, "LOWERBOUND < X <= HIGHERBOUND" },
        { LimitExpression.LOWERBOUND_LEQ_X_LE_HIGHER_BOUND, "LOWERBOUND <= X < HIGHERBOUND" },
        { LimitExpression.LOWERBOUND_LE_X_LE_HIGHER_BOUND, "LOWERBOUND < X < HIGHERBOUND" },
        { LimitExpression.LOWERBOUND_LEQ_X, "LOWERBOUND <= X" },
        { LimitExpression.LOWERBOUND_LE_X, "LOWERBOUND < X" },
        { LimitExpression.X_LEQ_HIGHER_BOUND, "X <= HIGHERBOUND" },
        { LimitExpression.X_LE_HIGHER_BOUND, "X < HIGHERBOUND" },
        { LimitExpression.X_EQ_HIGHER_BOUND, "X == HIGHERBOUND" },
        { LimitExpression.X_NEQ_HIGHER_BOUND, "X != HIGHERBOUND" },
        { LimitExpression.X_LEQ_LOWERBOUND_OR_HIGHERBOUND_LEQ_X, "X <= LOWERBOUND OR HIGHERBOUND <= X" },
        { LimitExpression.X_LE_LOWERBOUND_or_HIGHERBOUND_LEQ_X, "X < LOWERBOUND or HIGHERBOUND <= X" },
        { LimitExpression.X_LEQ_LOWERBOUND_or_HIGHERBOUND_LE_X, "X <= LOWERBOUND or HIGHERBOUND < X" },
        { LimitExpression.X_LE_LOWERBOUND_or_HIGHERBOUND_LE_X, "X < LOWERBOUND or HIGHERBOUND < X" },
    };

    private readonly LimitExpression _expression;

    /// <summary>Create a new limit expression.</summary>
    /// <param name="expression">Expression describing how the bounds apply.</param>
    /// <param name="lowerBound">Value for the LOWERBOUND token.</param>
    /// <param name="higherBound">Value for the HIGHERBOUND token.</param>
    public Limit(LimitExpression expression, object? lowerBound = null, object? higherBound = null)
    {
        _expression = expression;
        LowerBound = lowerBound;
        HigherBound = higherBound;
    }

    /// <summary>Value substituted for the LOWERBOUND token when present in the expression.</summary>
    public object? LowerBound { get; }

    /// <summary>Value substituted for the HIGHERBOUND token when present in the expression.</summary>
    public object? HigherBound { get; }

    /// <summary>Render the expression string with the current bounds inserted.</summary>
    public override string ToString()
    {
        var template = ExpressionTemplates[_expression];
        return template
            .Replace("LOWERBOUND", ConvertBound(LowerBound), StringComparison.OrdinalIgnoreCase)
            .Replace("HIGHERBOUND", ConvertBound(HigherBound), StringComparison.OrdinalIgnoreCase);
    }

    private static string ConvertBound(object? value)
    {
        return value switch
        {
            null => string.Empty,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };
    }
}

/// <summary>Arbitrary key/value metadata serialized to Characteristic elements.</summary>
public sealed class Characteristic : Buildable
{
    private const string ReservedPrefix = "Proligent.";
    private const string ReservedError = "Characteristic names starting with 'Proligent.' are reserved for internal use.";
    private readonly bool _allowReserved;

    /// <summary>Create a new characteristic.</summary>
    /// <param name="fullName">FullName attribute; must be unique per owning element.</param>
    /// <param name="value">Optional value attribute.</param>
    /// <param name="allowReserved">Allow reserved name.</param>
    public Characteristic(string fullName, string? value = null, bool allowReserved = false)
    {
        FullName = fullName;
        Value = value ?? string.Empty;
        _allowReserved = allowReserved;
        EnsureAllowed();
    }

    /// <summary>FullName attribute written to XML.</summary>
    public string FullName { get; }

    /// <summary>Optional Value attribute.</summary>
    public string Value { get; }

    internal void EnsureAllowed()
    {
        if (!_allowReserved && FullName.StartsWith(ReservedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(ReservedError);
        }
    }

    /// <inheritdoc />
    public override XElement Build(Util? util = null)
    {
        return new XElement(
            XmlNamespaces.Dw + "Characteristic",
            new XAttribute("FullName", FullName),
            string.IsNullOrWhiteSpace(Value) ? null : new XAttribute("Value", Value));
    }
}

/// <summary>Reference to a document attached to a run or product unit.</summary>
public sealed class Document : Buildable
{
    /// <summary>Create a new document reference.</summary>
    public Document(string fileName, string? identifier = null, string? name = null, string? description = null)
    {
        FileName = fileName;
        Identifier = identifier ?? Util.Default.Uuid();
        Name = name ?? string.Empty;
        Description = description ?? string.Empty;
    }

    /// <summary>GUID stored in Identifier.</summary>
    public string Identifier { get; }

    /// <summary>Path or filename stored in the FileName attribute.</summary>
    public string FileName { get; }

    /// <summary>Optional human-readable identifier stored in Name.</summary>
    public string Name { get; }

    /// <summary>Optional description persisted to Description.</summary>
    public string Description { get; }

    /// <inheritdoc />
    public override XElement Build(Util? util = null)
    {
        return new XElement(
            XmlNamespaces.Dw + "Document",
            new XAttribute("Identifier", Identifier),
            new XAttribute("FileName", FileName),
            string.IsNullOrWhiteSpace(Name) ? null : new XAttribute("Name", Name),
            string.IsNullOrWhiteSpace(Description) ? null : new XAttribute("Description", Description));
    }
}

/// <summary>Common attributes shared by the process/operation/sequence/step run types.</summary>
public abstract class ManufacturingStep : Buildable
{
    /// <summary>
    /// Create a new manufacturing step.
    /// </summary>
    /// <param name="id">Identifier persisted to the relevant *_Id attribute.</param>
    /// <param name="name">Display name serialized to StepName/SequenceFullName/etc.</param>
    /// <param name="status">Execution status value stored in the respective *_Status attribute.</param>
    /// <param name="startTime">Start timestamp persisted to *_StartTime/StartDate.</param>
    /// <param name="endTime">Completion timestamp persisted to *_EndTime/EndDate.</param>
    protected ManufacturingStep(
        string? id = null,
        string? name = null,
        ExecutionStatusKind status = ExecutionStatusKind.NOT_COMPLETED,
        DateTime? startTime = null,
        DateTime? endTime = null)
    {
        Id = id ?? Util.Default.Uuid();
        Name = name ?? string.Empty;
        Status = status;
        StartTime = startTime ?? DateTime.Now;
        EndTime = endTime ?? DateTime.Now;
    }

    /// <summary>Identifier persisted to the relevant *_Id attribute.</summary>
    public string Id { get; set; }

    /// <summary>Display name serialized to StepName/SequenceFullName/etc.</summary>
    public string Name { get; set; }

    /// <summary>Execution status value stored in the respective *_Status attribute.</summary>
    public ExecutionStatusKind Status { get; private set; }

    /// <summary>Start timestamp persisted to *_StartTime/StartDate.</summary>
    public DateTime? StartTime { get; set; }

    /// <summary>Completion timestamp persisted to *_EndTime/EndDate.</summary>
    public DateTime? EndTime { get; private set; }

    /// <summary>
    /// Mark the step as finished and set the execution status and end time that will be serialized to the payload.
    /// </summary>
    public void Complete(ExecutionStatusKind status, DateTime? endTime = null)
    {
        Status = status;
        EndTime = endTime ?? DateTime.Now;
    }
}

/// <summary>
/// Extension of <see cref="ManufacturingStep" /> that carries the process/sequence version number.
/// </summary>
public abstract class VersionedManufacturingStep : ManufacturingStep
{
    /// <summary>Create a versioned manufacturing step.</summary>
    protected VersionedManufacturingStep(
        string? id = null,
        string? name = null,
        string? version = null,
        ExecutionStatusKind status = ExecutionStatusKind.NOT_COMPLETED,
        DateTime? startTime = null,
        DateTime? endTime = null)
        : base(id, name, status, startTime, endTime)
    {
        Version = version ?? string.Empty;
    }

    /// <summary>Version string written to SequenceVersion or ProcessVersion.</summary>
    public string Version { get; set; }
}

/// <summary>
/// Measurement captured during a step run, mapped to the Measure element.
/// </summary>
public sealed class Measure : Buildable
{
    private readonly object _value;

    /// <summary>Create a new measurement.</summary>
    public Measure(
        object value,
        string? id = null,
        Limit? limit = null,
        DateTime? time = null,
        string? comments = null,
        string? unit = null,
        string? symbol = null,
        ExecutionStatusKind? status = null)
    {
        _value = value;
        Id = id ?? Util.Default.Uuid();
        Limit = limit;
        Time = time ?? DateTime.Now;
        Comments = comments ?? string.Empty;
        Unit = unit ?? string.Empty;
        Symbol = symbol ?? string.Empty;
        Status = status;
    }

    /// <summary>Unique identifier written to MeasureId.</summary>
    public string Id { get; }

    /// <summary>Optional numeric bounds.</summary>
    public Limit? Limit { get; }

    /// <summary>Timestamp describing when the value was acquired.</summary>
    public DateTime? Time { get; }

    /// <summary>Free-form note written to the Comments attribute.</summary>
    public string Comments { get; }

    /// <summary>Engineering unit name persisted to Unit.</summary>
    public string Unit { get; }

    /// <summary>Unit symbol stored in Symbol.</summary>
    public string Symbol { get; }

    /// <summary>Execution status emitted as MeasureExecutionStatus.</summary>
    public ExecutionStatusKind? Status { get; }

    /// <inheritdoc />
    public override XElement Build(Util? util = null)
    {
        util ??= Util.Default;

        var measureElement = new XElement(
            XmlNamespaces.Dw + "Measure",
            new XAttribute("MeasureId", Id),
            new XAttribute("MeasureTime", util.FormatDateTime(Time)));

        if (Status.HasValue)
        {
            measureElement.Add(new XAttribute("MeasureExecutionStatus", Status.Value));
        }

        if (!string.IsNullOrWhiteSpace(Comments))
        {
            measureElement.Add(new XAttribute("Comments", Comments));
        }

        if (!string.IsNullOrWhiteSpace(Unit))
        {
            measureElement.Add(new XAttribute("Unit", Unit));
        }

        if (!string.IsNullOrWhiteSpace(Symbol))
        {
            measureElement.Add(new XAttribute("Symbol", Symbol));
        }

        var (stringValue, measureKind) = NormalizeValue(_value);
        measureElement.Add(
            new XElement(
                XmlNamespaces.Dw + "Value",
                new XAttribute("Type", measureKind),
                stringValue));

        if (Limit != null)
        {
            measureElement.Add(new XElement(
                XmlNamespaces.Dw + "Limit",
                new XAttribute("LimitExpression", Limit.ToString())));
        }

        return measureElement;
    }

    private static (string Value, MeasureKind Kind) NormalizeValue(object value)
    {
        switch (value)
        {
            case string str:
                return (str, MeasureKind.STRING);
            case bool b:
                return (b.ToString(), MeasureKind.BOOL);
            case int i:
                return (i.ToString(CultureInfo.InvariantCulture), MeasureKind.INTEGER);
            case long l:
                return (l.ToString(CultureInfo.InvariantCulture), MeasureKind.INTEGER);
            case float f:
                return (f.ToString(CultureInfo.InvariantCulture), MeasureKind.REAL);
            case double d:
                return (d.ToString(CultureInfo.InvariantCulture), MeasureKind.REAL);
            case decimal m:
                return (m.ToString(CultureInfo.InvariantCulture), MeasureKind.REAL);
            case DateTime dt:
                return (dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), MeasureKind.DATETIME);
            case DateTimeOffset dto:
                return (dto.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), MeasureKind.DATETIME);
            default:
                throw new ArgumentException("Incompatible value type for Measure.");
        }
    }
}

/// <summary>
/// Execution of a single manufacturing step with measures, characteristics, and attached documents.
/// </summary>
public sealed class StepRun : ManufacturingStep
{
    private readonly List<Measure> _measures = new();

    /// <summary>Create a step run.</summary>
    public StepRun(
        Measure? measure = null,
        string? id = null,
        string? name = null,
        ExecutionStatusKind status = ExecutionStatusKind.NOT_COMPLETED,
        DateTime? startTime = null,
        DateTime? endTime = null,
        IEnumerable<Characteristic>? characteristics = null,
        IEnumerable<Document>? documents = null)
        : base(id, name, status, startTime, endTime)
    {
        if (measure != null)
        {
            _measures.Add(measure);
        }

        Characteristics = (characteristics ?? Array.Empty<Characteristic>()).ToList();
        Documents = (documents ?? Array.Empty<Document>()).ToList();
        CharacteristicHelpers.EnsureCharacteristicsAllowed(Characteristics);
    }

    /// <summary>Metadata entries serialized under Characteristic.</summary>
    public List<Characteristic> Characteristics { get; }

    /// <summary>Document references serialized under Document.</summary>
    public List<Document> Documents { get; }

    /// <inheritdoc />
    public override XElement Build(Util? util = null)
    {
        util ??= Util.Default;
        var step = new XElement(
            XmlNamespaces.Dw + "StepRun",
            new XAttribute("StepRunId", Id),
            new XAttribute("StartDate", util.FormatDateTime(StartTime)));

        if (Status != ExecutionStatusKind.NOT_COMPLETED)
        {
            step.Add(new XAttribute("EndDate", util.FormatDateTime(EndTime)));
        }

        if (!string.IsNullOrWhiteSpace(Name))
        {
            step.Add(new XAttribute("StepName", Name));
        }

        step.Add(new XAttribute("StepExecutionStatus", Status));

        foreach (var measure in _measures)
        {
            step.Add(measure.Build(util));
        }

        foreach (var characteristic in Characteristics)
        {
            step.Add(characteristic.Build(util));
        }

        foreach (var document in Documents)
        {
            step.Add(document.Build(util));
        }

        return step;
    }

    /// <summary>
    /// Attach a measurement that will be emitted inside this StepRun's measure collection.
    /// </summary>
    public Measure AddMeasure(Measure measure)
    {
        _measures.Add(measure);
        return measure;
    }

    /// <summary>Attach metadata that will be serialized under this step run.</summary>
    public Characteristic AddCharacteristic(Characteristic characteristic)
    {
        characteristic.EnsureAllowed();
        Characteristics.Add(characteristic);
        return characteristic;
    }

    /// <summary>Associate a document reference with this step run.</summary>
    public Document AddDocument(Document document)
    {
        Documents.Add(document);
        return document;
    }
}

/// <summary>
/// Ordered collection of step runs executed on a station/user.
/// </summary>
public sealed class SequenceRun : VersionedManufacturingStep
{
    private string _station = string.Empty;

    /// <summary>Create a new sequence run.</summary>
    public SequenceRun(
        IEnumerable<StepRun>? steps = null,
        string? id = null,
        string? name = null,
        string? version = null,
        string? user = null,
        ExecutionStatusKind status = ExecutionStatusKind.NOT_COMPLETED,
        DateTime? startTime = null,
        DateTime? endTime = null,
        IEnumerable<Characteristic>? characteristics = null,
        IEnumerable<Document>? documents = null)
        : base(id, name, version, status, startTime, endTime)
    {
        Steps = (steps ?? Array.Empty<StepRun>()).ToList();
        User = user ?? string.Empty;
        Characteristics = (characteristics ?? Array.Empty<Characteristic>()).ToList();
        Documents = (documents ?? Array.Empty<Document>()).ToList();
        CharacteristicHelpers.EnsureCharacteristicsAllowed(Characteristics);
    }

    /// <summary>Step runs executed within this sequence.</summary>
    public List<StepRun> Steps { get; }

    /// <summary>Operator stored in the User attribute.</summary>
    public string User { get; set; }

    /// <summary>Metadata entries serialized under Characteristic.</summary>
    public List<Characteristic> Characteristics { get; }

    /// <summary>Document references serialized under Document.</summary>
    public List<Document> Documents { get; }

    /// <summary>Station assigned by the owning OperationRun.</summary>
    public string Station => _station;

    internal void AssignStation(string station)
    {
        if (string.IsNullOrWhiteSpace(station))
        {
            throw new ArgumentException("Station cannot be empty when applied to SequenceRun.");
        }

        if (!string.IsNullOrWhiteSpace(_station) && !string.Equals(_station, station, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("SequenceRun is already associated with a different station.");
        }

        _station = station;
    }

    /// <inheritdoc />
    public override XElement Build(Util? util = null)
    {
        util ??= Util.Default;
        if (string.IsNullOrWhiteSpace(_station))
        {
            throw new InvalidOperationException("SequenceRun must be added to an OperationRun with a station before building.");
        }

        var sequence = new XElement(
            XmlNamespaces.Dw + "SequenceRun",
            new XAttribute("SequenceRunId", Id),
            new XAttribute("StartDate", util.FormatDateTime(StartTime)),
            new XAttribute("StationFullName", _station));

        if (Status != ExecutionStatusKind.NOT_COMPLETED)
        {
            sequence.Add(new XAttribute("EndDate", util.FormatDateTime(EndTime)));
        }

        if (!string.IsNullOrWhiteSpace(Name))
        {
            sequence.Add(new XAttribute("SequenceFullName", Name));
        }

        sequence.Add(new XAttribute("SequenceExecutionStatus", Status));

        if (!string.IsNullOrWhiteSpace(Version))
        {
            sequence.Add(new XAttribute("SequenceVersion", Version));
        }

        if (!string.IsNullOrWhiteSpace(User))
        {
            sequence.Add(new XAttribute("User", User));
        }

        foreach (var step in Steps)
        {
            sequence.Add(step.Build(util));
        }

        foreach (var characteristic in Characteristics)
        {
            sequence.Add(characteristic.Build(util));
        }

        foreach (var document in Documents)
        {
            sequence.Add(document.Build(util));
        }

        return sequence;
    }

    /// <summary>Append a step run that will be serialized within sequence_run.</summary>
    public StepRun AddStepRun(StepRun stepRun)
    {
        Steps.Add(stepRun);
        return stepRun;
    }

    /// <summary>Attach metadata that will be serialized under this sequence run.</summary>
    public Characteristic AddCharacteristic(Characteristic characteristic)
    {
        characteristic.EnsureAllowed();
        Characteristics.Add(characteristic);
        return characteristic;
    }

    /// <summary>Associate a document reference with this sequence run.</summary>
    public Document AddDocument(Document document)
    {
        Documents.Add(document);
        return document;
    }
}

/// <summary>
/// Group of sequence runs executed within a process operation.
/// </summary>
public sealed class OperationRun : ManufacturingStep
{
    /// <summary>Create an operation run.</summary>
    public OperationRun(
        string station,
        IEnumerable<SequenceRun>? sequences = null,
        string? id = null,
        string? name = null,
        string? user = null,
        string? processName = null,
        string? testPositionName = null,
        ExecutionStatusKind status = ExecutionStatusKind.NOT_COMPLETED,
        DateTime? startTime = null,
        DateTime? endTime = null,
        IEnumerable<Characteristic>? characteristics = null,
        IEnumerable<Document>? documents = null)
        : base(id, name, status, startTime, endTime)
    {
        if (string.IsNullOrWhiteSpace(station))
        {
            throw new ArgumentException("OperationRun.station is required and cannot be empty.");
        }

        Station = station;
        Sequences = (sequences ?? Array.Empty<SequenceRun>()).ToList();
        User = user ?? string.Empty;
        ProcessName = processName ?? string.Empty;
        Characteristics = (characteristics ?? Array.Empty<Characteristic>()).ToList();
        Documents = (documents ?? Array.Empty<Document>()).ToList();
        TestPositionName = testPositionName ?? string.Empty;
        CharacteristicHelpers.EnsureCharacteristicsAllowed(Characteristics);
        PropagateStationToSequences();
    }

    /// <summary>Station context stored in StationFullName.</summary>
    public string Station { get; }

    /// <summary>Sequence runs executed within the operation.</summary>
    public List<SequenceRun> Sequences { get; }

    /// <summary>Operator stored in the User attribute.</summary>
    public string User { get; set; }

    /// <summary>Parent process name serialized as ProcessFullName.</summary>
    public string ProcessName { get; set; }

    /// <summary>Metadata entries serialized under Characteristic.</summary>
    public List<Characteristic> Characteristics { get; }

    /// <summary>Document references serialized under Document.</summary>
    public List<Document> Documents { get; }

    /// <summary>
    /// Optional test position identifier serialized as the Proligent.TestPositionName characteristic when provided.
    /// </summary>
    public string TestPositionName { get; set; }

    /// <inheritdoc />
    public override XElement Build(Util? util = null)
    {
        util ??= Util.Default;
        PropagateStationToSequences();

        var operation = new XElement(
            XmlNamespaces.Dw + "OperationRun",
            new XAttribute("OperationRunId", Id),
            new XAttribute("OperationRunStartTime", util.FormatDateTime(StartTime)),
            new XAttribute("StationFullName", Station));

        if (Status != ExecutionStatusKind.NOT_COMPLETED)
        {
            operation.Add(new XAttribute("OperationRunEndTime", util.FormatDateTime(EndTime)));
        }

        if (!string.IsNullOrWhiteSpace(Name))
        {
            operation.Add(new XAttribute("OperationName", Name));
        }

        operation.Add(new XAttribute("OperationStatus", Status));

        if (!string.IsNullOrWhiteSpace(User))
        {
            operation.Add(new XAttribute("User", User));
        }

        if (!string.IsNullOrWhiteSpace(ProcessName))
        {
            operation.Add(new XAttribute("ProcessFullName", ProcessName));
        }

        foreach (var sequence in Sequences)
        {
            operation.Add(sequence.Build(util));
        }

        var characteristics = new List<Characteristic>(Characteristics);
        if (!string.IsNullOrWhiteSpace(TestPositionName))
        {
            characteristics.Add(new Characteristic("Proligent.TestPositionName", TestPositionName, allowReserved: true));
        }

        foreach (var characteristic in characteristics)
        {
            operation.Add(characteristic.Build(util));
        }

        foreach (var document in Documents)
        {
            operation.Add(document.Build(util));
        }

        return operation;
    }

    /// <summary>Append a sequence run that will be serialized within operation_run.</summary>
    public SequenceRun AddSequenceRun(SequenceRun sequenceRun)
    {
        sequenceRun.AssignStation(Station);
        Sequences.Add(sequenceRun);
        return sequenceRun;
    }

    /// <summary>Attach metadata that will be serialized under this operation run.</summary>
    public Characteristic AddCharacteristic(Characteristic characteristic)
    {
        characteristic.EnsureAllowed();
        Characteristics.Add(characteristic);
        return characteristic;
    }

    /// <summary>Associate a document reference with this operation run.</summary>
    public Document AddDocument(Document document)
    {
        Documents.Add(document);
        return document;
    }

    private void PropagateStationToSequences()
    {
        foreach (var sequence in Sequences)
        {
            sequence.AssignStation(Station);
        }
    }
}

/// <summary>
/// Top-level execution of a process with nested operation runs.
/// </summary>
public sealed class ProcessRun : VersionedManufacturingStep
{
    /// <summary>Create a process run.</summary>
    public ProcessRun(
        string? productUnitIdentifier = null,
        string? productFullName = null,
        IEnumerable<OperationRun>? operations = null,
        string? id = null,
        string? name = null,
        string? version = null,
        string? processMode = null,
        ExecutionStatusKind status = ExecutionStatusKind.NOT_COMPLETED,
        DateTime? startTime = null,
        DateTime? endTime = null)
        : base(id, name, version, status, startTime, endTime)
    {
        ProductUnitIdentifier = productUnitIdentifier ?? Util.Default.Uuid();
        ProductFullName = productFullName ?? "DUT";
        Operations = (operations ?? Array.Empty<OperationRun>()).ToList();
        ProcessMode = processMode ?? string.Empty;
    }

    /// <summary>Identifier stored in ProductUnitIdentifier.</summary>
    public string ProductUnitIdentifier { get; set; }

    /// <summary>Product name stored in ProductFullName.</summary>
    public string ProductFullName { get; set; }

    /// <summary>Operation runs serialized inside OperationRun.</summary>
    public List<OperationRun> Operations { get; }

    /// <summary>Optional process mode string persisted to ProcessMode.</summary>
    public string ProcessMode { get; set; }

    /// <inheritdoc />
    public override XElement Build(Util? util = null)
    {
        util ??= Util.Default;

        foreach (var operation in Operations)
        {
            if (string.IsNullOrWhiteSpace(operation.ProcessName) && !string.IsNullOrWhiteSpace(Name))
            {
                operation.ProcessName = Name;
            }
        }

        var process = new XElement(
            XmlNamespaces.Dw + "TopProcessRun",
            new XAttribute("ProcessRunId", Id),
            new XAttribute("ProductUnitIdentifier", ProductUnitIdentifier),
            new XAttribute("ProductFullName", ProductFullName),
            new XAttribute("ProcessRunStartTime", util.FormatDateTime(StartTime)));

        if (Status != ExecutionStatusKind.NOT_COMPLETED)
        {
            process.Add(new XAttribute("ProcessRunEndTime", util.FormatDateTime(EndTime)));
        }

        if (!string.IsNullOrWhiteSpace(Name))
        {
            process.Add(new XAttribute("ProcessFullName", Name));
        }

        process.Add(new XAttribute("ProcessRunStatus", Status));

        if (!string.IsNullOrWhiteSpace(Version))
        {
            process.Add(new XAttribute("ProcessVersion", Version));
        }

        if (!string.IsNullOrWhiteSpace(ProcessMode))
        {
            process.Add(new XAttribute("ProcessMode", ProcessMode));
        }

        foreach (var operation in Operations)
        {
            process.Add(operation.Build(util));
        }

        return process;
    }

    /// <summary>Append an operation run that will be serialized within operation_run.</summary>
    public OperationRun AddOperationRun(OperationRun operationRun)
    {
        if (string.IsNullOrWhiteSpace(operationRun.ProcessName) && !string.IsNullOrWhiteSpace(Name))
        {
            operationRun.ProcessName = Name;
        }

        Operations.Add(operationRun);
        return operationRun;
    }
}

/// <summary>
/// Description of the product unit involved in a process, mapped to the ProductUnit element.
/// </summary>
public sealed class ProductUnit : Buildable
{
    /// <summary>Create a product unit description.</summary>
    public ProductUnit(
        string? productUnitIdentifier = null,
        string? productFullName = null,
        IEnumerable<Characteristic>? characteristics = null,
        IEnumerable<Document>? documents = null,
        string? manufacturer = null,
        DateTime? creationTime = null,
        DateTime? manufacturingTime = null,
        bool? scrapped = null,
        DateTime? scrapTime = null)
    {
        ProductUnitIdentifier = productUnitIdentifier ?? Util.Default.Uuid();
        ProductFullName = productFullName ?? string.Empty;
        Characteristics = (characteristics ?? Array.Empty<Characteristic>()).ToList();
        Documents = (documents ?? Array.Empty<Document>()).ToList();
        Manufacturer = manufacturer ?? string.Empty;
        CreationTime = creationTime;
        ManufacturingTime = manufacturingTime;
        Scrapped = scrapped;
        ScrapTime = scrapTime;
        CharacteristicHelpers.EnsureCharacteristicsAllowed(Characteristics);
    }

    /// <summary>Unique identifier stored in ProductUnitIdentifier.</summary>
    public string ProductUnitIdentifier { get; set; }

    /// <summary>Fully qualified product name written to ProductFullName.</summary>
    public string ProductFullName { get; set; }

    /// <summary>Metadata entries serialized under Characteristic.</summary>
    public List<Characteristic> Characteristics { get; }

    /// <summary>Document references serialized under Document.</summary>
    public List<Document> Documents { get; }

    /// <summary>Manufacturer/site stored in ByManufacturer.</summary>
    public string Manufacturer { get; set; }

    /// <summary>Creation timestamp emitted as CreationTime.</summary>
    public DateTime? CreationTime { get; set; }

    /// <summary>Manufacturing timestamp emitted as ManufacturingTime.</summary>
    public DateTime? ManufacturingTime { get; set; }

    /// <summary>Flag stored in Scrapped.</summary>
    public bool? Scrapped { get; set; }

    /// <summary>Timestamp stored in ScrappedTime; required when scrapped is true.</summary>
    public DateTime? ScrapTime { get; set; }

    /// <inheritdoc />
    public override XElement Build(Util? util = null)
    {
        util ??= Util.Default;

        var productUnit = new XElement(
            XmlNamespaces.Dw + "ProductUnit",
            new XAttribute("ProductUnitIdentifier", ProductUnitIdentifier));

        if (!string.IsNullOrWhiteSpace(ProductFullName))
        {
            productUnit.Add(new XAttribute("ProductFullName", ProductFullName));
        }

        if (!string.IsNullOrWhiteSpace(Manufacturer))
        {
            productUnit.Add(new XAttribute("ByManufacturer", Manufacturer));
        }

        if (CreationTime.HasValue)
        {
            productUnit.Add(new XAttribute("CreationTime", util.FormatDateTime(CreationTime)));
        }

        if (ManufacturingTime.HasValue)
        {
            productUnit.Add(new XAttribute("ManufacturingTime", util.FormatDateTime(ManufacturingTime)));
        }

        if (Scrapped.HasValue)
        {
            productUnit.Add(new XAttribute("Scrapped", Scrapped.Value.ToString().ToLowerInvariant()));
        }

        if (ScrapTime.HasValue)
        {
            productUnit.Add(new XAttribute("ScrappedTime", util.FormatDateTime(ScrapTime)));
        }

        foreach (var characteristic in Characteristics)
        {
            productUnit.Add(characteristic.Build(util));
        }

        foreach (var document in Documents)
        {
            productUnit.Add(document.Build(util));
        }

        return productUnit;
    }

    /// <summary>Attach metadata that will be serialized under this product unit.</summary>
    public Characteristic AddCharacteristic(Characteristic characteristic)
    {
        characteristic.EnsureAllowed();
        Characteristics.Add(characteristic);
        return characteristic;
    }

    /// <summary>Associate a document reference with this product unit.</summary>
    public Document AddDocument(Document document)
    {
        Documents.Add(document);
        return document;
    }
}

/// <summary>
/// Container for the full Datawarehouse payload, including the top process run and optional product unit details.
/// </summary>
public sealed class DataWareHouse : Buildable
{
    /// <summary>Create a new warehouse payload.</summary>
    public DataWareHouse(
        ProcessRun? topProcess = null,
        ProductUnit? productUnit = null,
        DateTime? generationTime = null,
        string? sourceFingerprint = null)
    {
        TopProcess = topProcess;
        ProductUnit = productUnit;
        GenerationTime = generationTime ?? DateTime.Now;
        SourceFingerprint = sourceFingerprint ?? Util.Default.Uuid();
    }

    /// <summary>Primary ProcessRun serialized to TopProcessRun.</summary>
    public ProcessRun? TopProcess { get; private set; }

    /// <summary>Optional ProductUnit serialized to ProductUnit.</summary>
    public ProductUnit? ProductUnit { get; private set; }

    /// <summary>Timestamp emitted as GenerationTime.</summary>
    public DateTime? GenerationTime { get; set; }

    /// <summary>Identifier stored in DataSourceFingerprint to prevent replays.</summary>
    public string SourceFingerprint { get; set; }

    /// <inheritdoc />
    public override XElement Build(Util? util = null)
    {
        util ??= Util.Default;

        var warehouse = new XElement(
            XmlNamespaces.Dw + "Proligent.Datawarehouse",
            new XAttribute("GenerationTime", util.FormatDateTime(GenerationTime)),
            new XAttribute("DataSourceFingerprint", SourceFingerprint));

        if (TopProcess != null)
        {
            warehouse.Add(TopProcess.Build(util));
        }

        if (ProductUnit != null)
        {
            warehouse.Add(ProductUnit.Build(util));
        }

        return warehouse;
    }

    /// <summary>Assign the ProcessRun that will populate top_process_run.</summary>
    public ProcessRun SetProcessRun(ProcessRun processRun)
    {
        TopProcess = processRun;
        return processRun;
    }

    /// <summary>Assign the ProductUnit that will populate product_unit.</summary>
    public ProductUnit SetProductUnit(ProductUnit productUnit)
    {
        ProductUnit = productUnit;
        return productUnit;
    }
}

internal static class CharacteristicHelpers
{
    public static void EnsureCharacteristicsAllowed(IEnumerable<Characteristic> characteristics)
    {
        foreach (var characteristic in characteristics)
        {
            characteristic.EnsureAllowed();
        }
    }
}

