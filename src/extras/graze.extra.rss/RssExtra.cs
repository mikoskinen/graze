using System.ComponentModel.Composition;
using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using graze.contracts;

namespace graze.extra.rss
{
    [Export(typeof(IExtra))]
    public class RssExtra : IExtra
    {
        public string KnownElement
        {
            get { return "Rss"; }
        }

        public object GetExtra(XElement element, dynamic currentModel)
        {
            var xAttribute = element.Attribute("Url");
            if (xAttribute == null)
                return null;

            var feed = SyndicationFeed.Load(XmlReader.Create(xAttribute.Value));

            foreach (var item in feed.Items)
            {
                if (item.Content != null)
                    continue;

                var content = GetContent(item);
                item.Content = new TextSyndicationContent(content, TextSyndicationContentKind.Html);
            }
            return feed;
        }

        public static string GetContent(SyndicationItem item)
        {
            var sb = new StringBuilder();
            foreach (SyndicationElementExtension extension in item.ElementExtensions)
            {
                var ele = extension.GetObject<XElement>();
                if (ele.Name.LocalName == "encoded" && ele.Name.Namespace.ToString().Contains("content"))
                {
                    sb.Append(ele.Value);
                }
            }

            return sb.ToString();
        }
    }
}
