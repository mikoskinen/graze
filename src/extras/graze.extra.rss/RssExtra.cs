using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ServiceModel.Syndication;
using System.Xml;
using System.Xml.Linq;
using graze.contracts;

namespace graze.extra.rss
{
    [Export(typeof(IExtra))]
    public class RssExtra : IExtra
    {
        public bool CanProcess(XElement element)
        {
            return element != null && element.Name.LocalName.Equals("Rss");
        }

        public object GetExtra(XElement element)
        {
            var xAttribute = element.Attribute("Url");
            if (xAttribute == null)
                return null;

            var feed = SyndicationFeed.Load(XmlReader.Create(xAttribute.Value));

            return feed;
        }
    }
}
