using System;
using System.Collections.Generic;
using System.Text;

namespace Proligent.XmlGenerator;

/// <summary>
/// Aggregator for all errors that occurred when validating an XML against an XSD.
/// </summary>
public class XsdValidationException : ApplicationException
{
    /// <summary>
    /// List of validation errors that were detected when validation an XSD against an XML.
    /// </summary>
    public List<string> XsdValidationErrors { get; internal set; }

    /// <summary>
    /// Default constructor.
    /// </summary>
    public XsdValidationException()
    {
        XsdValidationErrors = new List<string>();
    }

    /// <summary>
    /// Initializes an XsdValidationException using an existing exception.
    /// </summary>
    /// <param name="message">A message describing the problem that occurred.</param>
    /// <param name="innerException">A previously thrown exception related to the problem.</param>
    public XsdValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
        XsdValidationErrors = new List<string>();
    }

    /// <summary>
    /// Gets a message that describes the current exception.
    /// </summary>
    public override string Message
    {
        get
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(base.Message);

            foreach (var message in XsdValidationErrors)
            {
                stringBuilder.AppendLine(message);
            }

            return stringBuilder.ToString();
        }
    }
}
