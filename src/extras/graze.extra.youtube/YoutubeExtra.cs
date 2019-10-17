using System;
using System.Xml.Linq;
using graze.contracts;

namespace graze.extra.youtube
{
    public class YoutubeExtra : IExtra
    {
        public string KnownElement
        {
            get { return "Youtube"; }
        }

        public object GetExtra(XElement element, dynamic currentModel)
        {
            var urlAttribute = element.Attribute("Url");

            if (urlAttribute == null)
            {
                return null;
            }

            var width = 560;
            var widthAttribute = element.Attribute("Width");

            if (widthAttribute != null)
            {
                width = Convert.ToInt32(widthAttribute.Value);
            }

            var height = 315;
            var heightAttribute = element.Attribute("Height");

            if (heightAttribute != null)
            {
                height = Convert.ToInt32(heightAttribute.Value);
            }

            return $"<iframe width=\"{width}\" height=\"{height}\" src=\"{urlAttribute.Value}\" frameborder=\"0\" allowfullscreen></iframe>";
        }
    }
}
