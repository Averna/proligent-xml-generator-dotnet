using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Proligent.XmlGenerator;

/// <summary>Assembly action recorded by an <see cref="AssemblyTrace"/>.</summary>
public enum AssemblyAction
{
    /// <summary>A child was assembled onto the parent.</summary>
    ASSEMBLE,

    /// <summary>A child was removed from the parent.</summary>
    DISASSEMBLE,

    /// <summary>
    /// An existing assembly was observed and entered into the system. The action time is when it was
    /// identified and entered, not necessarily when it was physically assembled.
    /// </summary>
    SET_CURRENT,
}

internal static partial class AssemblyValidation
{
    [GeneratedRegex(
        @"^([^/\s][^/]{0,64})?[^/\s](/([^/\s][^/]{0,64})?[^/\s])*\z",
        RegexOptions.CultureInvariant
    )]
    private static partial Regex FullNamePattern();

    [GeneratedRegex(
        @"^(?:[A-Fa-f0-9]{8}-(?:[A-Fa-f0-9]{4}-){3}[A-Fa-f0-9]{12}|\{[A-Fa-f0-9]{8}-(?:[A-Fa-f0-9]{4}-){3}[A-Fa-f0-9]{12}\}|\([A-Fa-f0-9]{8}-(?:[A-Fa-f0-9]{4}-){3}[A-Fa-f0-9]{12}\))\z",
        RegexOptions.CultureInvariant
    )]
    private static partial Regex GuidPattern();

    public static string NonEmpty(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{field} is required and cannot be empty.", field);
        }

        if (value.Length > maxLength)
        {
            throw new ArgumentException(
                $"{field} cannot exceed {maxLength} characters.",
                field
            );
        }

        return value;
    }

    public static string? OptionalNonEmpty(string? value, string field, int maxLength)
    {
        return value == null ? null : NonEmpty(value, field, maxLength);
    }

    public static string FullName(string? value, string field)
    {
        NonEmpty(value, field, 1950);
        if (!FullNamePattern().IsMatch(value!))
        {
            throw new ArgumentException(
                $"{field} is not a valid full name (segments of at most 65 characters separated by '/', without leading/trailing whitespace).",
                field
            );
        }

        return value!;
    }

    public static string? OptionalFullName(string? value, string field)
    {
        return value == null ? null : FullName(value, field);
    }

    public static string Guid(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{field} is required and cannot be empty.", field);
        }

        if (!GuidPattern().IsMatch(value))
        {
            throw new ArgumentException($"{field} must be a valid GUID.", field);
        }

        return value;
    }
}

/// <summary>Parent product unit of an assembly trace (Parent element).</summary>
public sealed class AssemblyTraceParent
{
    /// <summary>Create a parent identity.</summary>
    public AssemblyTraceParent(
        string productFullName,
        string productUnitIdentifier,
        string? referenceDesignator = null
    )
    {
        ProductFullName = AssemblyValidation.FullName(productFullName, nameof(ProductFullName));
        ProductUnitIdentifier = AssemblyValidation.NonEmpty(
            productUnitIdentifier,
            nameof(ProductUnitIdentifier),
            50
        );
        ReferenceDesignator = AssemblyValidation.OptionalNonEmpty(
            referenceDesignator,
            nameof(ReferenceDesignator),
            50
        );
    }

    /// <summary>Create a parent identity from an existing product unit (values are copied).</summary>
    public AssemblyTraceParent(ProductUnit productUnit, string? referenceDesignator = null)
        : this(
            (productUnit ?? throw new ArgumentNullException(nameof(productUnit))).ProductFullName,
            productUnit.ProductUnitIdentifier,
            referenceDesignator
        ) { }

    /// <summary>Parent product full name.</summary>
    public string ProductFullName { get; }

    /// <summary>Parent product unit identifier.</summary>
    public string ProductUnitIdentifier { get; }

    /// <summary>Optional reference designator, part of the trace identity.</summary>
    public string? ReferenceDesignator { get; }

    internal XElement Build() =>
        new(
            XmlNamespaces.Dw + "Parent",
            new XAttribute("ProductFullName", ProductFullName),
            new XAttribute("ProductUnitIdentifier", ProductUnitIdentifier),
            ReferenceDesignator == null
                ? null
                : new XAttribute("ReferenceDesignator", ReferenceDesignator)
        );
}

/// <summary>Child identity of an assembly trace: either a product unit or a supplier component.</summary>
public abstract class AssemblyTraceChild
{
    internal AssemblyTraceChild() { }

    internal abstract XElement Build();

    /// <summary>Reference a child that is a product unit, by identity.</summary>
    public static AssemblyTraceChild FromProductUnit(
        string productFullName,
        string productUnitIdentifier
    ) => new ProductUnitChild(productFullName, productUnitIdentifier);

    /// <summary>Reference a child that is a product unit, from an existing model object.</summary>
    public static AssemblyTraceChild FromProductUnit(ProductUnit productUnit)
    {
        ArgumentNullException.ThrowIfNull(productUnit);

        return new ProductUnitChild(productUnit.ProductFullName, productUnit.ProductUnitIdentifier);
    }

    /// <summary>Reference a child that is a supplier component.</summary>
    public static AssemblyTraceChild FromSupplierComponent(
        string manufacturer,
        string manufacturerPartNumber,
        string componentFullName,
        string? manufacturerLot = null,
        string? manufacturerSerialNumber = null
    ) =>
        new SupplierComponentChild(
            manufacturer,
            manufacturerPartNumber,
            componentFullName,
            manufacturerLot,
            manufacturerSerialNumber
        );
}

/// <summary>Product-unit child (ProductUnitUniqueIdentifier).</summary>
public sealed class ProductUnitChild : AssemblyTraceChild
{
    /// <summary>Create a product-unit child identity.</summary>
    public ProductUnitChild(string productFullName, string productUnitIdentifier)
    {
        ProductFullName = AssemblyValidation.FullName(productFullName, nameof(ProductFullName));
        ProductUnitIdentifier = AssemblyValidation.NonEmpty(
            productUnitIdentifier,
            nameof(ProductUnitIdentifier),
            50
        );
    }

    /// <summary>Child product full name.</summary>
    public string ProductFullName { get; }

    /// <summary>Child product unit identifier.</summary>
    public string ProductUnitIdentifier { get; }

    internal override XElement Build() =>
        new(
            XmlNamespaces.Dw + "Child",
            new XElement(
                XmlNamespaces.Dw + "ProductUnitUniqueIdentifier",
                new XAttribute("ProductFullName", ProductFullName),
                new XAttribute("ProductUnitIdentifier", ProductUnitIdentifier)
            )
        );
}

/// <summary>Supplier-component child (ComponentUnitIdentifier).</summary>
public sealed class SupplierComponentChild : AssemblyTraceChild
{
    /// <summary>Create a supplier-component child identity.</summary>
    public SupplierComponentChild(
        string manufacturer,
        string manufacturerPartNumber,
        string componentFullName,
        string? manufacturerLot = null,
        string? manufacturerSerialNumber = null
    )
    {
        Manufacturer = AssemblyValidation.FullName(manufacturer, nameof(Manufacturer));
        ManufacturerPartNumber = AssemblyValidation.NonEmpty(
            manufacturerPartNumber,
            nameof(ManufacturerPartNumber),
            128
        );
        ComponentFullName = AssemblyValidation.FullName(
            componentFullName,
            nameof(ComponentFullName)
        );
        ManufacturerLot = AssemblyValidation.OptionalNonEmpty(
            manufacturerLot,
            nameof(ManufacturerLot),
            255
        );
        ManufacturerSerialNumber = AssemblyValidation.OptionalNonEmpty(
            manufacturerSerialNumber,
            nameof(ManufacturerSerialNumber),
            128
        );
    }

    /// <summary>Original manufacturer.</summary>
    public string Manufacturer { get; }

    /// <summary>Manufacturer part number.</summary>
    public string ManufacturerPartNumber { get; }

    /// <summary>Component full name.</summary>
    public string ComponentFullName { get; }

    /// <summary>Optional manufacturer lot.</summary>
    public string? ManufacturerLot { get; }

    /// <summary>Optional manufacturer serial number.</summary>
    public string? ManufacturerSerialNumber { get; }

    internal override XElement Build() =>
        new(
            XmlNamespaces.Dw + "Child",
            new XElement(
                XmlNamespaces.Dw + "ComponentUnitIdentifier",
                new XAttribute("Manufacturer", Manufacturer),
                new XAttribute("ManufacturerPartNumber", ManufacturerPartNumber),
                ManufacturerLot == null ? null : new XAttribute("ManufacturerLot", ManufacturerLot),
                ManufacturerSerialNumber == null
                    ? null
                    : new XAttribute("ManufacturerSerialNumber", ManufacturerSerialNumber),
                new XAttribute("ComponentFullName", ComponentFullName)
            )
        );
}

/// <summary>Execution context of an assembly trace: exactly one operation run or sequence run reference.</summary>
public sealed class AssemblyTraceContext
{
    private AssemblyTraceContext(bool isOperationRun, string id)
    {
        IsOperationRun = isOperationRun;
        RunId = id;
    }

    /// <summary>True for an operation run reference, false for a sequence run reference.</summary>
    public bool IsOperationRun { get; }

    /// <summary>Referenced run identifier.</summary>
    public string RunId { get; }

    /// <summary>Reference an operation run by its object (the run is not built or exported).</summary>
    public static AssemblyTraceContext FromOperationRun(OperationRun operationRun) =>
        FromOperationRunId((operationRun ?? throw new ArgumentNullException(nameof(operationRun))).Id);

    /// <summary>Reference a sequence run by its object (the run is not built or exported).</summary>
    public static AssemblyTraceContext FromSequenceRun(SequenceRun sequenceRun) =>
        FromSequenceRunId((sequenceRun ?? throw new ArgumentNullException(nameof(sequenceRun))).Id);

    /// <summary>Reference an operation run by id.</summary>
    public static AssemblyTraceContext FromOperationRunId(string operationRunId) =>
        new(true, AssemblyValidation.Guid(operationRunId, nameof(operationRunId)));

    /// <summary>Reference a sequence run by id.</summary>
    public static AssemblyTraceContext FromSequenceRunId(string sequenceRunId) =>
        new(false, AssemblyValidation.Guid(sequenceRunId, nameof(sequenceRunId)));

    /// <summary>
    /// Create a context from candidate references; exactly one must be supplied.
    /// </summary>
    public static AssemblyTraceContext Create(
        OperationRun? operationRun = null,
        SequenceRun? sequenceRun = null,
        string? operationRunId = null,
        string? sequenceRunId = null
    )
    {
        var count =
            (operationRun != null ? 1 : 0)
            + (sequenceRun != null ? 1 : 0)
            + (operationRunId != null ? 1 : 0)
            + (sequenceRunId != null ? 1 : 0);
        if (count != 1)
        {
            throw new ArgumentException(
                "Exactly one of operationRun, sequenceRun, operationRunId or sequenceRunId must be provided."
            );
        }

        if (operationRun != null)
        {
            return FromOperationRun(operationRun);
        }

        if (sequenceRun != null)
        {
            return FromSequenceRun(sequenceRun);
        }

        return operationRunId != null
            ? FromOperationRunId(operationRunId)
            : FromSequenceRunId(sequenceRunId!);
    }

    internal XElement Build() =>
        new(
            XmlNamespaces.Dw + "AssemblyContext",
            new XElement(
                XmlNamespaces.Dw + (IsOperationRun ? "OperationRun" : "SequenceRun"),
                new XAttribute("Id", RunId)
            )
        );
}

/// <summary>
/// Assembly/disassembly fact mapped to the top-level AssemblyTrace element. Traces are immutable facts in
/// Proligent identified by parent (including reference designator), child, action and action time.
/// </summary>
public sealed class AssemblyTrace : Buildable
{
    /// <summary>Create an assembly trace. The action time is mandatory and never defaults to now.</summary>
    public AssemblyTrace(
        AssemblyTraceParent parent,
        AssemblyTraceChild child,
        AssemblyAction action,
        DateTime actionTime,
        AssemblyTraceContext? context = null,
        string? failureId = null,
        string? stationFullName = null,
        string? operatorName = null,
        int childComponentCount = 1
    )
    {
        Parent = parent ?? throw new ArgumentNullException(nameof(parent));
        Child = child ?? throw new ArgumentNullException(nameof(child));
        if (!Enum.IsDefined(action))
        {
            throw new ArgumentException(
                $"Unsupported assembly action '{(int)action}'.",
                nameof(action)
            );
        }

        Action = action;

        if (actionTime == default)
        {
            throw new ArgumentException("actionTime must be explicitly provided.", nameof(actionTime));
        }

        if (actionTime.Year < 1900 || actionTime.Year >= 2100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(actionTime),
                "actionTime must be between 1900-01-01 and 2100-01-01."
            );
        }

        ActionTime = actionTime;
        Context = context;
        FailureId = failureId == null ? null : AssemblyValidation.Guid(failureId, nameof(failureId));
        StationFullName = AssemblyValidation.OptionalFullName(
            stationFullName,
            nameof(stationFullName)
        );
        Operator = AssemblyValidation.OptionalNonEmpty(operatorName, nameof(operatorName), 255);

        if (childComponentCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(childComponentCount),
                childComponentCount,
                "childComponentCount cannot be negative."
            );
        }

        ChildComponentCount = childComponentCount;
    }

    /// <summary>Parent identity.</summary>
    public AssemblyTraceParent Parent { get; }

    /// <summary>Child identity.</summary>
    public AssemblyTraceChild Child { get; }

    /// <summary>Assembly action.</summary>
    public AssemblyAction Action { get; }

    /// <summary>Explicit action time. For SET_CURRENT, when the assembly was observed and entered.</summary>
    public DateTime ActionTime { get; }

    /// <summary>Optional execution context.</summary>
    public AssemblyTraceContext? Context { get; }

    /// <summary>Optional failure id.</summary>
    public string? FailureId { get; }

    /// <summary>Optional station full name.</summary>
    public string? StationFullName { get; }

    /// <summary>Optional operator.</summary>
    public string? Operator { get; }

    /// <summary>Number of identical child components (default 1, may be 0).</summary>
    public int ChildComponentCount { get; }

    /// <inheritdoc />
    public override XElement Build(Util? util = null)
    {
        util ??= Util.Default;

        var trace = new XElement(
            XmlNamespaces.Dw + "AssemblyTrace",
            new XAttribute("Action", Action.ToString()),
            new XAttribute("ActionTime", util.FormatDateTime((DateTime?)ActionTime)),
            FailureId == null ? null : new XAttribute("FailureId", FailureId),
            StationFullName == null ? null : new XAttribute("StationFullName", StationFullName),
            Operator == null ? null : new XAttribute("Operator", Operator),
            new XAttribute(
                "ChildComponentCount",
                ChildComponentCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
            ),
            Parent.Build(),
            Child.Build(),
            Context?.Build()
        );

        return trace;
    }
}


