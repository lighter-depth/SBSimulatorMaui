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
    /// �����̏����C���|�[�g���܂��B
    /// </summary>
    static async Task ImportDictionaryAsync()
    {
        SBDictionary.NoTypeWords = new();
        SBDictionary.TypedWords = new();

        // �����������̓ǂݍ���
        var notypeFiles = new List<string>();
        foreach (var i in new[]
        {
            "no-type-words", "no-type-words-extension-common", "no-type-words-extension-main",
            "no-type-words-extension-proper", "no-type-words-extension-quantity", "no-type-words-extension-sahen-conn"
        })
            notypeFiles.Add($"{i}.csv");
        foreach (var file in notypeFiles) await LoadNoTypeWordsFromFile(file);

        // �L���������̓ǂݍ���
        var typedFiles = new List<string>();
        foreach (var i in new[]
        {
            "��", "��", "��", "��", "��",
            "��", "��", "��", "��", "��",
            "��", "��", "��", "��", "��",
            "��", "��", "��", "��", "��",
            "��", "��", "��", "��", "��",
            "��", "��", "��", "��", "��",
            "��", "��", "��", "��", "��",
            "��", "��", "��",
            "��", "��", "��", "��", "��",
            "��",
            "��", "��", "��", "��", "��",
            "��", "��", "��", "��", "��",
            "��", "��", "��",
            "��", "��", "��", "��", "��",
            "��", "��", "��", "��", "��"
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