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

        public string KnownElement
        {
            get { return "Markdown"; }
        }

        public object GetExtra(XElement element, dynamic currentModel)
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
        }
    }
}
