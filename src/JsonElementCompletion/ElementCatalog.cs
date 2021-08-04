using EnvDTE;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlCssClassCompletion.JsonElementCompletion
{
    [Export]
    internal class ElementCatalog
    {
        [Import]
        DTEClass DTE;

        public List<CssClass> Classes { get; } = new List<CssClass>()
        {
            new CssClass("test-class", "testfile1.css")
        };

        internal ElementCatalog()
        {
            RefreshClasses();
        }

        public void RefreshClasses()
        {
            

            foreach (var item in DTE.Solution.Projects)
            {
                var projectPath = ((Project)item).FullName;
            }
            //TODO:
            //search the entire project structure (with referenced projects!) for .css files and parse the classes from it.
            //also search for <link> attributes from .html / .cshtml files and parse their classes as well.
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
