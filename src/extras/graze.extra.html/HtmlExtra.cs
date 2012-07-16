using System.Net;
using System.Xml.Linq;
using graze.contracts;

namespace graze.extra.html
{
    public class HtmlExtra : IExtra
    {
        public string KnownElement
        {
            get { return "Html"; }
        }

        public object GetExtra(XElement element, dynamic currentModel)
        {
            var xAttribute = element.Attribute("Url");
            if (xAttribute == null)
                return null;

            var client = new WebClient();
            return client.DownloadString(xAttribute.Value);
        }
    }
}
