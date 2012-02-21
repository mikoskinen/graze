using System.ComponentModel.Composition;
using System.Net;
using System.Xml.Linq;
using graze.contracts;

namespace graze.extra.html
{
    [Export(typeof(IExtra))]
    public class HtmlExtra : IExtra
    {
        public bool CanProcess(XElement element)
        {
            return element != null && element.Name.LocalName.Equals("Html");
        }

        public object GetExtra(XElement element)
        {
            var xAttribute = element.Attribute("Url");
            if (xAttribute == null)
                return null;

            var client = new WebClient();
            return client.DownloadString(xAttribute.Value);
        }
    }
}
