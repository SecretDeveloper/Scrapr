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

            // format and return result
            var sb = new StringBuilder();
            foreach(var dto in azureServiceDTOs)
            {
                sb.Append(string.Format(@"|-
|{0}
|[https://azure.microsoft.com{1} {2}]
|{3}
| class='col-grey-light-bg' |Not yet evaluated (NYE)
| Contact EA for evaluation and approval before using.
| class='col-grey-light-bg' |Not in use
|2021-01-15
", dto.Category, dto.URL, dto.ProductName, dto.Description).Replace("'", "\""));
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
