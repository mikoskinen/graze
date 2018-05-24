﻿using System;
using System.Collections.Concurrent;
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
using RazorEngine.Text;
using graze.contracts;

namespace graze
{
    [Export(typeof(IFolderConfiguration))]
    [Export(typeof(IGenerator))]
    public class Core : IFolderConfiguration, IGenerator
    {
        private readonly GrazeParameters parameters;

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
            : this(GrazeParameters.Default)
        {
        }

        public Core(GrazeParameters parameters)
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
                new Computer().FileSystem.CopyDirectory(parameters.TemplateAssetsFolder, parameters.OutputAssetsFolder);
            
            if (parameters.CopyOutputFile)
                File.WriteAllText(parameters.OutputHtmlPage, result);

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

            var delayedExecution = new ConcurrentBag<Tuple<IExtra, XElement>>();
            
            Parallel.ForEach(elements, new ParallelOptions { MaxDegreeOfParallelism = parameters.MaxDegreeOfParallelism }, element =>
                                           {
                                               foreach (var extra in Extras)
                                               {
                                                   if (!CanProcess(extra, element))
                                                       continue;

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
                ProcessExtra(result, tuple.Item2, tuple.Item1 );
            }

            return result;
        }

        private bool RequiresDelayedExecution(IExtra extra)
        {
            return extra.GetType().GetCustomAttributes(typeof (DelayedExecutionAttribute), true).Any();
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
                    ((IDictionary<string, object>) result).Add(keyValuePair.Key, keyValuePair.Value);
                }
            }
            else
            {
                var name = element.Value.ToString(CultureInfo.InvariantCulture);
                ((IDictionary<string, object>)result).Add(name, modelExtra);
            }
        }

        private static bool CanProcess(IExtra extra, XElement element)
        {
            return element != null && element.Name.LocalName.Equals(extra.KnownElement);
        }

        public static string GenerateOutput(ExpandoObject model, string template)
        {
			if (model == null)
				throw new ArgumentNullException("model");
			if (template == null)
				throw new ArgumentNullException("template");

            var config = new TemplateServiceConfiguration
                             {
                                 EncodedStringFactory = new RawStringFactory(),
                                 Resolver = new DelegateTemplateResolver(name =>
                                                                             {
                                                                                 var file = name;
                                                                                 var content = File.ReadAllText(file);
                                                                                 return content;
                                                                             })
                             };

            string result;
            using (var service = new TemplateService(config))
            {
                result = service.Parse(template, model);
            }

            return result;
        }

        string IGenerator.GenerateOutput(ExpandoObject model, string template)
        {
            return GenerateOutput(model, template);
        }
    }
}
