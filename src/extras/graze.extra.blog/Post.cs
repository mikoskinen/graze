using System;
using System.Collections.Generic;

namespace graze.extra.childpages
{
    public class Post
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public List<string> Tags { get; set; }
        public string Content { get; set; }
        public string Location { get; set; }
        public DateTime Time { get; set; }
        public string ParentPage { get { return "../index.html"; } }
        public string Slurg { get; set; }
    }
}