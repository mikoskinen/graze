using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using graze.contracts;
using RazorLight;

namespace graze
{
    [Export(typeof(IFolderConfiguration))]
    [Export(typeof(IGenerator))]
    public class Core : IFolderConfiguration, IGenerator
    {
        private readonly Parameters _parameters;
        private static string _engineLock = "lock";
        private static RazorLightEngine _engine;

        [ImportMany(typeof(IExtra))]
        public IEnumerable<IExtra> Extras
        {
            get; set;
        }

        public string TemplateRootFolder
        {
            get
            {
                return _parameters.TemplateRoot;
            }
        }

        public string OutputRootFolder
        {
            get
            {
                return _parameters.OutputRoot;
            }
        }

        public string ConfigurationRootFolder
        {
            get
            {
                return Path.GetDirectoryName(_parameters.TemplateConfigurationFile);
            }
        }

        public Core()
            : this(Parameters.Default)
        {
        }

        public Core(Parameters parameters)
        {
            _parameters = parameters;

        }

        public void Run()
        {
            dynamic result = new ExpandoObject();

            Run(result);
        }

        public void Run(ExpandoObject startingModel)
        {
            SetExtras();

            // Filter out duplicates
            Extras = Extras.GroupBy(x => x.GetType().FullName).Select(x => x.First()).ToList();

            CreateSite(startingModel);
        }

        private void SetExtras()
        {
            if (Extras != null && Extras.Count() != 0)
            {
                return;
            }

            var extrasFolderCatalog = new DirectoryCatalog(_parameters.ExtrasFolder);
            var currentAssemblyCatalog = new AssemblyCatalog(typeof(Program).Assembly);
            var aggregateCatalog = new AggregateCatalog(extrasFolderCatalog, currentAssemblyCatalog);

            var container = new CompositionContainer(aggregateCatalog);
            container.ComposeParts(this);
        }

        private void CreateSite(ExpandoObject startingModel)
        {
            CreateOutputDirectory();

            var configuration = XDocument.Load(_parameters.TemplateConfigurationFile);

            var model = CreateModel(configuration, startingModel);

            if (_parameters.HandleDirectories)
            {
                common.DirCopy.Copy(_parameters.TemplateAssetsFolder, _parameters.OutputAssetsFolder);
            }

            if (_parameters.CopyOutputFile)
            {

                var template = File.ReadAllText(_parameters.TemplateLayoutFile);
                var result = GenerateOutput(model, template, _parameters.TemplateRoot);

                File.WriteAllText(_parameters.OutputHtmlPage, result);
            }
        }

        private void CreateOutputDirectory()
        {
            if (_parameters.HandleDirectories)
            {
                if (Directory.Exists(_parameters.OutputRoot))
                {
                    Directory.Delete(_parameters.OutputRoot, true);
                    Thread.Sleep(150);
                }

                Directory.CreateDirectory(_parameters.OutputRoot);
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
            {
                return result;
            }

            var elements = from node in dataElement.Elements()
                           select node;

            var delayedExecution = new ConcurrentBag<Tuple<IExtra, XElement>>();

            Parallel.ForEach(elements, new ParallelOptions { MaxDegreeOfParallelism = _parameters.MaxDegreeOfParallelism }, element =>
                                           {
                                               foreach (var extra in Extras)
                                               {
                                                   if (!CanProcess(extra, element))
                                                   {
                                                       continue;
                                                   }

                                                   if (RequiresDelayedExecution(extra))
                                                   {
                                                       delayedExecution.Add(new Tuple<IExtra, XElement>(extra, element));
                                                       continue;
                                                   }

                                                   ProcessExtra(result, element, extra);
                                               }
                                           });

            foreach (var tuple in delayedExecution)
            {
                ProcessExtra(result, tuple.Item2, tuple.Item1);
            }

            return result;
        }

        private bool RequiresDelayedExecution(IExtra extra)
        {
            return extra.GetType().GetCustomAttributes(typeof(DelayedExecutionAttribute), true).Any();
        }

        private static void ProcessExtra(dynamic result, XElement element, IExtra extra)
        {
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
                var name = "";

                if (element.HasElements)
                {
                    name = element.LastNode.ToString().Trim();
                }
                else
                {
                    name = element.Value.ToString(CultureInfo.InvariantCulture);
                }

                ((IDictionary<string, object>)result).Add(name, modelExtra);
            }
        }

        private static bool CanProcess(IExtra extra, XElement element)
        {
            return element != null && element.Name.LocalName.Equals(extra.KnownElement);
        }

        public static string GenerateOutput(ExpandoObject model, string template, string templateRoot)
        {
            // 14.2.2019 
            // Converted from RazorEngine to RazorLight. Caused two breaking changes:
            // 1. HTML is encoded by default and it can't be controlled through options, only through layout files.
            // 2. Have to use Layout instead of _Layout.

            var currentLocation = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            var templateRootPath = Path.Combine(currentLocation, templateRoot);

            var engine = GetRazorEngine(templateRootPath);

            var result = engine.CompileRenderAsync(template.GetHashCode().ToString(), template, model, model).Result;

            return result;
        }

        private static RazorLightEngine GetRazorEngine(string templateRootPath)
        {
            if (_engine != null)
            {
                return _engine;
            }

            lock (_engineLock)
            {
                if (_engine != null)
                {
                    return _engine;
                }

                _engine = new RazorLightEngineBuilder()
                    .UseMemoryCachingProvider()
                    .UseFilesystemProject(templateRootPath)
                    .Build();

                return _engine;
            }
        }

        public class Parameters
        {
            public string TemplateRoot
            {
                get; private set;
            }
            public string OutputRoot
            {
                get; private set;
            }
            private bool handleDirectories = true;
            public bool HandleDirectories
            {
                get
                {
                    return handleDirectories;
                }
                private set
                {
                    handleDirectories = value;
                }
            }

            private bool copyOutputFile = true;
            public bool CopyOutputFile
            {
                get
                {
                    return copyOutputFile;
                }
                private set
                {
                    copyOutputFile = value;
                }
            }

            public string TemplateConfigurationFile
            {
                get; private set;
            }
            public string TemplateLayoutFile
            {
                get; private set;
            }
            public string TemplateAssetsFolder
            {
                get; private set;
            }
            public string OutputHtmlPage
            {
                get; private set;
            }
            public string OutputAssetsFolder
            {
                get; private set;
            }
            public int MaxDegreeOfParallelism
            {
                get; private set;
            }
            public string ExtrasFolder
            {
                get; private set;
            }

            public Parameters(string templateRoot, string outputRoot, bool handleDirectories, string layoutFile, string outputPage, bool copyOutputFile)
                : this(
                    templateRoot ?? defaultTemplateRoot,
                    outputRoot ?? defaultOutputRoot,
                    handleDirectories,
                    layoutFile ?? Path.Combine(templateRoot ?? defaultTemplateRoot, defaultLayoutFile),
                    outputPage ?? Path.Combine(outputRoot ?? defaultOutputRoot, defaultOutputPage),
                    copyOutputFile,
                    Path.Combine(templateRoot ?? defaultTemplateRoot, defaultConfigurationFile),
                    Path.Combine(templateRoot ?? defaultTemplateRoot, defaultAssetsFolder),
                    Path.Combine(outputRoot ?? defaultOutputRoot, defaultAssetsFolder),
                    4,
                    @".\extras\"
                    )
            {
            }

            public Parameters(string templateRoot, string outputRoot, bool handleDirectories, string templateLayoutFile, string outputHtmlPage,
                bool copyOutputFile, string templateConfigurationFile, string templateAssetsFolder, string outputAssetsFolder, int maxDegreeOfParallelism, string extrasFolder)
            {
                TemplateRoot = templateRoot ?? defaultTemplateRoot;
                OutputRoot = outputRoot ?? defaultOutputRoot;
                HandleDirectories = handleDirectories;
                TemplateLayoutFile = templateLayoutFile ?? Path.Combine(templateRoot ?? defaultTemplateRoot, defaultLayoutFile);
                OutputHtmlPage = outputHtmlPage ?? Path.Combine(outputRoot ?? defaultOutputRoot, defaultOutputPage);
                CopyOutputFile = copyOutputFile;

                TemplateConfigurationFile = templateConfigurationFile ?? Path.Combine(TemplateRoot, defaultConfigurationFile);
                TemplateAssetsFolder = templateAssetsFolder ?? Path.Combine(TemplateRoot, defaultAssetsFolder);
                OutputAssetsFolder = outputAssetsFolder ?? Path.Combine(OutputRoot, defaultAssetsFolder);

                MaxDegreeOfParallelism = maxDegreeOfParallelism;
                ExtrasFolder = extrasFolder ?? @".\extras\";
            }

            public static Parameters Default
            {
                get
                {
                    return new Parameters(defaultTemplateRoot, defaultOutputRoot, true, null, null, true);
                }
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
            return GenerateOutput(model, template, _parameters.TemplateRoot);
        }
    }
}
