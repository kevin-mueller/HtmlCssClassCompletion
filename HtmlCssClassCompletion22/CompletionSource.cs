using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace HtmlCssClassCompletion22
{
    internal class CompletionSource : ICompletionSource
    {
        private CompletionSourceProvider m_sourceProvider;
        private ITextBuffer m_textBuffer;
        private List<Completion> m_compList;

        public CompletionSource(CompletionSourceProvider sourceProvider, ITextBuffer textBuffer)
        {
            m_sourceProvider = sourceProvider;
            m_textBuffer = textBuffer;
        }

        void ICompletionSource.AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            var strList = new List<string>
            {
                "addition",
                "adaptation",
                "subtraction",
                "summation"
            };
            m_compList = new List<Completion>();
            foreach (string str in strList)
                m_compList.Add(new Completion(str, str, str, null, null));

            completionSets.Add(new CompletionSet(
                "Tokens",    //the non-localized title of the tab
                "Tokens",    //the display title of the tab
                FindTokenSpanAtPosition(session.GetTriggerPoint(m_textBuffer),
                    session),
                m_compList,
                null));
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
