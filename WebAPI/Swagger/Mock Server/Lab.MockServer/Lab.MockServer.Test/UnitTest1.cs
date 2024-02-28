using System.Net;
using System.Text;
using System.Text.Json.JsonDiffPatch;
using FluentAssertions;

namespace Lab.MockServer.Test;

public class UnitTest1
{
    static readonly HttpClient Client = new()
    {
        BaseAddress = new Uri("http://localhost:1080/"),
    };

    [Fact]
    public void 動態建立假端點()
    {
        //建立假的端點
        var url = "mockserver/expectation";
        var body = """
                   {
                     "httpRequest": {
                       "method": "GET",
                       "path": "/view/cart"
                     },
                     "httpResponse": {
                       "body": "some_response_body"
                     }
                   }
                   """;
        var request = new HttpRequestMessage(HttpMethod.Put, url);
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = Client.SendAsync(request).Result;
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        //呼叫假的端點
        var getCartResult = Client.GetStringAsync("view/cart?cartId=055CA455-1DF7-45BB-8535-4F83E7266092").Result;
        getCartResult.Should().Be("some_response_body");
    }

    [Fact]
    public void 匯入OpenApi()
    {
        //建立假的端點
        var url = "mockserver/openapi";

        var yaml = @"
openapi: '3.0.0'
info:
  version: 1.0.0
  title: Swagger Petstore
  license:
    name: MIT
servers:
  - url: http://petstore.swagger.io/v1
paths:
  /pets:
    get:
      summary: List all pets
      operationId: listPets
      tags:
        - pets
      parameters:
        - name: limit
          in: query
          description: How many items to return at one time (max 100)
          required: false
          schema:
            type: integer
            maximum: 100
            format: int32
      responses:
        '200':
          description: A paged array of pets
          headers:
            x-next:
              description: A link to the next page of responses
              schema:
                type: string
          content:
            application/json:    
              schema:
                $ref: '#/components/schemas/Pets'
        default:
          description: unexpected error
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Error'
    post:
      summary: Create a pet
      operationId: createPets
      tags:
        - pets
      requestBody:
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/Pet'
        required: true
      responses:
        '201':
          description: Null response
        default:
          description: unexpected error
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Error'
  /pets/{petId}:
    get:
      summary: Info for a specific pet
      operationId: showPetById
      tags:
        - pets
      parameters:
        - name: petId
          in: path
          required: true
          description: The id of the pet to retrieve
          schema:
            type: string
      responses:
        '200':
          description: Expected response to a valid request
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Pet'
        default:
          description: unexpected error
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Error'
components:
  schemas:
    Pet:
      type: object
      required:
        - id
        - name
      properties:
        id:
          type: integer
          format: int64
        name:
          type: string
        tag:
          type: string
    Pets:
      type: array
      maxItems: 100
      items:
        $ref: '#/components/schemas/Pet'
    Error:
      type: object
      required:
        - code
        - message
      properties:
        code:
          type: integer
          format: int32
        message:
          type: string";

        var httpFile = "https://raw.githubusercontent.com/OAI/OpenAPI-Specification/main/examples/v3.0/petstore.yaml";
        var jsonPayload = new
        {
            specUrlOrPayload = httpFile
        };

        var body = System.Text.Json.JsonSerializer.Serialize(jsonPayload);
        var request = new HttpRequestMessage(HttpMethod.Put, url);
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = Client.SendAsync(request).Result;
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        //呼叫假的端點
        var getCartResult = Client.GetStringAsync("/v1/pets").Result;

        var expected = """
                       [
                         {
                           "id": 0,
                           "name": "some_string_value",
                           "tag": "some_string_value"
                         }
                       ]
                       """;
        var diff = JsonDiffPatcher.Diff(expected, getCartResult);
        Assert.Null(diff);
    }
}