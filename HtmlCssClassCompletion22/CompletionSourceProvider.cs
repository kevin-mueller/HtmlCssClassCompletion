using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlCssClassCompletion22
{
    [Export(typeof(IAsyncCompletionSourceProvider))]
    [ContentType("razor")]
    [Name("token completion")]
    class SampleCompletionSourceProvider : IAsyncCompletionSourceProvider
    {
        readonly IDictionary<ITextView, IAsyncCompletionSource> cache = new Dictionary<ITextView, IAsyncCompletionSource>();

        ElementCatalog Cataolog = ElementCatalog.GetInstance();

        [Import]
        ITextStructureNavigatorSelectorService StructureNavigatorSelector;

        public IAsyncCompletionSource GetOrCreate(ITextView textView)
        {
            if (cache.TryGetValue(textView, out var itemSource))
                return itemSource;

            var source = new CompletionSource(Cataolog, StructureNavigatorSelector); // opportunity to pass in MEF parts
            textView.Closed += (o, e) => cache.Remove(textView); // clean up memory as files are closed
            cache.Add(textView, source);
            return source;
        }
    }
}
