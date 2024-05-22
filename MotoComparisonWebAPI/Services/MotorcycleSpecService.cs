using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net;
using MotoComparisonWebAPI.Context;
using System.Xml;

namespace MotoComparisonWebAPI.Services
{
    

    public class MotorcycleSpecService
    {
        private readonly MotorcycleContext _context;
        private string baseUrl = "https://www.motorcyclespecs.co.za/";
        private string bikes = "bikes/";

        public MotorcycleSpecService(MotorcycleContext context)
        {
            _context = context;
        }

        public async Task FetchAndStoreData()
        {
            var manufacturers = await ScrapeManufacturers();
            foreach (var manufacturer in manufacturers)
            {
                var manufacturerEntity = _context.Manufacturers.FirstOrDefault(m => m.Url == manufacturer.Value);
                if (manufacturerEntity == null)
                {
                    manufacturerEntity = new Manufacturer
                    {
                        Name = manufacturer.Key,
                        Url = manufacturer.Value
                    };
                    _context.Manufacturers.Add(manufacturerEntity);
                }               
            }

            await _context.SaveChangesAsync();


            foreach (var manufacturer in manufacturers)
            {
                var models = await ScrapeModels(manufacturer.Value);
                foreach (var model in models)
                {
                    var modelEntity = _context.Models.FirstOrDefault(m => m.Url == model.Value);


                    if (modelEntity == null)
                    {
                        var manufacturerEntity = _context.Manufacturers.FirstOrDefault(m => m.Url == manufacturer.Value);


                        if(manufacturerEntity == null)
                        {
                            continue;
                        }

                        modelEntity = new Model
                        {
                            Name = model.Key,
                            Url = model.Value,
                            Manufacturer = manufacturerEntity
                        };
                        _context.Models.Add(modelEntity);
                    }

                    var specs = await ScrapeMotorcycleSpecs(model.Value);
                    foreach (var spec in specs)
                    {
                        if (!_context.Specifications.Any(s => s.Key == spec.Key && s.ModelId == modelEntity.Id))
                        {
                            _context.Specifications.Add(new Specification
                            {
                                Key = spec.Key,
                                Value = spec.Value,
                                Model = modelEntity
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                }
            }
                

        }

        public async Task<List<KeyValuePair<string, string>>> ScrapeManufacturers()
        {
            var manufacturers = new List<KeyValuePair<string, string>>();

            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetStringAsync(baseUrl + "Manufacturer.htm");
                var doc = new HtmlDocument();
                doc.LoadHtml(response);

                var manufacturerNodes = doc.DocumentNode.SelectNodes("//a[contains(@href, 'bikes/')]");
                if (manufacturerNodes != null)
                {
                    manufacturers.AddRange(manufacturerNodes.Select(node =>
                        new KeyValuePair<string, string>(node.InnerText.Trim(), node.GetAttributeValue("href", string.Empty))));
                }
            }

            return manufacturers;
        }

        public async Task<List<KeyValuePair<string, string>>> ScrapeModels(string manufacturerUrl)
        {
            var models = new List<KeyValuePair<string, string>>();
            var nextPageUrl = baseUrl + manufacturerUrl;
           

            using (HttpClient client = new HttpClient())
            {
                while (!string.IsNullOrEmpty(nextPageUrl))
                {
                    string response = "";

                    try
                    {
                        response = await client.GetStringAsync(nextPageUrl);

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("\nException Caught!");
                        Console.WriteLine("Message :{0} ", e.Message);
                    }

                    if(string.IsNullOrEmpty(response))
                    {
                        continue;
                    }

                    var doc = new HtmlDocument();
                    doc.LoadHtml(response);

                    // Get all model nodes
                    var modelNodes = doc.DocumentNode.SelectNodes("//a[contains(@href, 'model/')]");
                    if (modelNodes != null)
                    {
                        models.AddRange(modelNodes.Select(node =>
                            new KeyValuePair<string, string>(node.InnerText.Trim(), node.GetAttributeValue("href", string.Empty))));
                    }

                    // Find the next page link
                    var nextLinkNode = doc.DocumentNode.SelectSingleNode("//a[contains(text(), 'Next')]");
                    if (nextLinkNode != null)
                    {
                        nextPageUrl = baseUrl + bikes + nextLinkNode.GetAttributeValue("href", string.Empty);
                    }
                    else
                    {
                        nextPageUrl = null;
                    }
                }
            }

            return models;
        }

        public async Task<Dictionary<string, string>> ScrapeMotorcycleSpecs(string url)
        {
            var specs = new Dictionary<string, string>();

            using (HttpClient client = new HttpClient())
            {
                string response = "";

                try
                {
                    response = await client.GetStringAsync(baseUrl + url);

                }catch(Exception e)
                {
                    Console.WriteLine("\nException Caught!");
                    Console.WriteLine("Message :{0} ", e.Message);
                }

                if(string.IsNullOrEmpty(response))
                {
                    return specs;
                }

                // Remove all <center> tags while preserving their content
                string cleanedHtml = Regex.Replace(response, "</?center.*?>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);

                var doc = new HtmlDocument();
                doc.LoadHtml(cleanedHtml);

                List<HtmlNode> specTables = new List<HtmlNode>();

                for (int i = 1; i < 100; i++)
                {
                    string tableId = $"table{i}";
                    HtmlNode specTable = doc.DocumentNode.SelectSingleNode($"//table[@id='{tableId}']");
                    if (specTable != null)
                    {
                        specTables.Add(specTable);
                    }
                }

                if (specTables != null)
                {
                    foreach(var specTable in specTables)
                    {
                        foreach (var row in specTable.SelectNodes(".//tr"))
                        {
                            var cells = row.SelectNodes(".//td");
                            if (cells != null && cells.Count == 2)
                            {
                                var key = CleanText(cells[0].InnerText.Trim());
                                var value = CleanText(cells[1].InnerText.Trim());
                                specs[key] = value;
                            }
                        }
                    }                    
                }
            }

            return specs;
        }

        public async Task FetchAndStoreDataByManufacturer(string manufacturerStr)
        {
            var manufacturerEntity = _context.Manufacturers.FirstOrDefault(m => m.Name == manufacturerStr);

            if (manufacturerEntity == null)
            {
                return;
            }


            var models = await ScrapeModels(manufacturerEntity.Url);
            foreach (var model in models)
            {
                var modelEntity = _context.Models.FirstOrDefault(m => m.Url == model.Value);


                if (modelEntity == null)
                {

                    modelEntity = new Model
                    {
                        Name = model.Key,
                        Url = model.Value,
                        Manufacturer = manufacturerEntity
                    };
                    _context.Models.Add(modelEntity);
                }

                var specs = await ScrapeMotorcycleSpecs(model.Value);
                foreach (var spec in specs)
                {
                    if (!_context.Specifications.Any(s => s.Key == spec.Key && s.ModelId == modelEntity.Id))
                    {
                        _context.Specifications.Add(new Specification
                        {
                            Key = spec.Key,
                            Value = spec.Value,
                            Model = modelEntity
                        });
                    }
                }

                await _context.SaveChangesAsync();
            }
        }

        private string CleanText(string input)
        {
            // Decode HTML entities
            string decoded = WebUtility.HtmlDecode(input);

            // Remove � characters
            decoded = Regex.Replace(decoded, "[�]", string.Empty);

            // Remove any other unwanted characters
            decoded = Regex.Replace(decoded, "&nbsp;", " ");

            return decoded;
        }
    }

}
