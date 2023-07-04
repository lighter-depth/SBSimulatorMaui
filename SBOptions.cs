
using Plugin.Maui.Audio;

namespace SBSimulatorMaui;

internal static class SBOptions
{
    public static string Version => "v0.1.0";
    public static bool AllowCustomAbility { get; internal set; } = true;
    public static string MainBgm { get; internal set; } = "horizon";
    public static string BattleBgm { get; internal set; } = "overflow";
}
