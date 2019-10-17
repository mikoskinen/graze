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
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using graze.common;
using graze.contracts;
using Markdig;

namespace graze.extra.childpages
{
    [DelayedExecutionAttribute]
    public class ChildPagesExtra : IExtra
    {
        private readonly Dictionary<string, Tag> _allTags = new Dictionary<string, Tag>();

        [Import(typeof(IFolderConfiguration))] private IFolderConfiguration _configuration;

        private string _defaultLayoutFile;

        [Import(typeof(IGenerator))] private IGenerator _generator;

        private string _readmeName;
        private string _relativePathPrefix;
        private bool _shouldGenerateFolderForEachPost;
        private bool _shouldGenerateRss;
        private bool _isRootFolder;

        public string KnownElement
        {
            get
            {
                return "ChildPages";
            }
        }

        public object GetExtra(XElement element, dynamic currentModel)
        {
            _relativePathPrefix = GetRelativePathPrefix(element);
            _defaultLayoutFile = GetDefaultLayoutFile(element);
            _shouldGenerateRss = GetShouldGenerateRss(element);
            _shouldGenerateFolderForEachPost = GetShouldGenerateFolderForEachPost(element);
            _readmeName = GetReadmeName(element);
            _isRootFolder = GetIsRootFolder(element);
            
            var childPagesFolder = GetChildPagesFolder(element);

            var childPagesOutputFolder = element.LastNode != null && element.LastNode.NodeType == XmlNodeType.Text
                ? element.LastNode.ToString().Trim().ToLowerInvariant()
                : "";
            var outputFolder = Path.Combine(_configuration.OutputRootFolder, childPagesOutputFolder);
            var groupDefinitions = ParseGroupDefinitions(element);

            Directory.CreateDirectory(outputFolder);

            var files = Directory.GetFiles(childPagesFolder, "*.md", SearchOption.AllDirectories);

            var posts = files.Select(file => CreatePage(file, childPagesOutputFolder, childPagesFolder)).ToList();

            var pageGroups = CreateGroups(posts, groupDefinitions);
            currentModel.PageGroups = pageGroups;

            var indexPageFileName = GetIndexPageFileName(element);

            if (!string.IsNullOrWhiteSpace(indexPageFileName))
            {
                CreateChildPageIndex(posts, currentModel, outputFolder,
                    Path.Combine(_configuration.TemplateRootFolder, element.Attribute("IndexLayoutFile").Value), indexPageFileName);
            }

            CreateTagPages(currentModel, outputFolder, childPagesOutputFolder,
                Path.Combine(_configuration.TemplateRootFolder, element.Attribute("TagLayoutFile").Value),
                Path.Combine(_configuration.TemplateRootFolder, element.Attribute("TagsIndexLayoutFile").Value));

            GeneratePages(posts, currentModel, _configuration.OutputRootFolder);

            CopyContent(childPagesFolder, outputFolder);
            CopyPostContent(posts, childPagesFolder, outputFolder);

            var result = new ChildPages
            {
                Name = childPagesOutputFolder, Pages = posts.OrderBy(x => x.Time).ToList(), Tags = _allTags.Select(x => x.Value).ToList()
            };

            if (_shouldGenerateRss)
            {
                GenerateRss(element, result, outputFolder);
            }

            return result;
        }

        private static string GetRelativePathPrefix(XElement element)
        {
            return element.Attribute("RelativePathPrefix").Value;
        }

        private string GetDefaultLayoutFile(XElement element)
        {
            if (element.Attribute("DefaultPageLayoutFile") == null)
            {
                return null;
            }

            return element.Attribute("DefaultPageLayoutFile").Value;
        }

        private bool GetShouldGenerateRss(XElement element)
        {
            if (element.Attribute("DefaultPageLayoutFile") == null)
            {
                return false;
            }

            return element.Attribute("RssGenerate").Value == "true";
        }

        private bool GetShouldGenerateFolderForEachPost(XElement element)
        {
            if (element.Attribute("FolderPerPage") == null)
            {
                return true;
            }

            return element.Attribute("FolderPerPage").Value == "true";
        }

        private string GetIndexPageFileName(XElement element)
        {
            if (element.Attribute("IndexFileName") == null)
            {
                return null;
            }

            return element.Attribute("IndexFileName").Value;
        }

        private string GetReadmeName(XElement element)
        {
            if (element.Attribute("ReadmeName") == null)
            {
                return null;
            }

            return element.Attribute("ReadmeName").Value;
        }
        
        private bool GetIsRootFolder(XElement element)
        {
            if (element.Attribute("IsRoot") == null)
            {
                return false;
            }

            return bool.Parse(element.Attribute("IsRoot").Value);
        }

        private string GetChildPagesFolder(XElement element)
        {
            return Path.Combine(_configuration.ConfigurationRootFolder, element.Attribute("Location") == null ? "pages" : element.Attribute("Location").Value);
        }

        private void CreateTagPages(dynamic currentModel, string outputFolder, string childPagesOutputFolder, string tagLayoutFile, string tagsIndexLayoutFile)
        {
            string tagsRoot;
            
            if (_isRootFolder)
            {
                tagsRoot = Path.Combine(outputFolder, "tags");
            }
            else
            {
                tagsRoot = Path.Combine(outputFolder, _relativePathPrefix, "tags");
            }
            
            Directory.CreateDirectory(tagsRoot);

            var modelDictionary = (IDictionary<string, object>) currentModel;

            foreach (var tagPair in _allTags)
            {
                var tag = tagPair.Value;
                var tagPath = Path.Combine(tagsRoot, tag.Name);

                Directory.CreateDirectory(tagPath);

                var layout = ReadLayout(tagLayoutFile);

                if (modelDictionary.ContainsKey("Tag"))
                {
                    modelDictionary.Remove("Tag");
                }

                modelDictionary.Add("Tag", tag);

                var tagPageContent = _generator.GenerateOutput(currentModel, layout);

                File.WriteAllText(Path.Combine(tagPath, "index.html"), tagPageContent);

                modelDictionary.Remove("Tag");
            }

            var tagIndexLayout = File.ReadAllText(tagsIndexLayoutFile);

            if (modelDictionary.ContainsKey("Tags"))
            {
                modelDictionary.Remove("Tags");
            }

            modelDictionary.Add("Tags", _allTags.Select(t =>
            {
                var tag = t.Value;
                tag.Location = Path.Combine(@"\", _relativePathPrefix, childPagesOutputFolder, "tags", tag.Name).Replace(@"\", @"/");

                return tag;
            }).ToList());

            var tagsIndexPageContent = _generator.GenerateOutput(currentModel, tagIndexLayout);

            File.WriteAllText(Path.Combine(tagsRoot, "index.html"), tagsIndexPageContent);

            modelDictionary.Remove("Tags");
        }

        private void CreateChildPageIndex(List<Page> pages, dynamic currentModel, string outputFolder, string layoutFile, string indexPageFileName)
        {
            var modelDictionary = (IDictionary<string, object>) currentModel;

            if (modelDictionary.ContainsKey("Pages"))
            {
                modelDictionary.Remove("Pages");
            }

            if (modelDictionary.ContainsKey("PagesDesc"))
            {
                modelDictionary.Remove("PagesDesc");
            }

            if (modelDictionary.ContainsKey("PagesAsc"))
            {
                modelDictionary.Remove("PagesAsc");
            }

            modelDictionary.Add("Pages", pages);
            modelDictionary.Add("PagesDesc", pages.OrderByDescending(x => x.Time).ToList());
            modelDictionary.Add("PagesAsc", pages.OrderBy(x => x.Time).ToList());
            
            string indexFileLocation;

            if (_isRootFolder)
            {
                indexFileLocation = Path.Combine(outputFolder, indexPageFileName);
            }
            else
            {
                indexFileLocation = Path.Combine(outputFolder, _relativePathPrefix, indexPageFileName);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(indexFileLocation));
            
            var layoutContent = ReadLayout(layoutFile);

            var staticPage = _generator.GenerateOutput(currentModel, layoutContent);

            File.WriteAllText(indexFileLocation, staticPage);

            modelDictionary.Remove("Pages");
            modelDictionary.Remove("PagesDesc");
            modelDictionary.Remove("PagesAsc");
        }

        private Page CreatePage(string file, string childPagesOutputRoot, string childPagesFolder)
        {
            var fileContent = File.ReadAllText(file);
            var meta = new Dictionary<string, string>();
            var content = fileContent;

            var postContent = Regex.Split(fileContent, "---");
            var containsMetaData = postContent.Count() > 1;

            if (containsMetaData)
            {
                var metadata = postContent[1];
                meta = metadata.GetMetaData();

                content = fileContent.Replace(metadata, "").TrimStart('-');
            }

            var title = meta.ContainsKey("title") ? meta["title"] : Path.GetFileNameWithoutExtension(file);
            var slurg = meta.ContainsKey("permalink") ? meta["permalink"] : Path.GetFileNameWithoutExtension(file);

            var description = meta.ContainsKey("description") ? meta["description"] : "";
            var tags = meta.ContainsKey("tags") ? meta["tags"].Split(',').Select(tag => tag.Trim()).ToList() : new List<string>();

            var layout = meta.ContainsKey("layout") ? meta["layout"] : _defaultLayoutFile;

            if (string.IsNullOrWhiteSpace(layout))
            {
                throw new ArgumentNullException("Layout", "No layout set either in the post or in the configuration.");
            }

            var layoutFile = Path.Combine(_configuration.TemplateRootFolder, layout);
            DateTime time;

            if (meta.ContainsKey("time"))
            {
                if (!DateTime.TryParse(meta["time"], CultureInfo.InvariantCulture, DateTimeStyles.None, out time))
                {
                    var parsingTimeFailedPage = $"Parsing time failed. Page {file}, Date {meta["time"]}";

                    throw new Exception(parsingTimeFailedPage);
                }
            }
            else
            {
                time = File.GetLastWriteTime(file);
            }

            var location = GetFileLocation(file, childPagesFolder, childPagesOutputRoot);

            var order = meta.ContainsKey("order") ? int.Parse(meta["order"]) : int.MaxValue;

            var post = new Page
            {
                Description = description,
                Location = location,
                Title = title,
                Time = time,
                TagNames = tags,
                Slurg = slurg,
                LayoutFile = layoutFile,
                ParentPage = Path.Combine("/", _relativePathPrefix, childPagesOutputRoot),
                Group = GetGroup(file, childPagesFolder, childPagesOutputRoot),
                Order = order,
                OriginalLocation = file,
                IncludeInNavigation = !meta.ContainsKey("in-navigation") || bool.Parse(meta["in-navigation"]),
                ContainsToc = !meta.ContainsKey("toc") || bool.Parse(meta["toc"])
            };

            var builder = new MarkdownPipelineBuilder()
                .UseAutoIdentifiers()
                .UseBootstrap()
                .UseEmojiAndSmiley()
                .UseYamlFrontMatter()
                .UseAdvancedExtensions()
                .UsePipeTables();

            var pipeline = builder.Build();

            post.Content = Markdown.ToHtml(content.Trim(), pipeline);

            foreach (var tag in post.TagNames)
            {
                if (_allTags.ContainsKey(tag))
                {
                    _allTags[tag].Pages.Add(post);

                    continue;
                }

                var value = new Tag { Name = tag, Pages = new List<Page> { post } };
                _allTags.Add(tag, value);
            }

            var parser = new HtmlParser();
            var document = parser.ParseDocument(post.Content);

            var elementsWithIds = document.All.Where(x => x.HasAttribute("id")).ToList();
            var toc = new List<Tuple<int, string, string>>();

            var currentLevel = 1;

            foreach (var element in elementsWithIds)
            {
                if (element is IHtmlHeadingElement header)
                {
                    currentLevel = int.Parse(new string(header.LocalName.Where(char.IsDigit).ToArray()));
                }
                else
                {
                    currentLevel += 1;
                }

                var tocItem = Tuple.Create(currentLevel, element.Id, element.TextContent);
                toc.Add(tocItem);
            }

            post.TableOfContents = toc;

            var images = document.Images.ToList();

            for (var i = 0; i < images.Count; i++)
            {
                var image = images[i];

                var wrapperLink = document.CreateElement("a");
                wrapperLink.SetAttribute("href", image.GetAttribute("src"));
                wrapperLink.ClassName = "lightbox";

                var imageParent = image.ParentElement;
                imageParent.ReplaceChild(wrapperLink, image);
                wrapperLink.AppendChild(image);
            }

            post.Content = document.Body.InnerHtml;

            return post;
        }

        private string GetFileLocation(string file, string childPagesFolder, string childPagesOutputRoot)
        {
            var routeRootUri = new Uri(childPagesFolder.EndsWith("\\") ? childPagesFolder : childPagesFolder + "\\");
            var routeUri = routeRootUri.MakeRelativeUri(new Uri(Path.ChangeExtension(file, null), UriKind.Absolute));

            var outputFileName = routeUri.ToString();

            if (!_shouldGenerateFolderForEachPost && outputFileName.EndsWith("readme", StringComparison.InvariantCultureIgnoreCase) &&
                !string.IsNullOrWhiteSpace(_readmeName))
            {
                outputFileName = outputFileName.Replace("readme", _readmeName);
            }

            var location = Path.Combine(@"\", _relativePathPrefix, childPagesOutputRoot, outputFileName).Replace(@"\", @"/");

            if (_shouldGenerateFolderForEachPost)
            {
                location = Path.Combine(location, "index.html");
            }
            else
            {
                location = location + ".html";
            }

            return location;
        }

        private string GetGroup(string file, string childPagesFolder, string childPagesOutputRoot)
        {
            var routeRootUri = new Uri(childPagesFolder.EndsWith("\\") ? childPagesFolder : childPagesFolder + "\\");
            var routeUri = routeRootUri.MakeRelativeUri(new Uri(Path.ChangeExtension(file, null), UriKind.Absolute)).ToString();

            var result = Path.GetDirectoryName(routeUri);

            return result;
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
                    page.Tags.Add(_allTags[tagName]);
                }

                page.PreviousPage = previousPage;
                page.NextPage = pages.Count > i + 1 ? pages[i + 1] : null;

                GeneratePage(page, currentModel, outputFolder);
                previousPage = page;
            }
        }

        private void GeneratePage(Page page, dynamic currentModel, string outputFolder)
        {
            var modelDictionary = (IDictionary<string, object>) currentModel;

            if (modelDictionary.ContainsKey("Page"))
            {
                modelDictionary.Remove("Page");
            }

            modelDictionary.Add("Page", page);

            var filePath = page.Location.Trim('/');

            if (!string.IsNullOrWhiteSpace(_relativePathPrefix) && _isRootFolder)
            {
                filePath = filePath.Replace(_relativePathPrefix + "/", "");
            }
            
            var postFolder = Path.Combine(outputFolder, Path.GetDirectoryName(filePath));
            Directory.CreateDirectory(postFolder);

            var layoutFile = page.LayoutFile;
            var layoutContent = ReadLayout(layoutFile);
            var staticPage = _generator.GenerateOutput(currentModel, layoutContent);

            var postFileName = Path.Combine(outputFolder, filePath);
            File.WriteAllText(postFileName, staticPage);

            page.OutputLocation = postFileName;

            modelDictionary.Remove("Page");
        }

        private string ReadLayout(string layoutFile)
        {
            var result = File.ReadAllText(layoutFile);

            if (string.IsNullOrWhiteSpace(_relativePathPrefix))
            {
                return result;
            }

            if (!string.IsNullOrWhiteSpace(_relativePathPrefix) && !_isRootFolder)
            {
                return result;
            }
            
            result = result.Replace("/assets", $"/{_relativePathPrefix}/assets");
            return result;
        }

        private void CopyContent(string childPagesFolder, string outputFolder)
        {
            var contentSourceFolder = Path.Combine(childPagesFolder, "content");

            if (!Directory.Exists(contentSourceFolder))
            {
                return;
            }

            var contentOutputFolder = Path.Combine(outputFolder, "content");

            DirCopy.Copy(contentSourceFolder, contentOutputFolder);
        }

        private void CopyPostContent(List<Page> posts, string childPagesFolder, string outputFolder)
        {
            var handledPostContentFolders = new HashSet<(string, string)>();

            foreach (var post in posts)
            {
                var postOriginalDirectory = Path.GetDirectoryName(post.OriginalLocation);
                var postTargetDirectory = Path.GetDirectoryName(post.OutputLocation);

                var key = (postOriginalDirectory, postTargetDirectory);

                if (handledPostContentFolders.Contains(key))
                {
                    continue;
                }

                var imageFiles = Directory.EnumerateFiles(postOriginalDirectory, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(s => s.EndsWith(".gif", StringComparison.InvariantCultureIgnoreCase) ||
                                s.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase) ||
                                s.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase)).ToList();

                foreach (var imageFile in imageFiles)
                {
                    var fileName = Path.GetFileName(imageFile);

                    File.Copy(imageFile, Path.Combine(postTargetDirectory, fileName), true);
                }

                handledPostContentFolders.Add(key);
            }
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

        private List<PageGroup> ParseGroupDefinitions(XElement element)
        {
            var result = new List<PageGroup>();

            var groupDefinitions = element.Descendants("Group").ToList();

            for (var i = 0; i < groupDefinitions.Count; i++)
            {
                var groupDefinition = groupDefinitions[i];
                var key = groupDefinition.Attribute("Key").Value;
                var name = groupDefinition.Value;
                var order = i;

                var item = new PageGroup();
                item.Key = key;
                item.Name = name;
                item.Order = order;

                if (groupDefinition.Attribute("ShowChildPagesIfEmptyOrOne") != null)
                {
                    item.ShowChildPagesIfEmptyOrOne = bool.Parse(groupDefinition.Attribute("ShowChildPagesIfEmptyOrOne").Value);
                }

                if (groupDefinition.Attribute("ShowTocIfOnePage") != null)
                {
                    item.ShowTocIfOnePage = bool.Parse(groupDefinition.Attribute("ShowTocIfOnePage").Value);
                }
                else
                {
                    item.ShowTocIfOnePage = true;
                }

                if (groupDefinition.Attribute("ReplaceGroupNameWithPageNameIfOnePage") != null)
                {
                    item.ReplaceGroupNameWithPageNameIfOnePage = bool.Parse(groupDefinition.Attribute("ReplaceGroupNameWithPageNameIfOnePage").Value);
                }
                else
                {
                    item.ReplaceGroupNameWithPageNameIfOnePage = true;
                }

                result.Add(item);
            }

            return result;
        }

        private List<PageGroup> CreateGroups(List<Page> pages, List<PageGroup> pageGroups)
        {
            var result = new List<PageGroup>(pageGroups);

            var groupedPosts = pages.GroupBy(x => x.Group).ToList();

            foreach (var postGroup in groupedPosts)
            {
                var existingGroup = result.FirstOrDefault(x => x.Key == postGroup.Key);

                if (existingGroup != null)
                {
                    existingGroup.Pages = new List<Page>();
                }
                else
                {
                    result.Add(new PageGroup
                    {
                        Key = postGroup.Key,
                        Order = int.MaxValue,
                        Name = postGroup.Key,
                        Pages = new List<Page>(),
                        ReplaceGroupNameWithPageNameIfOnePage = true,
                        ShowTocIfOnePage = true
                    });
                }
            }

            foreach (var pageGroup in result)
            {
                if (pageGroup.Pages == null)
                {
                    pageGroup.Pages = new List<Page>();
                }
            }

            foreach (var post in pages.Where(x => x.IncludeInNavigation))
            {
                var postGroup = result.FirstOrDefault(x => x.Key == post.Group);

                postGroup?.Pages.Add(post);
            }

            foreach (var postGroup in result)
            {
                if (postGroup.Pages?.Any() == true)
                {
                    postGroup.Pages = postGroup.Pages.OrderBy(x => x.Order).ThenBy(x => x.Title).ThenBy(x => x.Time).ToList();
                    postGroup.DefaultLocation = postGroup.Pages.First().Location;
                }
            }

            foreach (var page in pages)
            {
                var groupPages = result.FirstOrDefault(x => x.Key == page.Group)?.Pages ?? new List<Page>();
                page.PagesInGroup = groupPages.OrderBy(x => x.Order).ThenBy(x => x.Title).ThenBy(x => x.Time).ToList();
            }

            foreach (var pageGroup in result)
            {
                if (pageGroup.Pages.Count == 1)
                {
                    var page = pageGroup.Pages.First();
                    var tableOfContents = page.TableOfContents;

                    if (tableOfContents != null && tableOfContents.Any())
                    {
                        var pageList = new List<Page>();

                        foreach (var tableOfContent in page.TableOfContents)
                        {
                            pageList.Add(new Page { Location = page.Location + "#" + tableOfContent.Item2, Title = tableOfContent.Item3 });
                        }

                        page.TableOfContents = new List<Tuple<int, string, string>>();
                        pageGroup.Pages.Clear();
                        pageGroup.Pages.AddRange(pageList);
                    }

                    if (pageGroup.ReplaceGroupNameWithPageNameIfOnePage && string.IsNullOrWhiteSpace(pageGroup.Name))
                    {
                        pageGroup.Name = page.Title;
                    }
                    else if (!string.IsNullOrWhiteSpace(pageGroup.Name))
                    {
                        page.Title = pageGroup.Name;
                    }
                }
                else
                {
                    Page previousPageInGroup = null;

                    for (var i = 0; i < pageGroup.Pages.Count; i++)
                    {
                        var page = pageGroup.Pages[i];
                        page.PreviousPageInGroup = previousPageInGroup;
                        page.NextPageInGroup = pageGroup.Pages.Count > i + 1 ? pageGroup.Pages[i + 1] : null;

                        previousPageInGroup = page;
                    }
                }
            }

            result = result.OrderBy(x => x.Order).ThenBy(x => x.Name).ToList();

            return result;
        }
    }
}
