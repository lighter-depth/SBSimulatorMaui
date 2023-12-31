﻿namespace SBSimulatorMaui;

internal static class SBOptions
{
    public static string Version => "v0.2.1";
    public static bool AllowCustomAbility { get; internal set; } = true;
    public static string MainBgm { get; internal set; } = "horizon";
    public static string BattleBgm { get; internal set; } = "overflow";
    public static Random Random { get; internal set; } = new();
}
