namespace Lab.DeviceFingerprint.WebApi.Application.Services;

public interface IOtpGenerator
{
    string Generate();
}

public class RandomOtpGenerator : IOtpGenerator
{
    public string Generate() => Random.Shared.Next(100000, 999999).ToString();
}
