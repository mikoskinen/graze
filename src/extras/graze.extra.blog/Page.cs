using System;
using System.Collections.Generic;

namespace graze.extra.childpages
{
    public class Page
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public List<Tag> Tags { get; set; }
        public List<string> TagNames { get; set; }
        public string Content { get; set; }
        public string Location { get; set; }
        public DateTime Time { get; set; }
        public string ParentPage { get { return "../index.html"; } }
        public string Slurg { get; set; }
        public string LayoutFile { get; set; }
        public Page PreviousPage { get; set; }
        public Page NextPage { get; set; }
    }
}