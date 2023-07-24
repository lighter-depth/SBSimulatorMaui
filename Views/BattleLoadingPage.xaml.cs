namespace SBSimulatorMaui;

public partial class BattleLoadingPage : ContentPage
{
    public static bool IsOnline { get; internal set; } = false;
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
		await Shell.Current.GoToAsync(IsOnline ? nameof(OnlineBattlePage) : nameof(BattlePage));
	}
    /// <summary>
    /// 辞書の情報をインポートします。
    /// </summary>
    static async Task ImportDictionaryAsync()
    {
        SBDictionary.NoTypeWords = new();
        SBDictionary.TypedWords = new();

        // 無属性辞書の読み込み
        var notypeFiles = new List<string>();
        foreach (var i in new[]
        {
            "no-type-words", "no-type-words-extension-common", "no-type-words-extension-main",
            "no-type-words-extension-proper", "no-type-words-extension-quantity", "no-type-words-extension-sahen-conn"
        })
            notypeFiles.Add($"{i}.csv");
        foreach (var file in notypeFiles) await LoadNoTypeWordsFromFile(file);

        // 有属性辞書の読み込み
        var typedFiles = new List<string>();
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
            typedFiles.Add($"typed-words-{i}.csv");
        foreach (var file in typedFiles) await LoadTypedWordsFromFile(file);
    }
    static async Task LoadNoTypeWordsFromFile(string path)
    {
        using var stream = await FileSystem.Current.OpenAppPackageFileAsync(path);
        using var reader = new StreamReader(stream);
        while(!reader.EndOfStream) SBDictionary.NoTypeWords.Add(await reader.ReadLineAsync() ?? string.Empty);
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