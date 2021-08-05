using AsyncCompletionSample;
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

namespace HtmlCssClassCompletion.JsonElementCompletion
{
    internal class ElementCatalog
    {
        private static ElementCatalog _instance;
        internal static ElementCatalog GetInstance() => _instance ??= new ElementCatalog();

        public List<CssClass> Classes { get; set; } = new List<CssClass>();

        public void RefreshClasses(Projects projects, IVsStatusbar statusBar)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            statusBar.SetText("Caching Css Classes...");

            _ = System.Threading.Tasks.Task.Run(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var totalFiles = 0;
                var cssContentFailedToDownload = new List<Uri>();

                foreach (var item in projects)
                {
                    var folderPath = new FileInfo(((Project)item).FileName).DirectoryName;
                    var files = new DirectoryInfo(folderPath).GetFiles("*.css", SearchOption.AllDirectories);

                    var htmlFiles = new DirectoryInfo(folderPath).GetFiles("*.*html", SearchOption.AllDirectories);
                    totalFiles += files.Length + htmlFiles.Length;

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
                    statusBar.SetText($"Finished caching of css classes. Found {Classes.Count} classes in {totalFiles} files. " +
                        $"{cssContentFailedToDownload.Count} external CSS File(s) failed to download.");
                }
                else
                {
                    statusBar.SetText($"Finished caching of css classes. Found {Classes.Count} classes in {totalFiles} files.");
                }
            });
        }

        private List<Uri> GetCdnUrlsFromHtmlFiles(FileInfo[] htmlFiles)
        {
            var cdnUrls = new List<Uri>();
            //TODO implement parsing of html files.
            //parse all link attributes, check if they contain the rel="stylesheet" attribute and add their href content to the list.

            return cdnUrls;
        }

        private List<CssClass> GetCssClasses(string cssContent, string filePath)
        {
            var res = new List<CssClass>();
            var selectors = Parser.ParseCSS(cssContent)
                .Where(x => x.CharacterCategorisation == CSSParser.ContentProcessors.CharacterCategorisationOptions.SelectorOrStyleProperty);

            var fileName = filePath.Split(new[] { "\\" }, StringSplitOptions.None).LastOrDefault();

            //TODO parse linked css files as well.

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
