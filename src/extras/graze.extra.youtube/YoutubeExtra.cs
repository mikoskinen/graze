using System;
using System.ComponentModel.Composition;
using System.Xml.Linq;
using graze.contracts;

namespace graze.extra.youtube
{
    [Export(typeof(IExtra))]
    public class YoutubeExtra : IExtra
    {
        public bool CanProcess(XElement element)
        {
            return element != null && element.Name.LocalName.Equals("Youtube");
        }

        public object GetExtra(XElement element)
        {
            var urlAttribute = element.Attribute("Url");
            if (urlAttribute == null)
                return null;

            var width = 560;
            var widthAttribute = element.Attribute("Width");
            if (widthAttribute != null)
                width = Convert.ToInt32(widthAttribute.Value);

            var height = 315;
            var heightAttribute = element.Attribute("Height");
            if (heightAttribute != null)
                height = Convert.ToInt32(heightAttribute.Value);

            return string.Format("<iframe width=\"{0}\" height=\"{1}\" src=\"{2}\" frameborder=\"0\" allowfullscreen></iframe>", width, height, urlAttribute.Value) ;
        }
    }
}
