namespace graze
{
	using System.IO;

	public class GrazeParameters
	{
		public string TemplateRoot { get; set; }
		public string OutputRoot { get; set; }
		private bool handleDirectories = true;
		public bool HandleDirectories
		{
			get { return handleDirectories; }
			set { handleDirectories = value; }
		}

		private bool copyOutputFile = true;
		public bool CopyOutputFile
		{
			get { return copyOutputFile; }
			set { copyOutputFile = value; }
		}

		public string TemplateConfigurationFile { get; set; }
		public string TemplateLayoutFile { get; set; }
		public string TemplateAssetsFolder { get; set; }
		public string OutputHtmlPage { get; set; }
		public string OutputAssetsFolder { get; set; }
		public int MaxDegreeOfParallelism { get; set; }

		public GrazeParameters(string templateRoot, string outputRoot, bool handleDirectories, string layoutFile, string outputPage, bool copyOutputFile)
			: this(templateRoot ?? defaultTemplateRoot,
				outputRoot ?? defaultOutputRoot,
				handleDirectories,
				Path.Combine(templateRoot ?? defaultTemplateRoot, defaultConfigurationFile),
				layoutFile ?? Path.Combine(templateRoot ?? defaultTemplateRoot, defaultLayoutFile),
				Path.Combine(templateRoot ?? defaultTemplateRoot, defaultAssetsFolder),
				outputPage ?? Path.Combine(outputRoot ?? defaultOutputRoot, defaultOutputPage),
				Path.Combine(outputRoot ?? defaultOutputRoot, defaultAssetsFolder),
			   copyOutputFile, 4)
		{ }

		public GrazeParameters(string templateRoot, string outputRoot, bool handleDirectories, string templateConfigurationFile, string templateLayoutFile, string templateAssetsFolder, string outputHtmlPage, string outputAssetsFolder,
			bool copyOutputFile, int maxDegreeOfParallelism)
		{
			TemplateRoot = templateRoot;
			OutputRoot = outputRoot;
			HandleDirectories = handleDirectories;
			TemplateConfigurationFile = templateConfigurationFile;
			TemplateLayoutFile = templateLayoutFile;
			TemplateAssetsFolder = templateAssetsFolder;
			OutputHtmlPage = outputHtmlPage;
			OutputAssetsFolder = outputAssetsFolder;
			CopyOutputFile = copyOutputFile;
			MaxDegreeOfParallelism = maxDegreeOfParallelism;
		}

		public static GrazeParameters Default
		{
			get { return new GrazeParameters(defaultTemplateRoot, defaultOutputRoot, true, null, null, true); }
		}

		public const string defaultTemplateRoot = "template";
		public const string defaultOutputRoot = "output";
		public const string defaultConfigurationFile = "configuration.xml";
		public const string defaultLayoutFile = "index.cshtml";
		public const string defaultAssetsFolder = "assets";
		public const string defaultOutputPage = "index.html";
	}
}