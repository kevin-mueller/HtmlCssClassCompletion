using EnvDTE;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlCssClassCompletion.JsonElementCompletion
{
    internal class ElementCatalog
    {
        private static ElementCatalog _instance;
        internal static ElementCatalog GetInstance() => _instance ??= new ElementCatalog();

        public List<CssClass> Classes { get; set; } = new List<CssClass>();

        public void RefreshClasses()
        {
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
