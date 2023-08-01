using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using System.Text;

namespace SBSimulatorMaui;

internal static class Server
{
    public static string InitialPlayerInfo = string.Empty;
    public static string ServerConfig { get; set; }
    public static Room CurrentRoom { get; private set; } = Room.Empty;
    public static bool IsHost { get; private set; } = true;
    public static bool NotificationFlag { get; internal set; } = false;
    static int RoomIndex = 0;
    static CancellationTokenSource Cancellation = new();
    static bool HasEntered = false;
    static bool HasMatched = false;

    static Server()
    {
        ServerConfig = SerializationService.CallSerializer
        (
            0b01101000,
            0b01110100,
            0b01110100,
            0b01110000,
            0b00111010,
            0b00101111,
            0b00101111,
            0b01110000,
            0b01101100,
            0b01110011,
            0b01101011,
            0b00101110,
            0b01101110,
            0b01100101,
            0b01110100,
            0b00101111,
            0b01111000,
            0b01100010,
            0b01100001,
            0b01101101,
            0b01101111,
            0b01100111,
            0b01110101,
            0b01110011
        );

    }

    public static void Initialize()
    {
        Cancellation = new();
        HasEntered = false;
        HasMatched = false;
        IsHost = true;
    }
    static async Task SetUpAsync(CancellationToken ct)
    {
        try
        {
            while (!HasEntered && !ct.IsCancellationRequested)
            {
                HasEntered = await TryFindRoomAsync(ct);
                if (!HasEntered) await Task.Delay(7000, ct);
            }
            if (ct.IsCancellationRequested) return;
            await ReadAsync(ct);
            (CurrentRoom, IsHost) = CurrentRoom.Header switch
            {
                RoomState.Empty => (CurrentRoom with
                {
                    Header = RoomState.Waiting,
                    Player1Info = InitialPlayerInfo,
                    InitialChar = SBExtention.GetRandomChar().ToString(),
                    Seed = SBOptions.Random.Next(65535)
                }, true),
                RoomState.Waiting => (CurrentRoom with
                {
                    Header = RoomState.Full,
                    Player2Info = InitialPlayerInfo
                }, false),
                _ => (CurrentRoom, IsHost)
            };
            await RewriteAsync(ct);
        }
        catch
        {
            throw;
        }
    }
    static async Task WaitUntilMatchingAsync(CancellationToken ct)
    {
        while (CurrentRoom.Header != RoomState.Full && !ct.IsCancellationRequested)
        {
            await ReadAsync(ct);
            if (CurrentRoom.Header != RoomState.Full) await Task.Delay(5000, ct);
        }
        if (ct.IsCancellationRequested) return;
        HasMatched = true;
    }
    public static async Task RunAsync()
    {
        try
        {
            var ct = Cancellation.Token;
            await SetUpAsync(ct);
            if (!HasEntered) return;
            await WaitUntilMatchingAsync(ct);
            if (!HasMatched) return;
        }
        catch (TaskCanceledException)
        {
            return;
        }
    }
    public static async Task CancelAsync()
    {
        try
        {
            await LeaveRoomAsync(Cancellation.Token);
            Cancellation.Cancel();
        }
        catch
        {
            return;
        }
    }
    public static async Task OverwriteAsync(string player1Info, string player2Info)
    {
        try
        {
            CurrentRoom = CurrentRoom with { Player1Info = player1Info, Player2Info = player2Info };
            await RewriteAsync(Cancellation.Token);
        }
        catch
        {
            throw;
        }
    }
    static async Task RewriteAsync(CancellationToken ct)
    {
        try
        {
            var doc = await GetDocumentAsync(ct);
            var button = (IHtmlElement)doc.GetElementsByName("edit").FirstOrDefault();
            var display = (IHtmlTextAreaElement)doc.GetElementById("txt");
            button.DoClick();
            var text = display.TextContent;
            var rooms = text.Split("\n").ToList();
            rooms[RoomIndex] = CurrentRoom.ToString();
            display.Value = string.Join("\r\n", rooms);
            await display.SubmitAsync();
            await WaitAsync(ct);
            button.DoClick();
        }
        catch
        {
            throw;
        }
    }
    static async Task<bool> TryFindRoomAsync(CancellationToken ct)
    {
        try
        {
            var doc = await GetDocumentAsync(ct);
            var text = doc.GetElementById("txt").TextContent;
            var rooms = text.Split("\n").Select(x => x.ToRoom()).ToList();
            var headers = rooms.Select(x => x.Header).ToList();
            if (headers.All(x => x == RoomState.Full)) return false;
            if (headers.Contains(RoomState.Waiting))
            {
                RoomIndex = headers.IndexOf(RoomState.Waiting);
                return true;
            }
            RoomIndex = headers.IndexOf(RoomState.Empty);
            return true;
        }
        catch
        {
            throw;
        }
    }
    static async Task LeaveRoomAsync(CancellationToken ct)
    {
        try
        {
            CurrentRoom = Room.Empty;
            await RewriteAsync(ct);
        }
        catch
        {
            throw;
        }
    }
    static async Task ReadAsync(CancellationToken ct)
    {
        try
        {
            var doc = await GetDocumentAsync(ct);
            var text = doc.GetElementById("txt").TextContent;
            var rooms = text.Split("\n").Select(x => x.ToRoom()).ToList();
            var previousRoom = CurrentRoom with { };
            CurrentRoom = rooms[RoomIndex];
            if (CurrentRoom != previousRoom) NotificationFlag = true;
        }
        catch
        {
            throw;
        }
    }
    public static async Task ForceReadAsync() => await ReadAsync(Cancellation.Token);
    static async Task WaitAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(50, ct);
        }
        catch
        {
            return;
        }
    }
    static async Task<IDocument> GetDocumentAsync(CancellationToken ct)
    {
        try
        {
            var context = BrowsingContext.New(Configuration.Default.WithDefaultLoader());
            var doc = await context.OpenAsync(ServerConfig, ct);
            return doc;
        }
        catch
        {
            throw;
        }
    }
}
static file class SerializationService
{
    internal static string CallSerializer(params byte[] data) => Encoding.UTF8.GetString(data);
}
