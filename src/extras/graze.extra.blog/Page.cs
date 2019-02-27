using System;
using System.Collections.Generic;

namespace graze.extra.childpages
{
    public class Page
    {
        public string Title
        {
            get; set;
        }
        public string Description
        {
            get; set;
        }
        public List<Tag> Tags
        {
            get; set;
        }
        public List<string> TagNames
        {
            get; set;
        }
        public string Content
        {
            get; set;
        }
        public string Location
        {
            get; set;
        }
        public DateTime Time
        {
            get; set;
        }
        public string ParentPage
        {
            get; set;
        }
        public string Slurg
        {
            get; set;
        }
        public string Group
        {
            get; set;
        }
        public string LayoutFile
        {
            get; set;
        }
        public Page PreviousPage
        {
            get; set;
        }
        public Page NextPage
        {
            get; set;
        }
        public List<Page> PagesInGroup
        {
            get; set;
        }

        public Page NextPageInGroup
        {
            get;set;
        }

        public Page PreviousPageInGroup
        {
            get;set;
        }

        public List<Tuple<int, string, string>> TableOfContents
        {
            get; set;
        }

        public int Order
        {
            get; set;
        }

        public string OriginalLocation
        {
            get; set;
        }

        public string OutputLocation
        {
            get; set;
        }

        public bool IncludeInNavigation
        {
            get; set;
        }
        public bool ContainsToc
        {
            get; set;
        }
    }
}
