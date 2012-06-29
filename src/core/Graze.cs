using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.VisualBasic.Devices;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using graze.contracts;

namespace graze
{
    [Export(typeof(IFolderConfiguration))]
    [Export(typeof(IGenerator))]
    public class Core : IFolderConfiguration, IGenerator
    {
        private readonly Parameters parameters;

        [ImportMany(typeof(IExtra))]
        public IEnumerable<IExtra> Extras { get; set; }

        public string TemplateRootFolder
        {
            get { return parameters.TemplateRoot; }
        }

        public string OutputRootFolder
        {
            get { return parameters.OutputRoot; }
        }

        public Core()
            : this(Parameters.Default)
        {
        }

        public Core(Parameters parameters)
        {
            this.parameters = parameters;

        }

        public string Run()
        {
            dynamic result = new ExpandoObject();

            return Run(result);
        }

        public string Run(ExpandoObject startingModel)
        {
            var result = CreateSite(startingModel);

            return result;
        }

        private string CreateSite(ExpandoObject startingModel)
        {
            CreateOutputDirectory();

            var configuration = XDocument.Load(parameters.TemplateConfigurationFile);

            var model = CreateModel(configuration, startingModel);

            var template = File.ReadAllText(this.parameters.TemplateLayoutFile);
            var result = GenerateOutput(model, template);

            if (parameters.HandleDirectories)
            {
                new Computer().FileSystem.CopyDirectory(parameters.TemplateAssetsFolder, parameters.OutputAssetsFolder);
                File.WriteAllText(parameters.OutputHtmlPage, result);
            }

            return result;
        }

        private void CreateOutputDirectory()
        {
            if (parameters.HandleDirectories)
            {
                if (Directory.Exists(parameters.OutputRoot))
                {
                    Directory.Delete(parameters.OutputRoot, true);
                    Thread.Sleep(150);
                }

                Directory.CreateDirectory(parameters.OutputRoot);
                Thread.Sleep(150);
            }
        }

        /// <summary>
        /// Creates the site's model
        /// </summary>
        /// <param name="configuration">Configuration file</param>
        /// <param name="startingModel"> </param>
        /// <returns>Model</returns>
        private ExpandoObject CreateModel(XDocument configuration, ExpandoObject startingModel)
        {
            dynamic result = startingModel;

            var dataElement = configuration.Element("data");

            if (dataElement == null)
                return result;

            var elements = from node in dataElement.Elements()
                           select node;

            Parallel.ForEach(elements, element =>
                                           {
                                               var name = element.Value.ToString(CultureInfo.InvariantCulture);

                                               foreach (var extra in Extras)
                                               {
                                                   if (!CanProcess(extra, element))
                                                       continue;

                                                   var modelExtra = extra.GetExtra(element, result);

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
                                           });
            return result;
        }

        private static bool CanProcess(IExtra extra, XElement element)
        {
            return element != null && element.Name.LocalName.Equals(extra.KnownElement);
        }

        public static string GenerateOutput(ExpandoObject model, string template)
        {
            var config = new FluentTemplateServiceConfiguration(
                c => c.WithEncoding(RazorEngine.Encoding.Raw));

            string result;
            using (var service = new TemplateService(config))
            {
                result = service.Parse(template, model);
            }

            return result;
        }

        public class Parameters
        {
            public string TemplateRoot { get; private set; }
            public string OutputRoot { get; private set; }
            private bool handleDirectories = true;
            public bool HandleDirectories
            {
                get { return handleDirectories; }
                private set { handleDirectories = value; }
            }

            public string TemplateConfigurationFile { get; private set; }
            public string TemplateLayoutFile { get; private set; }
            public string TemplateAssetsFolder { get; private set; }
            public string OutputHtmlPage { get; private set; }
            public string OutputAssetsFolder { get; private set; }

            public Parameters(string templateRoot, string outputRoot, bool handleDirectories, string layoutFile, string outputPage)
                : this(templateRoot ?? defaultTemplateRoot,
                    outputRoot ?? defaultOutputRoot,
                    handleDirectories,
                    Path.Combine(templateRoot ?? defaultTemplateRoot, defaultConfigurationFile),
                    layoutFile ?? Path.Combine(templateRoot ?? defaultTemplateRoot, defaultLayoutFile),
                    Path.Combine(templateRoot ?? defaultTemplateRoot, defaultAssetsFolder),
                    outputPage ?? Path.Combine(outputRoot ?? defaultOutputRoot, defaultOutputPage),
                    Path.Combine(outputRoot ?? defaultOutputRoot, defaultAssetsFolder)) { }

            public Parameters(string templateRoot, string outputRoot, bool handleDirectories, string templateConfigurationFile, string templateLayoutFile, string templateAssetsFolder, string outputHtmlPage, string outputAssetsFolder)
            {
                TemplateRoot = templateRoot;
                OutputRoot = outputRoot;
                HandleDirectories = handleDirectories;
                TemplateConfigurationFile = templateConfigurationFile;
                TemplateLayoutFile = templateLayoutFile;
                TemplateAssetsFolder = templateAssetsFolder;
                OutputHtmlPage = outputHtmlPage;
                OutputAssetsFolder = outputAssetsFolder;
            }

            public static Parameters Default
            {
                get { return new Parameters(defaultTemplateRoot, defaultOutputRoot, true, null, null); }
            }

            private const string defaultTemplateRoot = "template";
            private const string defaultOutputRoot = "output";
            private const string defaultConfigurationFile = "configuration.xml";
            private const string defaultLayoutFile = "index.cshtml";
            private const string defaultAssetsFolder = "assets";
            private const string defaultOutputPage = "index.html";
        }

        string IGenerator.GenerateOutput(ExpandoObject model, string template)
        {
            return GenerateOutput(model, template);
        }
    }
}
