

namespace SBSimulatorMaui;

internal record Room(RoomState Header, string Player1Info, string Player2Info, string InitialChar, int Seed) 
{
    public static Room Empty => new(RoomState.Empty, string.Empty, string.Empty, string.Empty, 0);
    public override string ToString()
    {
        return $"{Header.StateToString()}|{Player1Info}|{Player2Info}|{InitialChar}|{Seed}|";
    }
}

internal enum RoomState { Empty, Waiting, Full }
internal static class RoomEx
{
    public static RoomState ToState(this string text)
    {
        return text switch
        {
            "E" => RoomState.Empty,
            "W" => RoomState.Waiting,
            _ => RoomState.Full
        };
    }
    public static string StateToString(this RoomState state)
    {
        return state switch
        {
            RoomState.Empty => "E",
            RoomState.Waiting => "W",
            _ => "F"
        };
    }
    public static Room ToRoom(this string text)
    {
        var roomText = text.Split("|");
        return new(roomText[0].ToState(), roomText[1], roomText[2], roomText[3], int.Parse(roomText[4]));
    }
}