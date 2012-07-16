using System.Collections.Generic;
using System.IO;

namespace graze.extra.childpages
{
    public class ChildPages
    {
        public List<Page> Pages { get; set; }
        public List<Tag> Tags { get; set; }
        public string Name { get; set; }
        public string Rss { get; set; }

        public string Location
        {
            get { return Path.Combine(Name, "index.html"); }
        }
    }
}