using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualBasic.Devices;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using graze.contracts;

namespace graze
{
    [Export(typeof(IFolderConfiguration))]
    public class Core : IFolderConfiguration
    {
        private const string TemplateRoot = @"template\";
        private const string TemplateConfiguration = @"template\configuration.xml";
        private const string TemplateLayoutFile = @"template\index.cshtml";
        private const string TemplateAssetsFolder = @"template\assets";

        private const string OutputFolder = "output";
        private const string OutputHtmlPage = @"output\index.html";
        private const string OutputAssetsFolder = @"output\assets";

        [ImportMany(typeof(IExtra))]
        public IEnumerable<IExtra> Extras { get; set; }

        public string TemplateRootFolder
        {
            get { return TemplateRoot; }
        }

        public void Run()
        {
            CreateSite();
        }

        private void CreateSite()
        {
            var configuration = XDocument.Load(TemplateConfiguration);

            var model = CreateModel(configuration);

            var result = GenerateOutput(model);

            if (Directory.Exists(OutputFolder))
                Directory.Delete(OutputFolder, true);

            Directory.CreateDirectory(OutputFolder);

            File.WriteAllText(OutputHtmlPage, result);

            new Computer().FileSystem.CopyDirectory(TemplateAssetsFolder, OutputAssetsFolder);
        }

        /// <summary>
        /// Creates the site's model
        /// </summary>
        /// <param name="configuration">Configuration file</param>
        /// <returns>Model</returns>
        private ExpandoObject CreateModel(XDocument configuration)
        {
            dynamic result = new ExpandoObject();

            var dataElement = configuration.Element("data");

            if (dataElement == null)
                return result;

            var elements = from node in dataElement.Elements()
                           select node;

            foreach (var element in elements)
            {

                var name = element.Value.ToString(CultureInfo.InvariantCulture);

                foreach (var extra in Extras)
                {
                    if (!CanProcess(extra, element))
                        continue;

                    var modelExtra = extra.GetExtra(element);

                    var resultDictionary = modelExtra as IDictionary<string, object>;
                    var containsMultipleModelProperties = resultDictionary != null;
                    if (containsMultipleModelProperties)
                    {
                        foreach (var keyValuePair in resultDictionary)
                        {
                            ((IDictionary<string, object>)result).Add(keyValuePair.Key, keyValuePair.Value);
                        }
                    }
                    else
                    {
                        ((IDictionary<string, object>)result).Add(name, modelExtra);

                    }
                }
            }
            return result;
        }

        private static bool CanProcess(IExtra extra, XElement element)
        {
            return element != null && element.Name.LocalName.Equals(extra.KnownElement);
        }

        /// <summary>
        /// Generates the static output based on the model and the template
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private static string GenerateOutput(ExpandoObject model)
        {
            var template = File.ReadAllText(TemplateLayoutFile);

            var config = new FluentTemplateServiceConfiguration(
                c => c.WithEncoding(RazorEngine.Encoding.Raw));

            string result;
            using (var service = new TemplateService(config))
            {
                result = service.Parse(template, model);
            }
            return result;
        }
    }
}
