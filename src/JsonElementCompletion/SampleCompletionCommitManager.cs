using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace HtmlCssClassCompletion.JsonElementCompletion
{
    /// <summary>
    /// The simplest implementation of IAsyncCompletionCommitManager that provides Commit Characters and uses default behavior otherwise
    /// </summary>
    internal class SampleCompletionCommitManager : IAsyncCompletionCommitManager
    {
        public SampleCompletionCommitManager()
        {
        }

        ImmutableArray<char> commitChars = new char[] { ' ', '"', '>', '/' }.ToImmutableArray();

        public IEnumerable<char> PotentialCommitCharacters => commitChars;

        public bool ShouldCommitCompletion(IAsyncCompletionSession session, SnapshotPoint location, char typedChar, CancellationToken token)
        {
            return true;
        }

        public CommitResult TryCommit(IAsyncCompletionSession session, ITextBuffer buffer, CompletionItem item, char typedChar, CancellationToken token)
        {
            // Objects of interest here are session.TextView and session.TextView.Caret.
            // This method runs synchronously

            return CommitResult.Unhandled; // use default commit mechanism.
        }
    }
}