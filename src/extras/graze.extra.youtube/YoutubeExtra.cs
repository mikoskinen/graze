using System;
using System.ComponentModel.Composition;
using System.Xml.Linq;
using graze.contracts;

namespace graze.extra.youtube
{
    [Export(typeof(IExtra))]
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
