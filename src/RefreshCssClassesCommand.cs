using CSSParser;
using EnvDTE;
using HtmlCssClassCompletion.JsonElementCompletion;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static HtmlCssClassCompletion.JsonElementCompletion.ElementCatalog;
using Task = System.Threading.Tasks.Task;

namespace AsyncCompletionSample
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class RefreshCssClassesCommand
    {
        ElementCatalog Catalog = ElementCatalog.GetInstance();

        private static IVsStatusbar StatusBar;

        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("f992657c-635a-4b3e-9d97-6e083ffaf4e7");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="RefreshCssClassesCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private RefreshCssClassesCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static RefreshCssClassesCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in RefreshCssClassesCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new RefreshCssClassesCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            DTE dte = (DTE)ServiceProvider.GetServiceAsync(typeof(DTE)).Result;
            Projects projects = dte.Solution.Projects;

            SetStatusMessage("Caching Css Classes...");

            _ = Task.Run(async delegate
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
                        Catalog.Classes.AddRange(await GetCssClassesAsync(file.FullName));
                    }
                }

                Catalog.Classes = Catalog.Classes.OrderBy(x => x.Name).ToList();
                Catalog.Classes = Catalog.Classes.DistinctBy(x => x.Name).ToList();

                SetStatusMessage($"Finished caching of css classes. Found {Catalog.Classes.Count} classes in {totalFiles} files.");
            });
        }

        private async Task<List<CssClass>> GetCssClassesAsync(string filePath)
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
                    valueCleaned = valueCleaned.Split('.')[0];

                    valueCleaned = valueCleaned.Trim();

                    res.Add(new CssClass(valueCleaned, filePath));
                }
            }

            return res;
        }



        internal static void SetStatusMessage(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (StatusBar == null)
            {
                StatusBar = Package.GetGlobalService(typeof(IVsStatusbar)) as IVsStatusbar;
            }

            StatusBar.SetText(message);
        }
    }
}
