using EnvDTE;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace HtmlCssClassCompletion.JsonElementCompletion
{
    class SampleCompletionSource : IAsyncCompletionSource
    {
        private ElementCatalog Catalog { get; }
        private ITextStructureNavigatorSelectorService StructureNavigatorSelector { get; }
        
        private DTE DTE;
        private string currentFileName;

        // ImageElements may be shared by CompletionFilters and CompletionItems. The automationName parameter should be localized.
        static ImageElement DefaultIcon = new ImageElement(new ImageId(new Guid("ae27a6b0-e345-4288-96df-5eaf394ee369"), 1747), "CssClass");

        public SampleCompletionSource(ElementCatalog catalog, ITextStructureNavigatorSelectorService structureNavigatorSelector)
        {
            Catalog = catalog;
            StructureNavigatorSelector = structureNavigatorSelector;
            
            ThreadHelper.ThrowIfNotOnUIThread();
            DTE ??= Package.GetGlobalService(typeof(DTE)) as DTE;
            currentFileName = DTE.ActiveDocument.Name;
        }

        public CompletionStartData InitializeCompletion(CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken token)
        {
            // We don't trigger completion when user typed
            if (char.IsNumber(trigger.Character)         // a number
                || char.IsPunctuation(trigger.Character) // punctuation
                || trigger.Character == '\n'             // new line
                || trigger.Reason == CompletionTriggerReason.Backspace
                || trigger.Reason == CompletionTriggerReason.Deletion)
            {
                return CompletionStartData.DoesNotParticipateInCompletion;
            }


            //check if we are in the class= context
            var lineStart = triggerLocation.GetContainingLine().Start;
            var spanBeforeCaret = new SnapshotSpan(lineStart, triggerLocation);
            var textBeforeCaret = triggerLocation.Snapshot.GetText(spanBeforeCaret);

            if (textBeforeCaret.IndexOf("class=", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var items = Regex.Split(textBeforeCaret, "class=", RegexOptions.IgnoreCase);
                if (items[1].Count(x => (x == '"')) == 1)
                {
                    var tokenSpan = FindTokenSpanAtPosition(triggerLocation);
                    return new CompletionStartData(CompletionParticipation.ProvidesItems, tokenSpan);
                }
            }

            return CompletionStartData.DoesNotParticipateInCompletion;
        }

        private SnapshotSpan FindTokenSpanAtPosition(SnapshotPoint triggerLocation)
        {
            // This method is not really related to completion,
            // we mostly work with the default implementation of ITextStructureNavigator 
            // You will likely use the parser of your language
            ITextStructureNavigator navigator = StructureNavigatorSelector.GetTextStructureNavigator(triggerLocation.Snapshot.TextBuffer);
            TextExtent extent = navigator.GetExtentOfWord(triggerLocation);
            if (triggerLocation.Position > 0 && (!extent.IsSignificant || !extent.Span.GetText().Any(c => char.IsLetterOrDigit(c))))
            {
                // Improves span detection over the default ITextStructureNavigation result
                extent = navigator.GetExtentOfWord(triggerLocation - 1);
            }

            var tokenSpan = triggerLocation.Snapshot.CreateTrackingSpan(extent.Span, SpanTrackingMode.EdgeInclusive);

            var snapshot = triggerLocation.Snapshot;
            var tokenText = tokenSpan.GetText(snapshot);
            if (string.IsNullOrWhiteSpace(tokenText))
            {
                // The token at this location is empty. Return an empty span, which will grow as user types.
                return new SnapshotSpan(triggerLocation, 0);
            }

            // Trim quotes and new line characters.
            int startOffset = 0;
            int endOffset = 0;

            if (tokenText.Length > 0)
            {
                if (tokenText.StartsWith("\""))
                    startOffset = 1;
            }
            if (tokenText.Length - startOffset > 0)
            {
                if (tokenText.EndsWith("\"\r\n"))
                    endOffset = 3;
                else if (tokenText.EndsWith("\r\n"))
                    endOffset = 2;
                else if (tokenText.EndsWith("\"\n"))
                    endOffset = 2;
                else if (tokenText.EndsWith("\n"))
                    endOffset = 1;
                else if (tokenText.EndsWith("\""))
                    endOffset = 1;
            }

            return new SnapshotSpan(tokenSpan.GetStartPoint(snapshot) + startOffset, tokenSpan.GetEndPoint(snapshot) - endOffset);
        }

        public async Task<CompletionContext> GetCompletionContextAsync(IAsyncCompletionSession session, CompletionTrigger trigger, SnapshotPoint triggerLocation, SnapshotSpan applicableToSpan, CancellationToken token)
        {
            var filteredList = new List<CompletionItem>();
            foreach (var element in Catalog.Classes)
            {
                //isolated css context
                if (element.FileNames.All(x => x.EndsWith(".razor.css")))
                {
                    if (!element.FileNames.Any(x => x.Replace(".css", "") == currentFileName))
                    {
                        continue;
                    }
                }

                filteredList.Add(MakeItemFromElement(element));
            }

            return await System.Threading.Tasks.Task.FromResult(new CompletionContext(filteredList.ToImmutableArray()));
        }

        /// <summary>
        /// Builds a <see cref="CompletionItem"/> based on <see cref="ElementCatalog.CssClass"/>
        /// </summary>
        private CompletionItem MakeItemFromElement(ElementCatalog.CssClass element)
        {
            var item = new CompletionItem(
                displayText: element.Name,
                source: this,
                icon: DefaultIcon);

            // Each completion item we build has a reference to the element in the property bag.
            // We use this information when we construct the tooltip.
            item.Properties.AddProperty(nameof(ElementCatalog.CssClass), element);

            return item;
        }

        /// <summary>
        /// Provides detailed element information in the tooltip
        /// </summary>
        public async Task<object> GetDescriptionAsync(IAsyncCompletionSession session, CompletionItem item, CancellationToken token)
        {
            if (item.Properties.TryGetProperty<ElementCatalog.CssClass>(nameof(ElementCatalog.CssClass), out var matchingElement))
            {
                return await System.Threading.Tasks.Task.FromResult($"{matchingElement.Name} ({string.Join(", ", matchingElement.FileNames)})");
            }
            return null;
        }
    }
}
