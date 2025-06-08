using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Lab.HashiCorpVault.Client;

public class VaultApiClient(HttpClient client, string vaultToken)
{
    private string _vaultToken = vaultToken;

    public void UpdateToken(string token)
    {
        _vaultToken = token;
    }

    public async Task<JsonObject> EnableAuthMethodAsync(string authMethod, string path = null)
    {
        var payload = new
        {
            type = authMethod
        };

        return await SendRequestAsync(HttpMethod.Post, $"/v1/sys/auth/{path}", payload);
    }

    public async Task<JsonObject> EnableSecretEngineAsync(string engine, string path)
    {
        var payload = new
        {
            type = engine
        };

        return await SendRequestAsync(HttpMethod.Post, $"/v1/sys/mounts/{path}", payload);
    }

    public async Task<JsonObject> WriteSecretAsync(string path, Dictionary<string, string> data)
    {
        var payload = new
        {
            data
        };

        return await SendRequestAsync(HttpMethod.Post, $"/v1/{path}", payload);
    }

    public async Task<JsonObject> WritePolicyAsync(string name, string policy)
    {
        var payload = new
        {
            policy
        };

        return await SendRequestAsync(HttpMethod.Put, $"/v1/sys/policies/acl/{name}", payload);
    }

    public async Task<JsonObject> CreateAppRoleAsync(string roleName, string policies, string tokenTtl, string tokenMaxTtl)
    {
        var payload = new
        {
            token_policies = policies,
            token_ttl = tokenTtl,
            token_max_ttl = tokenMaxTtl
        };

        return await SendRequestAsync(HttpMethod.Post, $"/v1/auth/approle/role/{roleName}", payload);
    }

    public async Task<JsonObject> GetRoleIdAsync(string roleName)
    {
        return await SendRequestAsync(HttpMethod.Get, $"/v1/auth/approle/role/{roleName}/role-id");
    }

    public async Task<JsonObject> GenerateSecretIdAsync(string roleName)
    {
        return await SendRequestAsync(HttpMethod.Post, $"/v1/auth/approle/role/{roleName}/secret-id");
    }

    public async Task<JsonObject> CreateTokenAsync(string policy, string ttl)
    {
        var payload = new
        {
            policies = new[] { policy },
            ttl
        };

        return await SendRequestAsync(HttpMethod.Post, "/v1/auth/token/create", payload);
    }

    private async Task<JsonObject> SendRequestAsync(HttpMethod method, string url, object payload = null)
    {
        var request = new HttpRequestMessage(method, url);

        request.Headers.Add("X-Vault-Token", _vaultToken);

        if (payload != null)
        {
            var json = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        var response = await client.SendAsync(request);
        return await HandleResponseAsync(response);
    }

    private async Task<JsonObject> HandleResponseAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Vault API request failed with status {response.StatusCode}: {content}");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return new JsonObject();
        }

        return JsonNode.Parse(content)?.AsObject() ?? new JsonObject();
    }
}
