using CSSParser;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using MoreLinq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using HtmlAgilityPack;
using Community.VisualStudio.Toolkit;

namespace HtmlCssClassCompletion22
{
    internal class ElementCatalog
    {
        private static ElementCatalog _instance;
        internal static ElementCatalog GetInstance() => _instance ??= new ElementCatalog();

        public List<CssClass> Classes { get; set; } = new List<CssClass>();

        public async Task RefreshClassesAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            await VS.StatusBar.ShowProgressAsync("Caching Css Classes...", 1, 2);

            DTE dte = await VS.GetServiceAsync<DTE, DTE>();
            var projects = dte.Solution.Projects;


            var test1 = await  VS.Solutions.GetAllProjectHierarchiesAsync();

            var totalFiles = 0;
            var cssContentFailedToDownload = new List<Uri>();

            foreach (var project in projects)
            {
                
                var folderPath = new FileInfo(((EnvDTE.Project)project).FullName).DirectoryName;

                //get all .css files directly from the project folder
                var files = new DirectoryInfo(folderPath).GetFiles("*.css", SearchOption.AllDirectories).ToList();

                //get all html files directly from the project folder (to extract cdn css files from it)
                var htmlFiles = new DirectoryInfo(folderPath).GetFiles("*.*html", SearchOption.AllDirectories);

                //also search package references of the project, in order to get the css files from nuget packages

                //var vsproject = ((Project)item).Object as VSLangProj.VSProject;
                var packageFiles = new List<FileInfo>();

                var vsproject = ((EnvDTE.Project)project).Object as VSLangProj.VSProject;

                var webPackages = vsproject.References.Flatten().Select(x =>
                    new FileInfo(((VSLangProj.Reference)x).Path).Directory.Parent.Parent.GetDirectories("staticwebassets").FirstOrDefault()).Where(x => x != null);

                foreach (var webPackage in webPackages)
                {
                    packageFiles.AddRange(webPackage.GetFiles("*.css", SearchOption.AllDirectories));
                }
                packageFiles.Sort();
                packageFiles = packageFiles.Distinct().ToList();
                files.AddRange(packageFiles);

                totalFiles += files.Count + htmlFiles.Length;

                var cssFileUrls = GetCdnUrlsFromHtmlFiles(htmlFiles);

                foreach (var file in files)
                {
                    Classes.AddRange(GetCssClasses(File.ReadAllText(file.FullName), file.FullName));
                }

                foreach (var fileUrl in cssFileUrls)
                {
                    try
                    {
                        Classes.AddRange(GetCssClasses(await new HttpClient().GetStringAsync(fileUrl), fileUrl.AbsoluteUri));
                    }
                    catch (HttpRequestException)
                    {
                        cssContentFailedToDownload.Add(fileUrl);
                    }
                }
            }

            Classes = Classes.OrderBy(x => x.Name).ToList();
            Classes = Classes.DistinctBy(x => x.Name).ToList();

            if (cssContentFailedToDownload.Any())
            {
                await VS.StatusBar.ShowProgressAsync($"Finished caching of css classes. Found {Classes.Count} classes in {totalFiles} files. " +
                    $"{cssContentFailedToDownload.Count} external CSS File(s) failed to download.", 2, 2);
            }
            else
            {
                await VS.StatusBar.ShowProgressAsync($"Finished caching of css classes. Found {Classes.Count} classes in {totalFiles} files.", 2, 2);
            }
        }

        private List<Uri> GetCdnUrlsFromHtmlFiles(FileInfo[] htmlFiles)
        {
            var cdnUrls = new List<string>();

            foreach (var htmlFilePath in htmlFiles)
            {
                var doc = new HtmlDocument();
                doc.Load(htmlFilePath.FullName);

                var linkNodes = doc.DocumentNode.SelectNodes("//link[@rel='stylesheet']");
                if (linkNodes == null)
                    continue;

                cdnUrls.AddRange(linkNodes.Select(x => x.GetAttributeValue("href", string.Empty))
                    .Where(x => x.StartsWith("https://") || x.StartsWith("http://")));
            }

            return cdnUrls.Where(x => x != string.Empty).Select(y => new Uri(y)).ToList();
        }

        private List<CssClass> GetCssClasses(string cssContent, string filePath)
        {
            var res = new List<CssClass>();
            var selectors = Parser.ParseCSS(cssContent)
                .Where(x => x.CharacterCategorisation == CSSParser.ContentProcessors.CharacterCategorisationOptions.SelectorOrStyleProperty);

            var fileName = filePath.Split(new[] { "\\" }, StringSplitOptions.None).LastOrDefault();

            foreach (var item in selectors)
            {
                if (item.Value.StartsWith("."))
                {
                    var tokens = item.Value.TrimPrefix(".").Split('.');
                    foreach (var token in tokens)
                    {
                        var finalNameValue = cleanValue(token);

                        var existing = Classes.FirstOrDefault(x => x.Name == finalNameValue);
                        if (existing == null)
                        {
                            res.Add(new CssClass(finalNameValue, new List<string> { fileName }));
                        }
                        else
                        {
                            if (!existing.FileNames.Contains(fileName))
                                existing.FileNames.Add(fileName);
                        }
                    }
                }
            }

            return res;

            static string cleanValue(string value)
            {
                var valueCleaned = value;
                valueCleaned = valueCleaned.Split(':')[0];
                valueCleaned = valueCleaned.Split('>')[0];
                valueCleaned = valueCleaned.Split(',')[0];
                valueCleaned = valueCleaned.Split('+')[0];
                valueCleaned = valueCleaned.Split('~')[0];
                valueCleaned = valueCleaned.Split('*')[0];
                valueCleaned = valueCleaned.Split('[')[0];
                valueCleaned = valueCleaned.Split(')')[0];
                //valueCleaned = valueCleaned.Split('.')[0];

                return valueCleaned.Trim();
            }
        }

        public class CssClass
        {
            public string Name { get; }
            public List<string> FileNames { get; } = new List<string>();

            internal CssClass(string name, List<string> fileNames)
            {
                Name = name;
                FileNames = fileNames;
            }
        }
    }
}
