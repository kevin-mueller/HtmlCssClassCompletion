using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Core.Imaging;
using System.Windows.Media;

namespace HtmlCssClassCompletion22
{
    internal class CompletionSource : ICompletionSource
    {
        private CompletionSourceProvider m_sourceProvider;
        private ITextBuffer m_textBuffer;
        private List<Completion> m_compList;

        static ImageElement DefaultIcon = new ImageElement(new ImageId(new Guid("ae27a6b0-e345-4288-96df-5eaf394ee369"), 1747), "CssClass");

        public CompletionSource(CompletionSourceProvider sourceProvider, ITextBuffer textBuffer)
        {
            m_sourceProvider = sourceProvider;
            m_textBuffer = textBuffer;
        }

        void ICompletionSource.AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            var strList = ElementCatalog.GetInstance().Classes;

            m_compList = new List<Completion>();
            foreach (var str in strList)
                m_compList.Add(MakeItemFromElement(str));

            completionSets.Add(new CompletionSet(
                "Tokens",    //the non-localized title of the tab
                "Tokens",    //the display title of the tab
                FindTokenSpanAtPosition(session.GetTriggerPoint(m_textBuffer),
                    session),
                m_compList,
                null));
        }

        private Completion MakeItemFromElement(ElementCatalog.CssClass element)
        {
            var item = new Completion(
                displayText: element.Name,
                insertionText: element.Name,
                description: element.Name,
                iconSource: null, "");

            // Each completion item we build has a reference to the element in the property bag.
            // We use this information when we construct the tooltip.
            item.Properties.AddProperty(nameof(ElementCatalog.CssClass), element);

            return item;
        }

        private ITrackingSpan FindTokenSpanAtPosition(ITrackingPoint point, ICompletionSession session)
        {
            SnapshotPoint currentPoint = (session.TextView.Caret.Position.BufferPosition) - 1;
            ITextStructureNavigator navigator = m_sourceProvider.NavigatorService.GetTextStructureNavigator(m_textBuffer);
            TextExtent extent = navigator.GetExtentOfWord(currentPoint);
            return currentPoint.Snapshot.CreateTrackingSpan(extent.Span, SpanTrackingMode.EdgeInclusive);
        }

        private bool m_isDisposed;
        public void Dispose()
        {
            if (!m_isDisposed)
            {
                GC.SuppressFinalize(this);
                m_isDisposed = true;
            }
        }
    }
}
