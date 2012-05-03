using System.Collections.Generic;

namespace graze.extra.childpages
{
    public class Tag
    {
        public string Name { get; set; }
        public List<Page> Pages { get; set; }
        public int Count { get { return Pages.Count; } }
        public string Location { get; set; }
    }
}