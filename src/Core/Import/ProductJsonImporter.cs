using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class ProductJsonImporter
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static ImportResult<ProductDto> Load(string path)
    {
        try
        {
            var items = JsonSerializer.Deserialize<List<ProductDto>>(File.ReadAllText(path), Options) ?? [];
            return new ImportResult<ProductDto>(items, []);
        }
        catch (JsonException ex)
        {
            return new ImportResult<ProductDto>([], [$"некоректний JSON: {ex.Message}"]);
        }
    }
}
