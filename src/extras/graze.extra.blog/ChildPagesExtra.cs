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

        public string KnownElement
        {
            get { return "ChildPages"; }
        }

        public object GetExtra(XElement element, dynamic currentModel)
        {
            var childPagesFolder = GetChildPagesFolder(element);

            var childPagesOutputFolder = element.Value.ToLowerInvariant();
            var outputFolder = Path.Combine(configuration.OutputRootFolder, childPagesOutputFolder);

            var files = Directory.GetFiles(childPagesFolder, "*.md");

            var posts = files.Select(file => GeneratePost(file, outputFolder, childPagesOutputFolder, currentModel)).ToList();

            CreateChildPageIndex(posts, currentModel, outputFolder, Path.Combine(configuration.TemplateRootFolder, element.Attribute("IndexLayoutFile").Value));

            CreateTagPages(currentModel, outputFolder, Path.Combine(configuration.TemplateRootFolder, element.Attribute("TagLayoutFile").Value), Path.Combine(configuration.TemplateRootFolder, element.Attribute("TagsIndexLayoutFile").Value));

            return posts;
        }

        private void CreateTagPages(dynamic currentModel, string outputFolder, string tagLayoutFile, string tagsIndexLayoutFile)
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
                                                               tag.Location = Path.Combine(tag.Name,
                                                                                           "index.html");
                                                               return tag;
                                                           }) .ToList());

            var tagsIndexPageContent = generator.GenerateOutput(currentModel, tagIndexLayout);

            File.WriteAllText(Path.Combine(tagsRoot, "index.html"), tagsIndexPageContent);

            modelDictionary.Remove("Tags");

        }

        private void CreateChildPageIndex(List<object> posts, dynamic currentModel, string outputFolder, string layoutFile)
        {
            //var originalPageLocations = posts.Select(p => ((Post)p).Location).ToList();
            //foreach (Post post in posts)
            //{
            //    post.Location = Path.Combine(post.Slurg, "index.html");
            //}

            var modelDictionary = (IDictionary<string, object>)currentModel;

            if (modelDictionary.ContainsKey("Pages"))
                modelDictionary.Remove("Pages");

            modelDictionary.Add("Pages", posts);

            var indexFileLocation = Path.Combine(outputFolder, "index.html");

            var layoutContent = File.ReadAllText(layoutFile);

            var staticPage = generator.GenerateOutput(currentModel, layoutContent);

            File.WriteAllText(indexFileLocation, staticPage);

            modelDictionary.Remove("Pages");

            //foreach (Post post in posts)
            //{
            //    post.Location = originalPageLocations[0];
            //    originalPageLocations.RemoveAt(0);
            //}
        }


        private string GetChildPagesFolder(XElement element)
        {
            return Path.Combine(configuration.TemplateRootFolder, element.Attribute("Location") == null ? "posts" : element.Attribute("Location").Value);
        }

        private Dictionary<string, Tag> allTags = new Dictionary<string, Tag>();

        private Post GeneratePost(string file, string outputFolder, string childPagesRootFolder, dynamic currentModel)
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

            var postFolder = Path.Combine(outputFolder, permalink);
            Directory.CreateDirectory(postFolder);

            var post = new Post
                           {
                               Description = description,
                               Location = Path.Combine(childPagesRootFolder, permalink, "index.html"),
                               Tags = tags,
                               Title = title,
                               Time = time,
                               Slurg = permalink
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

            var modelDictionary = (IDictionary<string, object>)currentModel;
            if (modelDictionary.ContainsKey("Post"))
                modelDictionary.Remove("Post");

            modelDictionary.Add("Post", post);

            var layoutContent = File.ReadAllText(layoutFile);
            var staticPage = generator.GenerateOutput(currentModel, layoutContent);

            var postFileLocation = Path.Combine(outputFolder, permalink, "index.html");

            File.WriteAllText(postFileLocation, staticPage);

            modelDictionary.Remove("Post");

            foreach (var tag in post.Tags)
            {
                if (allTags.ContainsKey(tag))
                {
                    allTags[tag].Pages.Add(post);
                    continue;
                }

                var value = new Tag { Name = tag, Pages = new List<Post> { post } };
                allTags.Add(tag, value);
            }

            return post;
        }
    }

    public class Tag
    {
        public string Name { get; set; }
        public List<Post> Pages { get; set; }
        public int Count { get { return Pages.Count; } }
        public string Location { get; set; }
    }
}
