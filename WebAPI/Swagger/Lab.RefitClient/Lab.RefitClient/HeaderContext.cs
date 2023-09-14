namespace Lab.RefitClient;

public class HeaderContext
{
    public string IdempotencyKey { get; set; }

    public string ApiKey { get; set; }
}