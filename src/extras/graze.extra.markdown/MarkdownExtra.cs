using System.ComponentModel.Composition;
using System.IO;
using System.Xml.Linq;
using graze.contracts;

namespace graze.extra.markdown
{
    [Export(typeof(IExtra))]
    public class MarkdownExtra : IExtra
    {
        [Import(typeof(IFolderConfiguration))]
        private IFolderConfiguration configuration; 

        public bool CanProcess(XElement element)
        {
            return element != null && element.Name.LocalName.Equals("Markdown");
        }

        public object GetExtra(XElement element)
        {
            var xAttribute = element.Attribute("Location");
            if (xAttribute == null)
                return string.Empty;

            var fileLocation = Path.Combine(configuration.TemplateRootFolder, xAttribute.Value);

            var options = new MarkdownOptions
            {
                AutoHyperlink = true,
                AutoNewlines = true,
                EmptyElementSuffix = "/>",
                EncodeProblemUrlCharacters = false,
                LinkEmails = true,
                StrictBoldItalic = true
            };

            var parser = new Markdown(options);

            var result = parser.Transform(File.ReadAllText(fileLocation));

            return result;
            //return new HtmlString(result);
        }
    }
}
