using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Proligent.XmlGenerator.Tests
{
    public static class XmlTestUtils
    {
        public static string NormalizeXml(string xml)
        {
            var doc = XDocument.Parse(xml);

            var readerSettings = new XmlReaderSettings
            {
                IgnoreWhitespace = true,
                IgnoreComments = true,
            };

            var writerSettings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = false,
            };

            var sb = new StringBuilder();

            using var reader = XmlReader.Create(new StringReader(doc.ToString()), readerSettings);
            using var writer = XmlWriter.Create(sb, writerSettings);

            while (reader.Read())
                writer.WriteNode(reader, true);

            writer.Flush();
            return sb.ToString();
        }
    }
}
