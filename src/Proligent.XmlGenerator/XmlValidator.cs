using System.Collections.Concurrent;
using System.Xml;
using System.Xml.Schema;

namespace Proligent.XmlGenerator;

/// <summary>Validation helpers for Proligent Datawarehouse XML.</summary>
public static class XmlValidator
{
    private static readonly ConcurrentDictionary<string, XmlSchemaSet> SchemaCache = new(
        StringComparer.OrdinalIgnoreCase
    );

    /// <summary>Validate an XML document against the canonical DTO schema.</summary>
    /// <param name="filePath">Path to the XML document to validate.</param>
    /// <param name="schemaPath">Optional override to the Datawarehouse XSD.</param>
    public static void ValidateXml(string filePath, string? schemaPath = null)
    {
        var (settings, readerFactory) = CreateValidator(schemaPath);

        using var reader = readerFactory(filePath, settings);
        while (reader.Read())
        {
            // The XmlReader will surface validation issues via the event handler configured in CreateValidator.
        }
    }

    /// <summary>
    /// Validate an XML document and return metadata instead of raising an exception.
    /// If there are multiple problems, it only reports the first one it finds.
    /// </summary>
    /// <param name="filePath">Path to the XML document to validate.</param>
    /// <param name="schemaPath">Optional override to the Datawarehouse XSD.</param>
    public static ValidationMetadata ValidateXmlSafe(string filePath, string? schemaPath = null)
    {
        try
        {
            ValidateXml(filePath, schemaPath);
        }
        catch (XmlSchemaValidationException ex)
        {
            var metadata = new ValidationMetadata(
                IsValid: false,
                Message: ex.Message,
                Reason: ex.Message,
                Path: ex.SourceUri ?? filePath,
                Line: ex.LineNumber > 0 ? ex.LineNumber : null,
                Column: ex.LinePosition > 0 ? ex.LinePosition : null
            );
            return (metadata);
        }
        catch (Exception ex)
        {
            var metadata = new ValidationMetadata(
                IsValid: false,
                Message: ex.Message,
                Reason: ex.InnerException?.Message
            );
            return (metadata);
        }

        return new ValidationMetadata(IsValid: true, Message: "Validation was successful.");
    }

    private static (
        XmlReaderSettings Settings,
        Func<string, XmlReaderSettings, XmlReader> Factory
    ) CreateValidator(string? schemaPath)
    {
        var resolvedSchema = ResolveSchema(schemaPath);
        var settings = new XmlReaderSettings
        {
            Schemas = resolvedSchema,
            ValidationType = ValidationType.Schema,
            ValidationFlags =
                XmlSchemaValidationFlags.ProcessSchemaLocation
                | XmlSchemaValidationFlags.ReportValidationWarnings,
        };

        // Track the current element path to improve error metadata.
        var elementStack = new Stack<string>();

        settings.ValidationEventHandler += (_, args) =>
        {
            string path = "/" + string.Join("/", elementStack.Reverse());
            throw new XmlSchemaValidationException(
                args.Message,
                args.Exception,
                args.Exception?.LineNumber ?? 0,
                args.Exception?.LinePosition ?? 0
            );
        };

        return (settings, ReaderFactory);

        XmlReader ReaderFactory(string path, XmlReaderSettings xmlReaderSettings)
        {
            var reader = XmlReader.Create(path, xmlReaderSettings);
            return new TrackingXmlReader(reader, elementStack);
        }
    }

    private static XmlSchemaSet ResolveSchema(string? schemaPath)
    {
        var path = schemaPath ?? Path.Combine(AppContext.BaseDirectory, "Xsd");

        string[] xsdFiles = Directory.GetFiles(path, "*.xsd", SearchOption.AllDirectories);

        if (xsdFiles.Length == 0)
        {
            throw new ApplicationException("No xsd found in resource folder " + path);
        }

        var schemaSet = new XmlSchemaSet();

        foreach (var xsd in xsdFiles)
        {
            using var stream = File.OpenRead(xsd);
            var schema = XmlSchema.Read(
                stream,
                (sender, args) => {
                    //Here you can log which xsd were loaded.
                }
            );
            if (schema != null)
            {
                schema.SourceUri = xsd;
                schemaSet.Add(schema);
            }
        }

        return schemaSet;
    }

    /// <summary>
    /// Wrapper that keeps track of the current XML path so validation errors can surface a location.
    /// </summary>
    private sealed class TrackingXmlReader : XmlReader
    {
        private readonly XmlReader _inner;
        private readonly Stack<string> _path;

        public TrackingXmlReader(XmlReader inner, Stack<string> path)
        {
            _inner = inner;
            _path = path;
        }

        public override bool Read()
        {
            var result = _inner.Read();
            if (!result)
            {
                return false;
            }

            if (_inner.NodeType == XmlNodeType.Element)
            {
                _path.Push(_inner.Name);
                if (_inner.IsEmptyElement)
                {
                    _path.Pop();
                }
            }
            else if (_inner.NodeType == XmlNodeType.EndElement && _path.Count > 0)
            {
                _path.Pop();
            }

            return true;
        }

        #region XmlReader delegation
        public override int AttributeCount => _inner.AttributeCount;
        public override string BaseURI => _inner.BaseURI;
        public override int Depth => _inner.Depth;
        public override bool EOF => _inner.EOF;
        public override bool HasValue => _inner.HasValue;
        public override bool IsDefault => _inner.IsDefault;
        public override bool IsEmptyElement => _inner.IsEmptyElement;
        public override string LocalName => _inner.LocalName;
        public override string NamespaceURI => _inner.NamespaceURI;
        public override XmlNameTable NameTable => _inner.NameTable;
        public override XmlNodeType NodeType => _inner.NodeType;
        public override string Prefix => _inner.Prefix;
        public override char QuoteChar => _inner.QuoteChar;
        public override ReadState ReadState => _inner.ReadState;
        public override string Value => _inner.Value;
        public override string XmlLang => _inner.XmlLang;
        public override XmlSpace XmlSpace => _inner.XmlSpace;
        public override string GetAttribute(int i) => _inner.GetAttribute(i);
        public override string? GetAttribute(string name) => _inner.GetAttribute(name);
        public override string? GetAttribute(string name, string? namespaceURI) => _inner.GetAttribute(name, namespaceURI);
        public override string? LookupNamespace(string prefix) => _inner.LookupNamespace(prefix);
        public override bool MoveToAttribute(string name) => _inner.MoveToAttribute(name);
        public override bool MoveToAttribute(string name, string? ns) => _inner.MoveToAttribute(name, ns);
        public override bool MoveToElement() => _inner.MoveToElement();
        public override bool MoveToFirstAttribute() => _inner.MoveToFirstAttribute();
        public override bool MoveToNextAttribute() => _inner.MoveToNextAttribute();
        public override bool ReadAttributeValue() => _inner.ReadAttributeValue();
        public override void ResolveEntity() => _inner.ResolveEntity();
        public override void Close() => _inner.Close();
        #endregion
    }
}
