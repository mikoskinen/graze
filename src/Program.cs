using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualBasic.Devices;

namespace graze
{
    class Program
    {
        private const string ConfigurationFile = "configuration.xml";

        private const string TemplateFolder = "template";
        private const string TemplateLayoutFile = @"template\index.cshtml";
        private const string TemplateAssetsFolder = @"template\assets";

        private const string OutputFolder = "output";
        private const string OutputHtmlPage = @"output\index.html";
        private const string OutputAssetsFolder = @"output\assets";

        static void Main(string[] args)
        {
            try
            {

                CreateSite();

                Console.WriteLine("Static site created successfully");

                if (Debugger.IsAttached)
                    Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());

                if (Debugger.IsAttached)
                    Console.ReadLine();
            }
        }

        private static void CreateSite()
        {
            var template = File.ReadAllText(TemplateLayoutFile);
            var model = GetModelFromXml();

            var result = RazorEngine.Razor.Parse(template, model);

            if (Directory.Exists(OutputFolder))
                Directory.Delete(OutputFolder, true);

            Directory.CreateDirectory(OutputFolder);

            File.WriteAllText(OutputHtmlPage, result);

            new Computer().FileSystem.CopyDirectory(TemplateAssetsFolder, OutputAssetsFolder);
        }

        private static ExpandoObject GetModelFromXml()
        {
            dynamic result = new ExpandoObject();

            var doc = XDocument.Load(ConfigurationFile);

            var nodes = from node in doc.Element("site").Elements()
                        select node;

            var modelProperties = (IDictionary<String, Object>)result;

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
                propertyCollection.Add(element.Name.LocalName, element.Value);
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
            var objProperties = (IDictionary<String, Object>)obj;

            foreach (var attribute in element.Attributes())
            {
                objProperties.Add(attribute.Name.LocalName, attribute.Value);
            }

            objProperties.Add(element.Name.LocalName, element.Value);

            return obj;
        }
    }

}
