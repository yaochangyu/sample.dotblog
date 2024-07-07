using System.Text.Json.Nodes;
using Json.Path;
using Xunit;

namespace Lab.JsonPathForTestCase.Test;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var json = """
                   {
                     "store": {
                       "book": [
                         {
                           "category": "reference",
                           "author": "Nigel Rees",
                           "title": "Sayings of the Century",
                           "price": 8.95
                         },
                         {
                           "category": "fiction",
                           "author": "Evelyn Waugh",
                           "title": "Sword of Honour",
                           "price": 12.99
                         },
                         {
                           "category": "fiction",
                           "author": "Herman Melville",
                           "title": "Moby Dick",
                           "isbn": "0-553-21311-3",
                           "price": 8.99
                         },
                         {
                           "category": "fiction",
                           "author": "J. R. R. Tolkien",
                           "title": "The Lord of the Rings",
                           "isbn": "0-395-19395-8",
                           "price": 22.99
                         }
                       ],
                       "bicycle": {
                         "color": "red",
                         "price": 19.95
                       }
                     }
                   }
                   """;
        
        var instance = JsonNode.Parse(json);
        var path = JsonPath.Parse("$.store.book[0].category");
        var result = path.Evaluate(instance);

    }
}