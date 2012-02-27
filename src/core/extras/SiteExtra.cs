using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Linq;
using System.Xml.Linq;
using graze.contracts;

namespace graze.extras
{
    [Export(typeof(IExtra))]
    public class SiteExtra : IExtra
    {
        public string KnownElement
        {
            get { return "site"; }
        }

        public object GetExtra(XElement element)
        {
            var result = new Dictionary<string, object>();

            var nodes = from node in element.Elements()
                        select node;

            var modelProperties = (IDictionary<string, object>)result;

            foreach (var xElement in nodes)
                AddPropertyToModel(xElement, modelProperties);

            return result;
        }

        private static void AddPropertyToModel(XElement element, IDictionary<string, object> propertyCollection)
        {
            var hasDescendants = element.Descendants().Count() > 1;

            if (hasDescendants)
            {
                // Parse a collection of values, like <reviews><review></review><reviews></review></reviews>
                var arrayOfValues = CreateArray(element);
                propertyCollection.Add(element.Name.LocalName, arrayOfValues);
            }
            else
                propertyCollection.Add(element.Name.LocalName, GetPropertyValue(element));
        }

        private static List<object> CreateArray(XElement element)
        {
            return element.Descendants().Select(GetPropertyValue).ToList();
        }

        private static object GetPropertyValue(XElement element)
        {
            var isComplexValue = !element.Attributes().Any();
            if (isComplexValue)
                return element.Value;

            var obj = new ExpandoObject();
            var objProperties = (IDictionary<string, object>)obj;

            foreach (var attribute in element.Attributes())
            {
                objProperties.Add(attribute.Name.LocalName, attribute.Value);
            }

            objProperties.Add(element.Name.LocalName, element.Value);

            return obj;
        }
    }
}
