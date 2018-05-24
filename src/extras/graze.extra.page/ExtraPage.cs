﻿namespace graze.extra.page
{
	using System.Collections.Generic;
	using System.ComponentModel.Composition;
	using System.IO;
	using System.Xml.Linq;

	using graze.contracts;

	public sealed class ExtraPage : IExtra
	{
		[Import(typeof(IFolderConfiguration))]
		public IFolderConfiguration Configuration { get; set; }

		[Import(typeof(IGenerator))]
		public IGenerator Generator { get; set; }

		public string KnownElement
		{
			get
			{
				return "Page";
			}
		}

		public object GetExtra(XElement element, dynamic currentModel)
		{
			var layoutPath = element.Attribute("Layout");
			if (layoutPath == null)
				return null;

			var outputPath = element.Attribute("Output");
			var outputFileName = outputPath == null
				? Path.ChangeExtension(Path.GetFileName(layoutPath.Value), "html")
				: outputPath.Value;

			var outputFolder = Configuration.OutputRootFolder;
			Directory.CreateDirectory(outputFolder);

			var template = File.ReadAllText(Path.Combine(Configuration.TemplateRootFolder, layoutPath.Value));
			var output = Generator.GenerateOutput(currentModel, template);

			File.WriteAllText(Path.Combine(Configuration.OutputRootFolder, outputFileName), output);

			return new Dictionary<string, object>();
		}
	}
}
