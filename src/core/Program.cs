using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using Mono.Options;

namespace graze
{
    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var parameters = GetParameters(args);
                var core = new Core(parameters);

                var extrasFolderCatalog = new DirectoryCatalog(@".\extras\");
                var currentAssemblyCatalog = new AssemblyCatalog(typeof(Program).Assembly);
                var aggregateCatalog = new AggregateCatalog(extrasFolderCatalog, currentAssemblyCatalog);

                var container = new CompositionContainer(aggregateCatalog);
                container.ComposeParts(core);

                core.Run();

                Console.WriteLine("Static site created successfully");

                if (Debugger.IsAttached)
                    Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());

                if (Debugger.IsAttached)
                    Console.ReadLine();

                Environment.ExitCode = 1;
            }
        }


        static Core.Parameters GetParameters(IEnumerable<string> args)
        {
            string templateRoot = null;
            string outputRoot = null;
            var shopHelp = false;

            var options = new OptionSet()
                                  {
                                      {"t|template=", "The template's root folder.", v => templateRoot = v},
                                      {"o|output=", "The output folder where static site is generated.", v => outputRoot = v},
                                      { "h|help",  "show this message and exit", v => shopHelp = true}
                                  };

            options.Parse(args);

            if (shopHelp)
            {
                options.WriteOptionDescriptions(Console.Out);
                if (Debugger.IsAttached)
                    Console.ReadLine();

                Environment.Exit(0);
            }

            return new Core.Parameters(templateRoot, outputRoot);

        }
    }

}
