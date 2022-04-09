using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlCssClassCompletion22
{
    [Command("f992657c-635a-4b3e-9d97-6e083ffaf4e7", 0x0100)]
    public class RefreshCssClassesCommand : BaseCommand<RefreshCssClassesCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ElementCatalog.GetInstance().RefreshClassesAsync();
        }
    }
}
