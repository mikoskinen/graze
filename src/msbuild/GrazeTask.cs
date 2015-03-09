namespace graze.msbuild
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel.Composition;
	using System.ComponentModel.Composition.Hosting;
	using System.Linq;
	using System.Text;
	using Microsoft.Build.Utilities;

	public class GrazeTask : Task
	{
		public string TemplateRoot { get; set; } = GrazeParameters.defaultTemplateRoot;
		public string OutputDirectory { get; set; } = GrazeParameters.defaultOutputRoot;
		/// <summary>
		/// Default: index.cshtml
		/// </summary>
		public string LayoutTemplate { get; set; } = GrazeParameters.defaultLayoutFile;
		public string OutputFile { get; set; } = GrazeParameters.defaultOutputPage;
		public bool CopyAssets { get; set; } = true;
		public bool CreateDirectories { get; set; } = true;
		public bool ProduceOutputFile { get; set; } = true;
		public string MSBuildProjectName { get; set; }

		private GrazeParameters CreateParameters()
		{
			if (this.CopyAssets != this.CreateDirectories)
				throw new NotSupportedException($"Configuration is not supported: CopyAssets ({this.CopyAssets}) != CreateDirectories ({this.CreateDirectories})");

			return new GrazeParameters(
				this.TemplateRoot,
				this.OutputFile,
				this.CreateDirectories,
				this.LayoutTemplate,
				this.OutputFile,
				this.ProduceOutputFile
			);
		}

		public override bool Execute()
		{
			try {
				var parameters = this.CreateParameters();
				var core = new Core(parameters);

				var extrasFolderCatalog = new DirectoryCatalog(@".\extras\");
				var currentAssemblyCatalog = new AssemblyCatalog(typeof(Program).Assembly);
				var aggregateCatalog = new AggregateCatalog(extrasFolderCatalog, currentAssemblyCatalog);

				var container = new CompositionContainer(aggregateCatalog);
				container.ComposeParts(core);

				core.Run();

				Log.LogMessage("{0} -graze-> {1}", this.MSBuildProjectName, parameters.OutputRoot);
				return true;
			} catch (Exception ex) {
				Console.Error.WriteLine(ex.ToString());

				return false;
			}
		}
	}
}
