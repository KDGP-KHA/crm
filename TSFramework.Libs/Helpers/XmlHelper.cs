using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace TSFramework.Libs.Helpers
{
    public class XmlMinifierSettings
    {
        public bool RemoveEmptyLines { get; set; }
        public bool RemoveWhitespaceBetweenElements { get; set; }
        public bool CloseEmptyTags { get; set; }
        public bool RemoveComments { get; set; }

        public static XmlMinifierSettings Aggressive =>
            new XmlMinifierSettings
            {
                RemoveEmptyLines = true,
                RemoveWhitespaceBetweenElements = true,
                CloseEmptyTags = true,
                RemoveComments = true
            };

        public static XmlMinifierSettings NoMinification =>
            new XmlMinifierSettings
            {
                RemoveEmptyLines = false,
                RemoveWhitespaceBetweenElements = false,
                CloseEmptyTags = false,
                RemoveComments = false
            };
    }

    public class XmlHelper
    {
        /// <summary>
        ///     Chuyển từ xml sang class
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xmlInput"></param>
        /// <returns></returns>
        public static T DeserializeXmlToClass<T>(string xmlInput) where T : class
        {
            xmlInput = RemoveAllNamespaces(xmlInput);
            var ser = new XmlSerializer(typeof(T));

            using (var sr = new StringReader(xmlInput))
            {
                return (T)ser.Deserialize(sr);
            }
        }

        /// <summary>
        ///     Chuyển từ class sang xml
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objToSerialize"></param>
        /// <returns></returns>
        public static string SerializeClassToXml<T>(T objToSerialize)
        {
            var emptyNamespaces = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
            var xmlSerializer = new XmlSerializer(objToSerialize.GetType());
            var settings = new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true };

            using (var textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, objToSerialize, emptyNamespaces);
                return textWriter.ToString();
            }
        }

        /// <summary>
        ///     Parse từ object sang chuỗi xml
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SerializeToString<T>(T value)
        {
            var emptyNamespaces = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
            var serializer = new XmlSerializer(value.GetType());
            var settings = new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true };

            using (var stream = new StringWriter())
            using (var writer = XmlWriter.Create(stream, settings))
            {
                serializer.Serialize(writer, value, emptyNamespaces);
                return stream.ToString();
            }
        }

        public static string RemoveAllNamespaces(string xmlDocument)
        {
            var xmlDocumentWithoutNs = RemoveAllNamespaces(XElement.Parse(xmlDocument));

            return xmlDocumentWithoutNs.ToString();
        }

        private static XElement RemoveAllNamespaces(XElement xmlDocument)
        {
            if (xmlDocument.HasElements)
                return new XElement(xmlDocument.Name.LocalName,
                    xmlDocument.Elements().Select(RemoveAllNamespaces));
            var xElement = new XElement(xmlDocument.Name.LocalName) { Value = xmlDocument.Value };

            foreach (var attribute in xmlDocument.Attributes())
                xElement.Add(attribute);

            return xElement;
        }

        public static string Minify(string xml)
        {
            XmlMinifierSettings minifierSettings = XmlMinifierSettings.Aggressive;

            var originalXmlDocument = new XmlDocument
            {
                XmlResolver = null,
                PreserveWhitespace = !(minifierSettings.RemoveWhitespaceBetweenElements || minifierSettings.RemoveEmptyLines)
            };
            originalXmlDocument.Load(new MemoryStream(Encoding.UTF8.GetBytes(xml)));

            //remove comments first so we have less to compress later
            if (minifierSettings.RemoveComments)
            {
                foreach (XmlNode comment in originalXmlDocument.SelectNodes("//comment()"))
                {
                    comment.ParentNode?.RemoveChild(comment);
                }
            }

            if (minifierSettings.CloseEmptyTags)
            {
                foreach (XmlElement el in originalXmlDocument.SelectNodes("descendant::*[not(*) and not(normalize-space())]"))
                {
                    el.IsEmpty = true;
                }
            }

            if (minifierSettings.RemoveWhitespaceBetweenElements)
            {
                return originalXmlDocument.InnerXml;
            }

            var minified = new MemoryStream();
            originalXmlDocument.Save(minified);

            return Encoding.UTF8.GetString(minified.ToArray());
        }
    }
}