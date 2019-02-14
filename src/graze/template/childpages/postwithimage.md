---
title: Test page with image and no layout
permalink: versions-20-22
description: Test page
time: 2012-07-16 09:37
tags: graze
---
Page content can be copied into the content-folder. This folder is automatically copied to the output folder when the pages are generated.

![versions2022](content/Adafy_Hori.png)

Default page layout can be set using a configuration attribute DefaultPageLayoutFile, for example:

    <ChildPages Location="childpages" DefaultPageLayoutFile="post.cshtml" IndexLayoutFile="pagesindex.cshtml" TagsIndexLayoutFile="tagsindex.cshtml" TagLayoutFile="tag.cshtml" RelativePathPrefix="">ChildPages</ChildPages>