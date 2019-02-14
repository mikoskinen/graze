using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using graze.common;
using graze.contracts;
using MarkdownSharp;

namespace graze.extra.childpages
{
    [DelayedExecutionAttribute]
    public class ChildPagesExtra : IExtra
    {
        [Import(typeof(IFolderConfiguration))]
        private IFolderConfiguration configuration;

        [Import(typeof(IGenerator))]
        private IGenerator generator;

        private readonly Dictionary<string, Tag> allTags = new Dictionary<string, Tag>();
        private string relativePathPrefix;

        public string KnownElement
        {
            get { return "ChildPages"; }
        }

        private string defaultLayoutFile;
        private bool shouldGenerateRss;

        public object GetExtra(XElement element, dynamic currentModel)
        {
            relativePathPrefix = GetRelativePathPrefix(element);
            defaultLayoutFile = GetDefaultLayoutFile(element);
            shouldGenerateRss = GetShouldGenerateRss(element);
            var childPagesFolder = GetChildPagesFolder(element);

            var childPagesOutputFolder = element.Value.ToLowerInvariant();
            var outputFolder = Path.Combine(configuration.OutputRootFolder, childPagesOutputFolder);

            Directory.CreateDirectory(outputFolder);

            var files = Directory.GetFiles(childPagesFolder, "*.md");

            var posts = files.Select(file => CreatePage(file, childPagesOutputFolder)).ToList();

            CreateChildPageIndex(posts, currentModel, outputFolder, Path.Combine(configuration.TemplateRootFolder, element.Attribute("IndexLayoutFile").Value));

            CreateTagPages(currentModel, outputFolder, childPagesOutputFolder, Path.Combine(configuration.TemplateRootFolder, element.Attribute("TagLayoutFile").Value), Path.Combine(configuration.TemplateRootFolder, element.Attribute("TagsIndexLayoutFile").Value));

            GeneratePages(posts.OrderBy(x => x.Time).ToList(), currentModel, outputFolder);

            CopyContent(childPagesFolder, outputFolder);

            var result = new ChildPages
            {
                Name = childPagesOutputFolder,
                Pages = posts.OrderBy(x => x.Time).ToList(),
                Tags = allTags.Select(x => x.Value).ToList(),

            };

            if (shouldGenerateRss)
                GenerateRss(element, result, outputFolder);

            return result;
        }

        private static string GetRelativePathPrefix(XElement element)
        {
            return element.Attribute("RelativePathPrefix").Value;
        }

        private string GetDefaultLayoutFile(XElement element)
        {
            if (element.Attribute("DefaultPageLayoutFile") == null)
                return null;

            return element.Attribute("DefaultPageLayoutFile").Value;
        }

        private bool GetShouldGenerateRss(XElement element)
        {
            if (element.Attribute("DefaultPageLayoutFile") == null)
                return false;

            return element.Attribute("RssGenerate").Value == "true";
        }

        private string GetChildPagesFolder(XElement element)
        {
            return Path.Combine(configuration.TemplateRootFolder, element.Attribute("Location") == null ? "pages" : element.Attribute("Location").Value);
        }

        private void CreateTagPages(dynamic currentModel, string outputFolder, string childPagesOutputFolder, string tagLayoutFile, string tagsIndexLayoutFile)
        {
            var tagsRoot = Path.Combine(outputFolder, "tags");
            Directory.CreateDirectory(tagsRoot);

            var modelDictionary = (IDictionary<string, object>)currentModel;
            foreach (var tagPair in allTags)
            {
                var tag = tagPair.Value;
                var tagPath = Path.Combine(tagsRoot, tag.Name);

                Directory.CreateDirectory(tagPath);

                var layout = File.ReadAllText(tagLayoutFile);

                if (modelDictionary.ContainsKey("Tag"))
                    modelDictionary.Remove("Tag");

                modelDictionary.Add("Tag", tag);

                var tagPageContent = generator.GenerateOutput(currentModel, layout);

                File.WriteAllText(Path.Combine(tagPath, "index.html"), tagPageContent);

                modelDictionary.Remove("Tag");
            }

            var tagIndexLayout = File.ReadAllText(tagsIndexLayoutFile);

            if (modelDictionary.ContainsKey("Tags"))
                modelDictionary.Remove("Tags");

            modelDictionary.Add("Tags", allTags.Select(t =>
                                                           {
                                                               var tag = t.Value;
                                                               tag.Location = Path.Combine(@"\", relativePathPrefix, childPagesOutputFolder, "tags", tag.Name).Replace(@"\", @"/");

                                                               return tag;
                                                           }).ToList());

            var tagsIndexPageContent = generator.GenerateOutput(currentModel, tagIndexLayout);

            File.WriteAllText(Path.Combine(tagsRoot, "index.html"), tagsIndexPageContent);

            modelDictionary.Remove("Tags");

        }

        private void CreateChildPageIndex(List<Page> pages, dynamic currentModel, string outputFolder, string layoutFile)
        {
            var modelDictionary = (IDictionary<string, object>)currentModel;

            if (modelDictionary.ContainsKey("Pages"))
                modelDictionary.Remove("Pages");

            if (modelDictionary.ContainsKey("PagesDesc"))
                modelDictionary.Remove("PagesDesc");

            if (modelDictionary.ContainsKey("PagesAsc"))
                modelDictionary.Remove("PagesAsc");

            modelDictionary.Add("Pages", pages);
            modelDictionary.Add("PagesDesc", pages.OrderByDescending(x => x.Time).ToList());
            modelDictionary.Add("PagesAsc", pages.OrderBy(x => x.Time).ToList());

            var indexFileLocation = Path.Combine(outputFolder, "index.html");

            var layoutContent = File.ReadAllText(layoutFile);

            var staticPage = generator.GenerateOutput(currentModel, layoutContent);

            File.WriteAllText(indexFileLocation, staticPage);

            modelDictionary.Remove("Pages");
            modelDictionary.Remove("PagesDesc");
            modelDictionary.Remove("PagesAsc");
        }

        private Page CreatePage(string file, string childPagesRootFolder)
        {
            var fileContent = File.ReadAllText(file);
            var postContent = Regex.Split(fileContent, "---");
            var metadata = postContent[1];
            var content = postContent[2];

            var meta = metadata.GetTags();

            var title = meta["title"];
            var permalink = meta["permalink"];
            var description = meta["description"];
            var tags = meta["tags"].Split(',').Select(tag => tag.Trim()).ToList();

            var layout = meta.ContainsKey("layout") ? meta["layout"] : defaultLayoutFile;

            if (string.IsNullOrWhiteSpace(layout))
                throw new ArgumentNullException("Layout", "No layout set either in the post or in the configuration.");

            var layoutFile = Path.Combine(configuration.TemplateRootFolder, layout);
            DateTime time;
            if (!DateTime.TryParse(meta["time"], CultureInfo.InvariantCulture, DateTimeStyles.None, out time))
            {
                var parsingTimeFailedPage = string.Format("Parsing time failed. Page {0}, Date {1}", file, meta["time"]);
                throw new Exception(parsingTimeFailedPage);
            }

            var post = new Page
            {
                Description = description,
                Location = Path.Combine(@"\", relativePathPrefix, childPagesRootFolder, permalink).Replace(@"\", @"/"),
                Title = title,
                Time = time,
                TagNames = tags,
                Slurg = permalink,
                LayoutFile = layoutFile,
                ParentPage = Path.Combine("/", relativePathPrefix, childPagesRootFolder)
            };

            var options = new MarkdownOptions
            {
                AutoHyperlink = true,
                AutoNewlines = true,
                EmptyElementSuffix = "/>",
                LinkEmails = true,
                StrictBoldItalic = true
            };

            var parser = new Markdown(options);

            post.Content = parser.Transform(content.Trim());

            foreach (var tag in post.TagNames)
            {
                if (allTags.ContainsKey(tag))
                {
                    allTags[tag].Pages.Add(post);
                    continue;
                }

                var value = new Tag { Name = tag, Pages = new List<Page> { post } };
                allTags.Add(tag, value);
            }

            return post;
        }

        private void GeneratePages(List<Page> pages, dynamic currentModel, string outputFolder)
        {
            // We are generating pages from the oldest to newest
            Page previousPage = null;

            for (var i = 0; i < pages.Count; i++)
            {
                var page = pages[i];
                page.Tags = new List<Tag>();

                foreach (var tagName in page.TagNames)
                {
                    page.Tags.Add(allTags[tagName]);
                }

                page.PreviousPage = previousPage;
                page.NextPage = pages.Count > i + 1 ? pages[i + 1] : null;

                GeneratePage(page, currentModel, outputFolder);
                previousPage = page;
            }
        }

        private void GeneratePage(Page page, dynamic currentModel, string outputFolder)
        {
            var modelDictionary = (IDictionary<string, object>)currentModel;
            if (modelDictionary.ContainsKey("Page"))
                modelDictionary.Remove("Page");

            modelDictionary.Add("Page", page);

            var postFolder = Path.Combine(outputFolder, page.Slurg);
            Directory.CreateDirectory(postFolder);

            var layoutContent = File.ReadAllText(page.LayoutFile);
            var staticPage = generator.GenerateOutput(currentModel, layoutContent);

            var postFileLocation = Path.Combine(outputFolder, page.Slurg, "index.html");

            File.WriteAllText(postFileLocation, staticPage);

            modelDictionary.Remove("Page");
        }

        private void CopyContent(string childPagesFolder, string outputFolder)
        {
            var contentSourceFolder = Path.Combine(childPagesFolder, "content");

            if (!Directory.Exists(contentSourceFolder))
                return;

            var contentOutputFolder = Path.Combine(outputFolder, "content");

            DirCopy.Copy(contentSourceFolder, contentOutputFolder);
        }

        private void GenerateRss(XElement element, ChildPages result, string outputFolder)
        {
            var rssUri = element.Attribute("RssUri").Value;
            var rssFeedName = element.Attribute("RssFeedName").Value;
            var rssAuthor = element.Attribute("RssAuthor").Value;
            var rssDescription = element.Attribute("RssDescription").Value;

            var feed = new SyndicationFeed(rssFeedName, rssDescription, new Uri(rssUri));
            feed.Authors.Add(new SyndicationPerson(rssAuthor));
            var feedItems = new List<SyndicationItem>();

            foreach (var page in result.Pages.Take(20))
            {
                var itemUri = rssUri + page.Location;
                var item = new SyndicationItem(page.Title, page.Content, new Uri(itemUri));
                item.PublishDate = new DateTimeOffset(page.Time);
                item.Id = page.Slurg;

                foreach (var tag in page.Tags)
                {
                    item.Categories.Add(new SyndicationCategory(tag.Name));
                }

                feedItems.Add(item);
            }

            feed.Items = feedItems;

            var settings = new XmlWriterSettings { Indent = true, IndentChars = "\t" };

            var feedLocation = Path.Combine(outputFolder, "rss.xml");
            using (var writer = XmlWriter.Create(feedLocation, settings))
            {
                feed.SaveAsRss20(writer);
            }

            result.Rss = result.Name + "/" + "rss.xml";
        }
    }
}
