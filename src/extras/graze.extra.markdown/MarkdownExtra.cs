using System.ComponentModel.Composition;
using System.IO;
using System.Xml.Linq;
using graze.contracts;
using Markdig;

namespace graze.extra.markdown
{
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
            {
                return string.Empty;
            }

            var fileLocation = Path.Combine(configuration.ConfigurationRootFolder, xAttribute.Value);

            var pipeline = new MarkdownPipelineBuilder()
                .UseBootstrap()
                .UseEmojiAndSmiley()
                .UseYamlFrontMatter()
                .UseAdvancedExtensions()
                .Build();

            var mdContent = File.ReadAllText(fileLocation);
            var result = Markdown.ToHtml(mdContent, pipeline);

            return result;
        }
    }
}
