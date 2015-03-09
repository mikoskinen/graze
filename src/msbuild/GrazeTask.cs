namespace graze.msbuild
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel.Composition;
	using System.ComponentModel.Composition.Hosting;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Text;
	using Microsoft.Build.Framework;
	using Microsoft.Build.Utilities;

	public class GrazeTask : Task
	{
		public string TemplateRoot { get; set; }
		public string OutputDirectory { get; set; }
		/// <summary>
		/// Default: index.cshtml
		/// </summary>
		public string LayoutTemplate { get; set; }
		public string OutputFile { get; set; }
		public bool CopyAssets { get; set; } = true;
		public bool CreateDirectories { get; set; } = true;
		public bool ProduceOutputFile { get; set; } = true;

		private GrazeParameters CreateParameters()
		{
			if (this.CopyAssets != this.CreateDirectories)
				throw new NotSupportedException($"Configuration is not supported: CopyAssets ({this.CopyAssets}) != CreateDirectories ({this.CreateDirectories})");

			return new GrazeParameters(
				this.TemplateRoot,
				this.OutputDirectory,
				this.CreateDirectories,
				this.LayoutTemplate,
				this.OutputFile,
				this.ProduceOutputFile
			);
		}

		public override bool Execute()
		{
			Log.LogMessage(MessageImportance.Normal, "parsing parameters...");
			var parameters = this.CreateParameters();
			Log.LogMessage(MessageImportance.Normal, "creating core...");
			var core = new Core(parameters);

			Log.LogMessage(MessageImportance.Normal, "composing parts...");
			var binRoot = Path.GetDirectoryName(typeof(Program).Assembly.Location);
			var extrasDir = Path.Combine(binRoot, "extras");
			var extrasFolderCatalog = new DirectoryCatalog(extrasDir);
			var currentAssemblyCatalog = new AssemblyCatalog(typeof(Program).Assembly);
			var aggregateCatalog = new AggregateCatalog(extrasFolderCatalog, currentAssemblyCatalog);

			var container = new CompositionContainer(aggregateCatalog);
			container.ComposeParts(core);

			Log.LogMessage(MessageImportance.Normal, "running Graze...");
			this.WithAssemblyResolveHack(() => core.Run());

			var fullOutputPath = Path.GetFullPath(parameters.OutputRoot);
			Log.LogMessage("graze -> {1}", fullOutputPath);
			return true;
		}

		void WithAssemblyResolveHack(Action action)
		{
			AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolveHack;
			try {
				action();
			} finally {
				AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolveHack;
			}
		}

		Assembly AssemblyResolveHack(object sender, ResolveEventArgs args) {
			Log.LogMessage(MessageImportance.Low, "resolve hack for {0}", args.Name);
			return Assembly.Load(args.Name);
		}
	}
}
