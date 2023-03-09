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
using System.ComponentModel;
using Microsoft.VisualStudio.TaskStatusCenter;
using Microsoft.VisualStudio.PlatformUI;

namespace HtmlCssClassCompletion22
{
    internal class ElementCatalog
    {
        private readonly HtmlCssClassCompletion22Package package;

        private static ElementCatalog _instance;
        private HttpClient _httpClient;

        public Dictionary<Uri, string> CdnCache = new();

        private ElementCatalog(HtmlCssClassCompletion22Package package)
        {
            this.package = package;
            _httpClient = new HttpClient();
        }

        internal static ElementCatalog GetInstance(HtmlCssClassCompletion22Package package = null) => _instance ??= new ElementCatalog(package);

        public List<CssClass> Classes { get; set; } = new List<CssClass>();

        public async Task RefreshClassesAsync()
        {
            IVsTaskStatusCenterService tsc = await VS.Services.GetTaskStatusCenterAsync();

            var options = default(TaskHandlerOptions);
            options.Title = "Caching CSS Classes";
            options.ActionsAfterCompletion = CompletionActions.None;
            options.TaskSuccessMessage = "CSS Caching Finished.";

            TaskProgressData data = default;
            data.CanBeCanceled = true;

            var handler = tsc.PreRegister(options, data);

            var task = BackgroundTaskAsync(data, handler, await GetProjectPathsWithReferencesAsync());
            handler.RegisterTask(task);
        }

        private async Task<Dictionary<string, List<DirectoryInfo>>> GetProjectPathsWithReferencesAsync()
        {
            Dictionary<string, List<DirectoryInfo>> projectPathsWithReferences = new();
            await ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
            {
                //do any operations on the DTE object here (where we're still on the main thread)
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                DTE dte = await VS.GetServiceAsync<DTE, DTE>();
                var projects = dte.Solution.Projects;

                if (projects?.Count > 0)
                {
                    var projectPaths = projects?.Flatten()?.Select(x =>
                    {
                        ThreadHelper.ThrowIfNotOnUIThread();
                        return ((EnvDTE.Project)x)?.FullName;
                    })?.Where(x => !string.IsNullOrEmpty(x))?.ToList();

                    foreach (var project in projects)
                    {
                        try
                        {
                            var vsproject = ((EnvDTE.Project)project).Object as VSLangProj.VSProject;

                            var webPackages = vsproject.References.Flatten().Select(x =>
                            {
                                try
                                {
                                    return new FileInfo(((VSLangProj.Reference)x).Path).Directory.Parent.Parent.GetDirectories("staticwebassets").FirstOrDefault();
                                }
                                catch
                                {
                                    return null;
                                }
                            }).Where(x => x != null);

                            projectPathsWithReferences.Add(((EnvDTE.Project)project).FullName, webPackages.ToList());
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
            });
            return projectPathsWithReferences;
        }

        private async Task BackgroundTaskAsync(TaskProgressData data, ITaskHandler handler, Dictionary<string, List<DirectoryInfo>> projectPaths)
        {
            Classes.Clear();

            var totalFiles = 0;
            var cssContentFailedToDownload = new List<Uri>();

            int currentStep = 0;
            int totalSteps = projectPaths.Keys.Count;
            foreach (var project in projectPaths)
            {
                data.PercentComplete = currentStep / totalSteps * 100;
                data.ProgressText = $"Caching project {currentStep} of {totalSteps}";
                handler.Progress.Report(data);

                string folderPath;
                try
                {
                    folderPath = new FileInfo(project.Key).DirectoryName;
                }
                catch
                {
                    //might occure if the path is invald/contains invalid characters. in that case, skip this project.
                    continue;
                }

                var di = new DirectoryInfo(folderPath);
                var excludedPaths = new string[]
                {
                    $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"
                };

                //get all .css files directly from the project folder
                var files = di.GetFiles("*.css", SearchOption.AllDirectories).Where(x => !excludedPaths.Any(y => x.FullName.Contains(y))).ToList();

                //get all html files directly from the project folder (to extract cdn css files from it)
                var htmlFiles = di.GetFiles("*.*html", SearchOption.AllDirectories).Where(x => !excludedPaths.Any(y => x.FullName.Contains(y)));

                //also search package references of the project, in order to get the css files from nuget packages

                var packageFiles = new List<FileInfo>();

                foreach (var webPackage in project.Value)
                {
                    packageFiles.AddRange(webPackage.GetFiles("*.css", SearchOption.AllDirectories));
                }
                files.AddRange(packageFiles);


                var cssFileUrls = GetCdnUrlsFromHtmlFiles(htmlFiles);

                totalFiles += files.Count + cssFileUrls.Count;

                foreach (var file in files)
                {
                    try
                    {
                        Classes.AddRange(GetCssClasses(File.ReadAllText(file.FullName), file.FullName));
                    }
                    catch
                    {
                        //skip if unable to parse.
                        continue;
                    }
                }

                foreach (var fileUrl in cssFileUrls)
                {
                    await Task.Run(async delegate
                    {
                        try
                        {
                            Classes.AddRange(GetCssClasses(await GetCdnContentAsync(fileUrl), fileUrl.AbsoluteUri));
                        }
                        catch (HttpRequestException)
                        {
                            cssContentFailedToDownload.Add(fileUrl);
                        }
                    });
                }
            }

            Classes = Classes.OrderBy(x => x.Name).ToList();
            Classes = Classes.DistinctBy(x => x.Name).ToList();

            currentStep++;
            data.PercentComplete = currentStep / totalSteps * 100;

            if (cssContentFailedToDownload.Any())
            {
                data.ProgressText = $"Finished caching of css classes. Found {Classes.Count} classes in {totalFiles} files. " +
                    $"{cssContentFailedToDownload.Count} external CSS File(s) failed to download.";
            }
            else
            {
                data.ProgressText = $"Finished caching of CSS classes. Found {Classes.Count} classes in {totalFiles} files.";
            }

            await VS.StatusBar.ShowMessageAsync(data.ProgressText);

            handler.Progress.Report(data);
        }

        private async Task<string> GetCdnContentAsync(Uri url)
        {
            if (((OptionPage)package.GetDialogPage(typeof(OptionPage))).UseCdnCache)
            {
                if (CdnCache.ContainsKey(url))
                    return CdnCache.FirstOrDefault(x => x.Key == url).Value;
            }

            var res = await _httpClient.GetStringAsync(url);
            CdnCache.Add(url, res);
            return res;
        }

        private List<Uri> GetCdnUrlsFromHtmlFiles(IEnumerable<FileInfo> htmlFiles)
        {
            var cdnUrls = new List<string>();

            foreach (var htmlFilePath in htmlFiles)
            {
                try
                {
                    var doc = new HtmlDocument();
                    doc.Load(htmlFilePath.FullName);

                    var linkNodes = doc.DocumentNode.SelectNodes("//link[@rel='stylesheet']");
                    if (linkNodes == null)
                        continue;

                    cdnUrls.AddRange(linkNodes.Select(x => x.GetAttributeValue("href", string.Empty))
                        .Where(x => x.StartsWith("https://") || x.StartsWith("http://")));
                }
                catch
                {
                    //continue with next html file
                    continue;
                }
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
