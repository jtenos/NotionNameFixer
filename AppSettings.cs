namespace NotionNameFixer;

internal class AppSettings
{
	public List<DirPair> DirPairs { get; set; } = default!;
}
internal record class DirPair(string Input, string Output);
