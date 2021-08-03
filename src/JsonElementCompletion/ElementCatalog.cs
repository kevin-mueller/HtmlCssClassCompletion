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
        public List<Element> Elements { get; } = new List<Element>()
        {
            new Element("test-class", "testfile1.css")
        };

        public class Element
        {
            public string Name { get; }
            public string FileName { get; }

            internal Element(string name, string fileName)
            {
                Name = name;
                FileName = fileName;
            }
        }
    }
}
