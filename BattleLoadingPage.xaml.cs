namespace SBSimulatorMaui;

public partial class BattleLoadingPage : ContentPage
{
    public static bool IsExecuted { get; private set; } = false;
	public BattleLoadingPage()
	{
		InitializeComponent();
		LoadBattle();
	}

	public async void LoadBattle()
	{
        await ImportDictionaryAsync();
        IsExecuted = true;
		await Shell.Current.GoToAsync(nameof(BattlePage));
	}
    /// <summary>
    /// 辞書の情報をインポートします。
    /// </summary>
    static async Task ImportDictionaryAsync()
    {
        using var noTypeStream = await FileSystem.Current.OpenAppPackageFileAsync("no-type-words.csv");
        using var noTypeWordsReader = new StreamReader(noTypeStream);
        using var exNoTypeStream = await FileSystem.OpenAppPackageFileAsync("no-type-word-extension.csv");
        using var exNoTypeWordReader = new StreamReader(exNoTypeStream);
        SBDictionary.NoTypeWords = new();
        SBDictionary.NoTypeWordEx = new();
        SBDictionary.TypedWords = new();
        while (!noTypeWordsReader.EndOfStream)
        {
            var line = await noTypeWordsReader.ReadLineAsync() ?? string.Empty;
            SBDictionary.NoTypeWords.Add(line);
        }
        while (!exNoTypeWordReader.EndOfStream)
        {
            var line = await exNoTypeWordReader.ReadLineAsync() ?? string.Empty;
            SBDictionary.NoTypeWordEx.Add(line);
        }
        var files = new List<string>();
        foreach (var i in new[]
        {
            "あ", "い", "う", "え", "お",
            "か", "き", "く", "け", "こ",
            "さ", "し", "す", "せ", "そ",
            "た", "ち", "つ", "て", "と",
            "な", "に", "ぬ", "ね", "の",
            "は", "ひ", "ふ", "へ", "ほ",
            "ま", "み", "む", "め", "も",
            "や", "ゆ", "よ",
            "ら", "り", "る", "れ", "ろ",
            "わ",
            "が", "ぎ", "ぐ", "げ", "ご",
            "ざ", "じ", "ず", "ぜ", "ぞ",
            "だ", "で", "ど",
            "ば", "び", "ぶ", "べ", "ぼ",
            "ぱ", "ぴ", "ぷ", "ぺ", "ぽ"
        })
            files.Add($"typed-words-{i}.csv");
        foreach (var file in files) await LoadTypedWordsFromFile(file);
    }
    static async Task LoadTypedWordsFromFile(string path)
    {
        using var stream = await FileSystem.Current.OpenAppPackageFileAsync(path);
        using var reader = new StreamReader(stream);
        while (!reader.EndOfStream)
        {
            var line = (await reader.ReadLineAsync() ?? string.Empty).Trim().Split();
            if (line.Length == 2) SBDictionary.TypedWords.TryAdd(line[0], new() { line[1].StringToType() });
            else if (line.Length == 3) SBDictionary.TypedWords.TryAdd(line[0], new() { line[1].StringToType(), line[2].StringToType() });
        }
    }
}