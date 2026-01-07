using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using System.Globalization;

namespace Proligent.XmlGenerator;

/// <summary>
/// This class is responsible to collect the XSD validation errors that occur while reading an XML file.
/// </summary>
public static class XsdValidator
{
    #region Private members

    private static XsdValidationException? _xmlValidationException = null;
    private static string _originalXmlName = string.Empty;
    private static XmlSchemaSet? _schemaSet = null;

    #endregion

    #region Construction

    /// <summary>
    /// Initializes the XsdValidator with all the XSD files found in the embedded resource folder of an assembly.
    /// </summary>
    /// <param name="embeddedResourceFolder">The folder containing the XSD files as embedded resources.</param>
    public static void XsdValidator(string embeddedResourceFolder)
    {
        _xmlValidationException = null;
        _schemaSet = CreateSchemaSet(Assembly.GetExecutingAssembly(), embeddedResourceFolder);
    }

    #endregion

    #region Public methods

    /// <summary>Validate an XML document against the canonical DTO schema.</summary>
    /// <param name="filePath">Path to the XML document to validate.</param>
    /// <param name="schemaPath">Optional override to the Datawarehouse XSD.</param>
    public static void ValidateXml(string filePath, string? schemaPath = null)
    {
        var inputXmlReaderSettings = new XmlReaderSettings();

        using (CreateValidationScope(filePath, inputXmlReaderSettings))
        {
            var reader = XmlReader.Create(sourceXmlStream, inputXmlReaderSettings);
        }

        /*
        var (settings, readerFactory) = CreateValidationScope(schemaPath);

        using var reader = readerFactory(filePath, settings);
        while (reader.Read())
        {
            // The XmlReader will surface validation issues via the event handler configured in CreateValidator.
        }
        */
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
                Column: ex.LinePosition > 0 ? ex.LinePosition : null);
            return (metadata);
        }
        catch (Exception ex)
        {
            var metadata = new ValidationMetadata(
                IsValid: false,
                Message: ex.Message,
                Reason: ex.InnerException?.Message);
            return (metadata);
        }

        return new ValidationMetadata(IsValid: true, Message: "Validation was successful.");
    }


    #endregion

    #region Private methods


    /// <summary>
    /// Configures the XmlReaderSettings to validate XML files with the XmlValidator.
    /// When disposed, the IDisposable provided will throw if the XML file contained errors.
    /// </summary>
    /// <param name="originalXmlName">Name of the XML file to be validated within the scope.</param>
    /// <param name="readerSettings">XmlReaderSettings that will be used to read or write the XML file to be validated.
    /// The XmlReaderSettings will be modified to apply the XsdValidator's validation parameters.</param>
    /// <returns>.</returns>
    private static IDisposable CreateValidationScope(
        string originalXmlName,
        XmlReaderSettings readerSettings
    )
    {
        if (_xmlValidationException != null)
        {
            throw new ApplicationException("Coding error.");
        }

        _originalXmlName = originalXmlName;

        readerSettings.ValidationType = ValidationType.Schema;
        readerSettings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
        readerSettings.Schemas = _schemaSet;

        readerSettings.ValidationEventHandler += OnXsdValidationError;

        return new DisposableAction(() =>
        {
            readerSettings.ValidationEventHandler -= OnXsdValidationError;
            ThrowExceptionIfErrorFound();
        });
    }

    private static void ThrowExceptionIfErrorFound()
    {
        if (_xmlValidationException != null)
        {
            var exceptionToThrow = _xmlValidationException;
            _xmlValidationException = null;

            throw exceptionToThrow;
        }
    }

    private static XmlSchemaSet CreateSchemaSet(Assembly assembly, string embeddedResourceFolder)
    {
        var xsdFiles = assembly
            .GetManifestResourceNames()
            .Where(
                resourceName =>
                    resourceName.StartsWith(
                        embeddedResourceFolder,
                        StringComparison.InvariantCultureIgnoreCase
                    ) && resourceName.EndsWith(".xsd", StringComparison.InvariantCultureIgnoreCase)
            )
            .ToArray();

        if (xsdFiles.Length == 0)
        {
            throw new ApplicationException(
                "No xsd found in embedded resource folder " + embeddedResourceFolder
            );
        }

        var schemaSet = new XmlSchemaSet();

        foreach (var xsd in xsdFiles)
        {
            using var stream = assembly.GetManifestResourceStream(xsd)
                               ?? throw new ApplicationException($"Resource '{xsd}' not found.");

            var xs = XmlSchema.Read(stream, (sender, args) =>
            {
                // TODO Mari you can log error here
            }) ?? throw new ApplicationException($"Unable to read XML schema '{xsd}'.");

            xs.SourceUri = xsd;
        }

        return schemaSet;
    }

    private static void OnXsdValidationError(object? sender, ValidationEventArgs e)
    {
        if (_xmlValidationException == null)
            _xmlValidationException = new XsdValidationException(
                "XSD validation errors occurred",
                e.Exception
            );

        var elementName = (sender as XmlReader)?.LocalName ?? "Unknown";
        var nodeType = (sender as XmlReader)?.NodeType ?? XmlNodeType.None;

        var message = string.Format(
            CultureInfo.InvariantCulture,
            "{0}({1}:{2}): while processing the {3} {4}: {5}",
            _originalXmlName,
            e.Exception.LineNumber,
            e.Exception.LinePosition,
            nodeType,
            elementName,
            e.Exception.Message
        );

        _xmlValidationException.XsdValidationErrors.Add(message);
    }

    #endregion

    /// <summary>
    /// Logs the XSD files' URI and the namespaces defined in the schemas the Validator is using.
    /// </summary>
    public static void LogConfiguredSchemas()
    {
        foreach (XmlSchema s in _schemaSet.Schemas())
        {
            Console.WriteLine(
                $"Configuration: Using schema {s.SourceUri}: namespace {s.TargetNamespace}"
            );
        }
    }
}
