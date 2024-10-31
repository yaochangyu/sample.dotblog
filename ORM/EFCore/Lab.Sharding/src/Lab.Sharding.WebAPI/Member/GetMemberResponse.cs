namespace Lab.Sharding.WebAPI.Member;

public class GetMemberResponse
{
	public string Id { get; set; }

	public string? Name { get; set; }

	public int? Age { get; set; }

	public string Email { get; set; }

	public long? SequenceId { get; set; }
}