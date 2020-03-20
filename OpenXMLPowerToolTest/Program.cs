using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenXMLPowerTools;

namespace OpenXMLPowerToolTest
{
    class Program
    {
        static void Main(string[] args)
        {
            List<Product> Products = new List<Product>();

            Products.Add(new Product()
            {
                ProductName = "Linked Pack 28L",
                ProductDesc = "<p>Our largest climbing pack for long days on the wall. Built with the burliest fabrics we could find to withstand abuse from short hauls and getting stuffed in chimneys.</p><br><h5>Style No. 48035</h5>",
                ProductFtrs = "<ol style='font-weight:bold'><li>Hard-Working Durable Fabric</li><li>Easy-Access Design</li><li>Top Compression Strap</li><li>Burly Haul Loops</li></ol>"
            });

            Products.Add(new Product()
            {
                ProductName = "The Day Heel",
                ProductDesc = "<p>A heel you can walk in. All. Damn. Day. The Day Heel’s ballet-inspired silhouette is designed with a rounded toe, a walkable 2-inch block heel, and an elasticized back for extra comfort.</p>",
                ProductFtrs = "<ol><li>Narrower fit <ul><li>Sizes 5–6.5 run large</li><li>Sizes 7–8.5 run true to size</li><li>Sizes 9–11 run small</li></ul></li><li>100% Italian leather<br/>Treat with protectant and spot clean with a cloth.</li><li>Made in Montopoli in Val D’Arno, Italy</li></ol>"
            });

            int i = 1;
            DocumentAssembler documentAssembler = new DocumentAssembler();
            var TemplatePath = $"{ConfigurationManager.AppSettings["TemplatePath"]}{ConfigurationManager.AppSettings["TemplateName"]}";

            foreach (var p in Products)
            {
                var Data = p.ToXElement<Product>();
                var FileBytes = documentAssembler.GenerateDocument(TemplatePath, Data);
                File.WriteAllBytes($"{ConfigurationManager.AppSettings["TemplatePath"]}Product_{i}.docx",FileBytes);
                FileBytes = null;
                i++;
            }
        }
    }
}
