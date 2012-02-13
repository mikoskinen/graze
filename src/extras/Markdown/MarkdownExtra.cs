using System.IO;
using System.Web;
using System.Xml.Linq;

namespace graze.extras.Markdown
{
    public class MarkdownExtra : IExtra
    {
        private readonly string templateRootFolder;

        public MarkdownExtra(string templateRootFolder)
        {
            this.templateRootFolder = templateRootFolder;
        }

        public bool CanProcess(XElement element)
        {
            return element != null && element.Name.LocalName.Equals("Markdown");
        }

        public object GetExtra(XElement element)
        {
            var xAttribute = element.Attribute("Location");
            if (xAttribute == null)
                return string.Empty;

            var fileLocation = Path.Combine(templateRootFolder, xAttribute.Value);

            var options = new MarkdownSharp.MarkdownOptions
            {
                AutoHyperlink = true,
                AutoNewlines = true,
                EmptyElementSuffix = "/>",
                EncodeProblemUrlCharacters = false,
                LinkEmails = true,
                StrictBoldItalic = true
            };

            var parser = new MarkdownSharp.Markdown(options);

            var result = parser.Transform(File.ReadAllText(fileLocation));

            return result;
            //return new HtmlString(result);
        }
    }
}
