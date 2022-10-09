using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Threading;
using MoreLinq;
using NuGet.VisualStudio;
using NuGet.VisualStudio.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Markup;
using VSLangProj;
using Task = System.Threading.Tasks.Task;

namespace HtmlCssClassCompletion22
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(HtmlCssClassCompletion22Package.PackageGuidString)]
    [ProvideOptionPage(typeof(OptionPage), "Better Razor CSS Class Intellisense", "General", 0, 0, true)]
    public sealed class HtmlCssClassCompletion22Package : AsyncPackage
    {
        /// <summary>
        /// HtmlCssClassCompletion22Package GUI5D string.
        /// </summary>
        public const string PackageGuidString = "70e49e7d-bc32-4db3-be4c-9b13b3bad2f0";

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            ElementCatalog.GetInstance(this);

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            await this.RegisterCommandsAsync();

            VS.Events.SolutionEvents.OnAfterBackgroundSolutionLoadComplete += OnAfterBackgroundSolutionLoadComplete;
            VS.Events.DocumentEvents.Saved += OnAfterDocumentSaved;
            VS.Events.ProjectItemsEvents.AfterAddProjectItems += OnAfterAddProjectItems;
        }

        [SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "Event Handler")]
        public async void OnAfterDocumentSaved(string documentPath)
        {
            if (!((OptionPage)GetDialogPage(typeof(OptionPage))).ScanOnSave)
                return;

            try
            {
                if (documentPath.EndsWith("css") || documentPath.EndsWith("html"))
                    await ElementCatalog.GetInstance().RefreshClassesAsync();
            }
            catch (Exception ex)
            {
                await VS.StatusBar.ShowMessageAsync($"Failed to scan CSS classes. {ex?.Message}");
            }
        }

        [SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "Event Handler")]
        private async void OnAfterBackgroundSolutionLoadComplete()
        {
            //yes I know, simply awaiting a delay is not the prettiest ways of making sure all references are present, but it's the only
            //one I've found. There is no other event, that occures at a later stage, so this will have to suffice.
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
                await ElementCatalog.GetInstance().RefreshClassesAsync();
            }
            catch (Exception ex)
            {
                //I think this method might fail when loading a really big project.
                //the problem is, that this event fires before all projects are actually loaded.
                //it fires as soon as the solution is "opened".
                //I think this is because VS started to implement the loading of projects as async.
                await VS.StatusBar.ShowMessageAsync($"Failed to scan CSS classes. {ex?.Message}");
            }
        }

        [SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "Event Handler")]
        private async void OnAfterAddProjectItems(IEnumerable<SolutionItem> obj)
        {
            try
            {
                if (obj.Any(x => x.Name.EndsWith(".css")))
                    await ElementCatalog.GetInstance().RefreshClassesAsync();
            }
            catch (Exception ex)
            {
                await VS.StatusBar.ShowMessageAsync($"Failed to scan CSS classes. {ex?.Message}");
            }
        }
        #endregion        
    }

    [ComVisible(true)]
    public class OptionPage : DialogPage
    {
        private bool scanOnSave = true;

        [Category("Better Razor CSS Class Intellisense")]
        [DisplayName("Scan on save")]
        [Description("Automatically re-scan when saving a css or html file.")]
        public bool ScanOnSave
        {
            get { return scanOnSave; }
            set { scanOnSave = value; }
        }

        private bool useCdnCache = false;

        [Category("Better Razor CSS Class Intellisense")]
        [DisplayName("Use Cdn Cache")]
        [Description("Cache the content of linked external css files. This reduces the time for scanning, especially if your internet connection is slow, but increases RAM usage.")]
        public bool UseCdnCache
        {
            get { return useCdnCache; }
            set { useCdnCache = value; }
        }
    }
}