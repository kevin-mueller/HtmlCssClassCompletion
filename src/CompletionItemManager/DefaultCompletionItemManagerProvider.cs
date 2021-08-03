using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.PatternMatching;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;

namespace HtmlCssClassCompletion.CompletionItemManager
{
    [Export(typeof(IAsyncCompletionItemManagerProvider))]
    [Name("Better Html Css Class Intellisense")]
    [ContentType("HTMLXProjection")]
    [Order(Before = PredefinedCompletionNames.DefaultCompletionItemManager)] // override the default item manager so that we can step through this code
    internal sealed class DefaultCompletionItemManagerProvider : IAsyncCompletionItemManagerProvider
    {
        [Import]
        public IPatternMatcherFactory PatternMatcherFactory;

        DefaultCompletionItemManager _instance;

        IAsyncCompletionItemManager IAsyncCompletionItemManagerProvider.GetOrCreate(ITextView textView)
        {
            return _instance ??= new DefaultCompletionItemManager(PatternMatcherFactory);
        }
    }
}
