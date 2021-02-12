using System;
using HtmlAgilityPack;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;

namespace Scrapr
{
    class Program
    {
        static async Task Main(string[] args)
        {            
            Console.WriteLine(await AzureServicesScrapr.Run("https://azure.microsoft.com/en-us/services/"));
        }
    }

    static class AzureServicesScrapr
    {
        class AzureServiceDTO
        {
            public string Category{get;set;}
            public string ProductName { get; set; }
            public string URL{get;set;}
            public string Description{get;set;}
        }

        public static async Task<string> Run(string url)
        {
            var client = new HttpClient();
            // Get html from url
            var html = await GetWeb(url);

            // parse through content and extract what we need
            var azureServiceDTOs = ParseHTML(html);

            var UnevaluatedInProd = new List<string>();
            UnevaluatedInProd.Add("/en-us/services/active-directory/");            
            UnevaluatedInProd.Add("/en-us/services/data-factory/");
            UnevaluatedInProd.Add("/en-us/services/functions/");
            UnevaluatedInProd.Add("/en-us/services/disks/");
            UnevaluatedInProd.Add("/en-us/services/virtual-machines/linux-and-open/");
            UnevaluatedInProd.Add("/en-us/services/virtual-machines/");
            UnevaluatedInProd.Add("/en-us/services/azure-sql/");
            UnevaluatedInProd.Add("/en-us/services/service-bus/");
            UnevaluatedInProd.Add("/en-us/services/storage/data-lake-analytics/");
            UnevaluatedInProd.Add("/en-us/services/storage/data-lake-storage/");
            UnevaluatedInProd.Add("/en-us/services/cognitive-services/");
            UnevaluatedInProd.Add("/en-us/services/notification-hubs/");
            UnevaluatedInProd.Add("/en-us/services/key-vault/");
            UnevaluatedInProd.Add("/en-us/services/virtual-machines/data-science-virtual-machines/");
            UnevaluatedInProd.Add("/en-us/services/devops/");
            UnevaluatedInProd.Add("/en-us/services/cdn/");            

            var EvaluatedNonProd = new List<string>();
            EvaluatedNonProd.AddRange(UnevaluatedInProd);
            EvaluatedNonProd.Add("/en-us/services/cache/");


            var defaultTemplate = @"|-
|{0}
|[https://azure.microsoft.com{1} {2}]
|{3}
| class='col-grey-light-bg' |Not yet evaluated (NYE)
| 
| class='col-grey-light-bg' |Not in use
|
";

            var evaluatedPlannedTemplate = @"|-
|{0}
|[https://azure.microsoft.com{1} {2}]
|{3}
| class='col-blue-dark-bg' |Pending
| Not yet approved for general use.
| class='col-purple-bg' |Experimentation
|2021-01-19
";

            var unevaluatedProdTemplate = @"|-
|{0}
|[https://azure.microsoft.com{1} {2}]
|{3}
| class='col-grey-light-bg' |Not yet evaluated (NYE)
| 
| class='col-green-bg' |Production
|2021-01-19
";

            var uniques = new List<string>();
            // format and return result
            var sb = new StringBuilder();
            foreach(var dto in azureServiceDTOs)
            {
                if (uniques.Contains(dto.URL)) continue;  // filter out duplicates that are in multiple categories.

                uniques.Add(dto.URL);

                if (UnevaluatedInProd.Contains(dto.URL))
                {
                    sb.Append(string.Format(unevaluatedProdTemplate, dto.Category, dto.URL, dto.ProductName, dto.Description).Replace("'", "\""));
                } else if(EvaluatedNonProd.Contains(dto.URL))
                {
                    sb.Append(string.Format(evaluatedPlannedTemplate, dto.Category, dto.URL, dto.ProductName, dto.Description).Replace("'", "\""));
                }
                else
                {
                    sb.Append(string.Format(defaultTemplate, dto.Category, dto.URL, dto.ProductName, dto.Description).Replace("'", "\""));
                }
            }

            return sb.ToString();
        }

        static async Task<string> GetWeb(string url)
        {
            using(var client = new HttpClient())
            {
                return await client.GetStringAsync(url);
            }
        }

        static List<AzureServiceDTO> ParseHTML(string html)
        {
            List<AzureServiceDTO> products = new List<AzureServiceDTO>();

            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            var productList = htmlDoc.DocumentNode.Descendants("div")
                .Where(node => node.GetAttributeValue("id", "") == "products-list")
                .ToList();

            var lastCategory = "";

            foreach (var product in productList)
            {
                foreach(var row in product.Descendants("div"))
                {
                    // Category row
                    if(row.GetAttributeValue("class", "").Contains("column"))
                    {
                        var category = row.Descendants("h2")
                            .Where(d => d.GetAttributeValue("class", "").Equals("product-category")).FirstOrDefault();
                        if(category != null)
                        {
                            lastCategory = category.InnerText;
                        }
                    }

                    // Product row
                    if (row.GetAttributeValue("class", "").Equals("column medium-6 end"))
                    {
                        var dto = new AzureServiceDTO();
                        dto.Category = lastCategory;

                        dto.ProductName = row.Descendants("span").FirstOrDefault().InnerText;
                        dto.URL = row.Descendants("a").FirstOrDefault().GetAttributeValue("href","");                                              
                        dto.Description = row.Descendants("p").FirstOrDefault().InnerText;
                        products.Add(dto);
                    }
                }
            }

            return products;
        }
    }
}
