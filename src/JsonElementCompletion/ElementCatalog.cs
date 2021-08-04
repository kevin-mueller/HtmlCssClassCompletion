using AsyncCompletionSample;
using CSSParser;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlCssClassCompletion.JsonElementCompletion
{
    internal class ElementCatalog
    {
        private static ElementCatalog _instance;
        internal static ElementCatalog GetInstance() => _instance ??= new ElementCatalog();

        public List<CssClass> Classes { get; set; } = new List<CssClass>();

        public void RefreshClasses(Projects projects, RefreshCssClassesCommand context)
        {
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

                context.SetStatusMessage($"Finished caching of css classes. Found {Classes.Count} classes in {totalFiles} files.");
            });
        }

        private List<CssClass> GetCssClasses(string filePath)
        {
            var res = new List<CssClass>();
            var selectors = Parser.ParseCSS(File.ReadAllText(filePath))
                .Where(x => x.CharacterCategorisation == CSSParser.ContentProcessors.CharacterCategorisationOptions.SelectorOrStyleProperty);

            foreach (var item in selectors)
            {
                string valueCleaned;
                if (item.Value.StartsWith("."))
                {
                    valueCleaned = item.Value.TrimPrefix(".");

                    valueCleaned = valueCleaned.Split(':')[0];
                    valueCleaned = valueCleaned.Split('>')[0];
                    valueCleaned = valueCleaned.Split(',')[0];
                    valueCleaned = valueCleaned.Split('+')[0];
                    valueCleaned = valueCleaned.Split('~')[0];
                    valueCleaned = valueCleaned.Split('*')[0];
                    valueCleaned = valueCleaned.Split('[')[0];
                    valueCleaned = valueCleaned.Split('.')[0];

                    valueCleaned = valueCleaned.Trim();

                    res.Add(new CssClass(valueCleaned, filePath.Split(new[] { "\\" }, StringSplitOptions.None).LastOrDefault()));
                }
            }

            return res;
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
