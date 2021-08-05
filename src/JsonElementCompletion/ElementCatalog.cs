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

                foreach (var item in projects)
                {
                    var folderPath = new FileInfo(((Project)item).FileName).DirectoryName;
                    var files = new DirectoryInfo(folderPath).GetFiles("*.css", SearchOption.AllDirectories);
                    totalFiles += files.Length;

                    foreach (var file in files)
                    {
                        Classes.AddRange(GetCssClasses(file.FullName));
                    }
                }

                Classes = Classes.OrderBy(x => x.Name).ToList();
                Classes = Classes.DistinctBy(x => x.Name).ToList();

                statusBar.SetText($"Finished caching of css classes. Found {Classes.Count} classes in {totalFiles} files.");
            });
        }

        private List<CssClass> GetCssClasses(string filePath)
        {
            var res = new List<CssClass>();
            var selectors = Parser.ParseCSS(File.ReadAllText(filePath))
                .Where(x => x.CharacterCategorisation == CSSParser.ContentProcessors.CharacterCategorisationOptions.SelectorOrStyleProperty);

            //TODO parse linked css files as well.
            //TODO execute css class caching on projects loaded event.


            foreach (var item in selectors)
            {
                if (item.Value.StartsWith("."))
                {
                    var tokens = item.Value.TrimPrefix(".").Split('.');
                    foreach (var token in tokens)
                    {
                        res.Add(new CssClass(cleanValue(token), filePath.Split(new[] { "\\" }, StringSplitOptions.None).LastOrDefault()));
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
            public string FileName { get; }

            internal CssClass(string name, string fileName)
            {
                Name = name;
                FileName = fileName;
            }
        }
    }
}
