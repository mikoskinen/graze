using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualBasic.Devices;
using RazorEngine;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using graze.contracts;

namespace graze
{
    [Export(typeof(IFolderConfiguration))]
    public class Program : IFolderConfiguration
    {
        private const string TemplateRoot = @"template\";
        private const string TemplateConfiguration = @"template\configuration.xml";
        private const string TemplateLayoutFile = @"template\index.cshtml";
        private const string TemplateAssetsFolder = @"template\assets";

        private const string OutputFolder = "output";
        private const string OutputHtmlPage = @"output\index.html";
        private const string OutputAssetsFolder = @"output\assets";

        public string TemplateRootFolder
        {
            get { return TemplateRoot; }
        }

        [ImportMany(typeof(IExtra))]
        public IEnumerable<IExtra> Extras { get; set; }

        static void Main(string[] args)
        {
            try
            {
                var program = new Program();
                program.Run();

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

        public void Run()
        {
            var catalog = new DirectoryCatalog(@".\extras\");
            var container = new CompositionContainer(catalog);
            container.ComposeParts(this);

            CreateSite();            
        }

        private void CreateSite()
        {
            var configuration = XDocument.Load(TemplateConfiguration);

            var model = GetModelFromXml(configuration);
            model = AddExtras(configuration, model);

            var result = GenerateOutput(model);

            if (Directory.Exists(OutputFolder))
                Directory.Delete(OutputFolder, true);

            Directory.CreateDirectory(OutputFolder);

            File.WriteAllText(OutputHtmlPage, result);

            new Computer().FileSystem.CopyDirectory(TemplateAssetsFolder, OutputAssetsFolder);
        }

        private static string GenerateOutput(ExpandoObject model)
        {
            var template = File.ReadAllText(TemplateLayoutFile);

            var config = new FluentTemplateServiceConfiguration(
                c => c.WithEncoding(Encoding.Raw));

            string result;
            using (var service = new TemplateService(config))
            {
                result = service.Parse(template, model);
            }
            return result;
        }

        /// <summary>
        /// Parses all the elements inside the "site/data" -element
        /// </summary>
        /// <param name="configuration">Configuration file</param>
        /// <returns>Model</returns>
        private static ExpandoObject GetModelFromXml(XDocument configuration)
        {
            dynamic result = new ExpandoObject();

            var nodes = from node in configuration.Element("site").Element("data").Elements()
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
            var objProperties = (IDictionary<String, Object>)obj;

            foreach (var attribute in element.Attributes())
            {
                objProperties.Add(attribute.Name.LocalName, attribute.Value);
            }

            objProperties.Add(element.Name.LocalName, element.Value);

            return obj;
        }

        /// <summary>
        /// Adds data to model based on elements inside the "site/extra" element
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        private ExpandoObject AddExtras(XDocument configuration, ExpandoObject model)
        {
            var extraElement = configuration.Element("site").Element("extra");

            if (extraElement == null)
                return model;
                               
            var elements = from node in extraElement.Elements()
                           select node;

            foreach (var element in elements)
            {
                var name = element.Value.ToString(CultureInfo.InvariantCulture);

                foreach (var extra in Extras)
                {
                    if (!extra.CanProcess(element))
                        continue;

                    ((IDictionary<String, Object>)model).Add(name, extra.GetExtra(element));
                }
            }

            return model;
        }
    }

}
