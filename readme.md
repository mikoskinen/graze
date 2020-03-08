# Graze

Graze is a static site generator. It takes a template and a configuration file and generates a static web site. The generated site is pure HTML / CSS / JavaScript and can be hosted on any web server. The Graze templates are created using the "Razor Syntax": http://haacked.com/archive/2011/01/06/razor-syntax-quick-reference.aspx.

### Breaking changes for new versions of Graze (moving from .NET Framework to .NET Core and .NET Standard)

Latest version of Graze has been updated from .NET Framework to .NET Core 3.1 and .NET Standard 2.0. This currently causes two breaking changes, one for layouts and one for raw HTML:

1. Reference to layout file using Layout, instead of _Layout like before.
2. For now, the HTML is encoded by default and the only way to get around this is set DisableEncoding = true; in your cshtml-file.

Here's an example index.cshtml where both of these changes are done:

```
@{
    Layout = "./Layout.cshtml";
    ViewBag.Title = Model.Description;
    DisableEncoding = true;
}

<h1>@Model.Description</h1>
...
```

## Getting started

1. Download the latest release (zip contains a sample): https://github.com/mikoskinen/graze/releases/tag/7.0.0
2. Run graze.exe

(Note: You may have to "Unblock" the DLL-files in extras-folder)

The static site (index.html) is generated into the output folder.

## NuGet

Graze is also available from the NuGet. To get started, first create a new "Empty Project" with Visual Studio and then add the Graze using NuGet. Graze.exe and the template-folder are installed into the root of your project.

```
Install-Package graze
```

### Graze templates

The Graze templates are created using the Razor syntax. Here's an example:

```
<html>
<head>
    <title>@Model.Title</title>
</head>
<body>
    <h1>@Model.Description</h1>
</body>
</html>
```

### Graze configuration

The configuration for Graze is done in XML. Here's an example configuration:

```
<?xml version="1.0" encoding="utf-8" ?>
<data>
	<site>
		<Title>Graze</Title>
		<Description>Graze: Static site generator using Razor</Description>
	</site>
</data>
```

The configuration file represents the data which is injected to the generated static site.

### Generating the static site

Once the Graze template and the configuration file are in place, the static site can be generated running the graze.exe. The static site is generated into the output-folder. 

```
<html>
<head>
    <title>Graze</title>
</head>
<body>
    <h1>Graze: Static site generator using Razor</h1>
</body>
</html>
```

## Features

### Dynamic configuration

The configuration.xml represents the data which is shown on the generated site. In the template you can access a configuration parameter using the @Model-keyword. New configuration options can be added freely and old ones can be removed.

### Lists and loops

Lists can be created in XML and accessed in the Graze template. Example XML:

```
  <Features>
    <Feature>Layouts defined using Razor syntax.</Feature>
    <Feature>Dynamic data models created in XML.</Feature>
    <Feature>Supports complex data models and arrays.</Feature>
    <Feature>Fast static site generation.</Feature>
    <Feature>Pure HTML / CSS / Javascript output. Host in Apache, IIS etc.</Feature>
  </Features>
```

Example template for accessing the list:

```
    <h2>Features:</h2>
    <ul>
        @foreach (var feature in Model.Features)
        {
            <li>@feature</li>
        }
    </ul>
```

### Ifs

The programming language behind the Graze template is C# so if and if-else statements are available. Given the following configuration:

```
  <Family>
	<Member Age="50">Saul</Member>
	<Member Age="31">Claire</Member>
	<Member Age="11">John</Member>
  </Family>
```

We can output different HTML from our template based on the member's age

```
	@foreach(var member in @Model.Family)
	{
		var membersAge = int.Parse(member.Age);
		
		if (membersAge >= 18)
		{
			<p>@member.Member is an adult</p>
		}
		else
		{
			<p>@member.Member is a child</p>
		}
	}
```

Generated output:

```
			<p>Saul is an adult</p>
			<p>Claire is an adult</p>
			<p>John is a child</p>
```


### Complex types

By default all the data in the XML is of type string when accessed from the template. But complex types can be created also:

```
<Link Url="https://github.com/mikoskinen/graze">Source code hosted in GitHub</Link>
```

```
<a href="@Model.Link.Url">@Model.Link.Link</a>
```

### Case sensitive

The configuration and the templates are case sensitive. 

## Extras

The Graze's core can create the site's model from the configuration file's site-element. But, the model can be enhanced using extras. Graze comes bundled with the following extras:

* Markdown
* HTML
* RSS
* YouTube

The example template bundled with the Graze shows how to use these model enhancers.

## Folder structure

Graze expects the following folder structure:

```
graze.exe
--template/
----configuration.xml
----index.cshtml
----assets
```

The assets folder is copied wholly to the output folder. The assets folder can include the CSS / JSS / image files required by the template.

!https://github.com/mikoskinen/graze/raw/master/doc/drawing_small.png!

## License

<pre>
<code>
MIT License
Copyright (C) 2019 Mikael Koskinen

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
of the Software, and to permit persons to whom the Software is furnished to do
so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
</code>
</pre>

### Other libraries

Graze uses the following libraries: 

* RazorLight: https://github.com/toddams/RazorLight
* MarkDig: https://github.com/lunet-io/markdig
* AngleSharp: https://github.com/AngleSharp/AngleSharp