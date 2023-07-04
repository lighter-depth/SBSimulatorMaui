using Plugin.Maui.Audio;
using System.Linq.Expressions;

namespace SBSimulatorMaui;

static class SBAudioManager
{
    static readonly Dictionary<string, IAudioPlayer> audioPlayers = new();

    public static async Task Init()
    {
        foreach (var i in new[]
        {
            "animal", "art", "body", "bug", "cloth", "concent", "denno", "down", "effective", "emote", "end", "food", "heal", "health", "horizon", "insult", "last",
            "math", "mech", "middmg", "ninja", "noneffective", "normal", "overflow", "pera", "person", "place", "plant", "play", "poison", "poison_heal", "religion", 
            "science", "seed_damage", "seeded", "society", "sports", "start", "tale", "time", "up", "violence", "warn", "weather", "wonderland", "work"
        })
        {
            audioPlayers.TryAdd(i, await CreatePlayerAsync($"{i}.mp3"));
        }
        foreach(var i in new[] { "denno", "wonderland" })
        {
            audioPlayers[i].Volume = 0.36f;
            audioPlayers[i].Loop = true;
        }
        foreach(var i in new[] { "horizon", "overflow", "last", "ninja" })
        {
            audioPlayers[i].Volume = 0.28f;
            audioPlayers[i].Loop = true;
        }
        audioPlayers["pera"].Volume = 0.4f;
        audioPlayers["art"].Volume = 0.8f;
    }

    static async Task<IAudioPlayer> CreatePlayerAsync(string fileName)
    {
        return AudioManager.Current.CreatePlayer(await FileSystem.OpenAppPackageFileAsync(fileName));
    }
    public static void PlaySound(string soundName)
    {
        if (audioPlayers.TryGetValue(soundName, out var player)) player.Play();
    }
    public static void StopSound(string soundName)
    {
        if (audioPlayers.TryGetValue(soundName, out var player)) player.Stop();
    }
    public static void CancelAudio()
    {
        foreach (var i in audioPlayers.Values) if (i.IsPlaying) i.Stop();
    }
    public static bool TryGetPlayer(string soundName, out IAudioPlayer player) => audioPlayers.TryGetValue(soundName, out player);
}
