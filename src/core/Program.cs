using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;

namespace graze
{
    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var core = new Core();

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
            }
        }
    }

}
