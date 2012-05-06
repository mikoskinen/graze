using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using graze.contracts;

namespace graze.extra.childpages
{
    [Export(typeof(IExtra))]
    public class ChildPagesExtra : IExtra
    {
        [Import(typeof(IFolderConfiguration))]
        private IFolderConfiguration configuration;

        [Import(typeof(IGenerator))]
        private IGenerator generator;

        private string relativePathPrefix;

        public string KnownElement
        {
            get { return "ChildPages"; }
        }

        public object GetExtra(XElement element, dynamic currentModel)
        {
            relativePathPrefix = element.Attribute("RelativePathPrefix").Value;

            var childPagesFolder = GetChildPagesFolder(element);

            var childPagesOutputFolder = element.Value.ToLowerInvariant();
            var outputFolder = Path.Combine(configuration.OutputRootFolder, childPagesOutputFolder);

            Directory.CreateDirectory(outputFolder);

            var files = Directory.GetFiles(childPagesFolder, "*.md");

            var posts = files.Select(file => CreatePage(file, childPagesOutputFolder)).ToList();

            CreateChildPageIndex(posts, currentModel, outputFolder, Path.Combine(configuration.TemplateRootFolder, element.Attribute("IndexLayoutFile").Value));

            CreateTagPages(currentModel, outputFolder, childPagesOutputFolder, Path.Combine(configuration.TemplateRootFolder, element.Attribute("TagLayoutFile").Value), Path.Combine(configuration.TemplateRootFolder, element.Attribute("TagsIndexLayoutFile").Value));

            GeneratePages(posts, currentModel, outputFolder );

            return posts.OrderBy(x => x.Time).ToList();
        }

        private void CreateTagPages(dynamic currentModel, string outputFolder, string childPagesOutputFolder,  string tagLayoutFile, string tagsIndexLayoutFile)
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
                                                           }) .ToList());

            var tagsIndexPageContent = generator.GenerateOutput(currentModel, tagIndexLayout);

            File.WriteAllText(Path.Combine(tagsRoot, "index.html"), tagsIndexPageContent);

            modelDictionary.Remove("Tags");

        }

        private void CreateChildPageIndex(List<Page> pages, dynamic currentModel, string outputFolder, string layoutFile)
        {
            var modelDictionary = (IDictionary<string, object>)currentModel;

            if (modelDictionary.ContainsKey("Pages"))
                modelDictionary.Remove("Pages");

            modelDictionary.Add("Pages", pages);

            var indexFileLocation = Path.Combine(outputFolder, "index.html");

            var layoutContent = File.ReadAllText(layoutFile);

            var staticPage = generator.GenerateOutput(currentModel, layoutContent);

            File.WriteAllText(indexFileLocation, staticPage);

            modelDictionary.Remove("Pages");
        }

        private string GetChildPagesFolder(XElement element)
        {
            return Path.Combine(configuration.TemplateRootFolder, element.Attribute("Location") == null ? "pages" : element.Attribute("Location").Value);
        }

        private Dictionary<string, Tag> allTags = new Dictionary<string, Tag>();

        private Page CreatePage(string file, string childPagesRootFolder)
        {
            var fileContent = File.ReadAllText(file);
            var postContent = Regex.Split(fileContent, "---");
            var metadata = postContent[1];
            var content = postContent[2];

            var title = metadata.GetTagsValue("title");
            var permalink = metadata.GetTagsValue("permalink");
            var description = metadata.GetTagsValue("description");
            var tags = metadata.GetTagsValue("tags").Split(',').Select(tag => tag.Trim()).ToList();
            var layout = metadata.GetTagsValue("layout");
            var layoutFile = Path.Combine(configuration.TemplateRootFolder, layout);
            var time = DateTime.Parse(metadata.GetTagsValue("time"), CultureInfo.InvariantCulture);

            var post = new Page
                           {
                               Description = description,
                               Location = Path.Combine(@"\", relativePathPrefix, childPagesRootFolder, permalink).Replace(@"\", @"/"),
                               Title = title,
                               Time = time,
                               TagNames =  tags,
                               Slurg = permalink,
                               LayoutFile = layoutFile,
                           };

            var options = new MarkdownOptions
            {
                AutoHyperlink = true,
                AutoNewlines = true,
                EmptyElementSuffix = "/>",
                EncodeProblemUrlCharacters = false,
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
            foreach (Page page in pages)
            {
                page.Tags = new List<Tag>();
                foreach (var tagName in page.TagNames)
                {
                    page.Tags.Add(allTags[tagName]);
                }

                GeneratePage(page, currentModel, outputFolder);
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
    }
}
