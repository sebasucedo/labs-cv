using DotLiquid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace io.ucedo.labs.cv;

public class HtmlGenerator
{
    const string DATA_URL = "https://cdn.ucedo.io/cv/raw.json";

    public static async Task<string> GenerateHtml()
    {
        var html = await GetBasic();

        return html;
    }

    private static async Task<string> GetBasic()
    {
        var name = await GetName();
        var html = await GetHtmlFromTemplate(name);

        return html;
    }

    private static async Task<string> GetName()
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(DATA_URL);
        var json = await response.Content.ReadAsStringAsync();

        using (JsonDocument document = JsonDocument.Parse(json))
        {
            JsonElement root = document.RootElement;
            if (root.TryGetProperty("name", out JsonElement nameElement))
            {
                string nombre = nameElement.GetString() ?? string.Empty;
                return nombre;
            }
        }

        return string.Empty;
    }

    private static async Task<string> GetHtmlFromTemplate(string name)
    {
        string templateContent = await File.ReadAllTextAsync("./views/index.liquid");

        Template template = Template.Parse(templateContent);
        var templarParameters = Hash.FromAnonymousObject(new { name });
        var html = template.Render(templarParameters);

        return html;
    }

}
