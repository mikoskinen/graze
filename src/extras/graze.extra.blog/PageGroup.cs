using System.Collections.Generic;

namespace graze.extra.childpages
{
    public class PageGroup
    {
        public string Key
        {
            get; set;
        }
        public string Name
        {
            get; set;
        }
        public List<Page> Pages
        {
            get; set;
        }
        public int Order
        {
            get; set;
        }
        public string DefaultLocation
        {
            get; set;
        }

        public bool ShowChildPagesIfEmptyOrOne
        {
            get;set;
        }

        public bool ShowTocIfOnePage
        {
            get; set;
        }

        public bool ReplaceGroupNameWithPageNameIfOnePage
        {
            get; set;
        }
    }
}
