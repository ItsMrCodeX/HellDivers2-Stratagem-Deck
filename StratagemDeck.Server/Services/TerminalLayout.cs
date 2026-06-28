using System.Collections.Concurrent;

namespace StratagemDeck.Server.Services;

public enum LogCategory
{
    Info,
    Network,
    Stratagem,
    Success,
    Error,
    System
}

public record LogEntry(string Message, LogCategory Category, DateTime Timestamp);

public class TerminalLayout : IDisposable
{
    private int _leftWidth;
    private int _sepCol;
    private int _rightWidth;
    private int _height;
    private int _logAreaHeight;

    private readonly List<LogEntry> _logHistory = [];
    private readonly ConcurrentQueue<LogEntry> _pendingLogs = new();
    private int _scrollOffset;
    private bool _running;

    private readonly List<string> _ips = [];
    private int _currentIpIndex;
    private string _pin = "";
    private string _qrCode = "";
    private bool _ignoreInput;

    public event Action? OnRegenerateRequested;
    public event Action<int>? OnIpCycleRequested;

    public TerminalLayout()
    {
        RecalcDimensions();
    }

    public bool IgnoreInput
    {
        set => _ignoreInput = value;
    }

    public string CurrentIp => _ips.Count > 0 ? _ips[_currentIpIndex] : "0.0.0.0";
    public bool HasMultipleIps => _ips.Count > 1;

    private void RecalcDimensions()
    {
        _leftWidth = Math.Max(20, Console.WindowWidth / 3);
        _sepCol = _leftWidth;
        _rightWidth = Math.Max(20, Console.WindowWidth - _sepCol - 1);
        _height = Console.WindowHeight;
        _logAreaHeight = Math.Max(3, _height - 1);
    }

    public void Initialize(List<string> ips, string pin, string qrCode)
    {
        RecalcDimensions();
        _ips.Clear();
        _ips.AddRange(ips);
        _currentIpIndex = _ips.Count > 0 ? _ips.Count - 1 : 0;
        _pin = pin;
        _qrCode = qrCode;

        Console.Clear();
        Console.CursorVisible = false;

        DrawSeparator();
        RenderLeftPane();
    }

    public void UpdateQrCode(string ip, string pin, string qrCode)
    {
        _pin = pin;
        _qrCode = qrCode;
        RenderLeftPane();
    }

    public void EnqueueLog(LogEntry entry)
    {
        _pendingLogs.Enqueue(entry);
    }

    /// <summary>
    /// Run the TUI main loop. Blocks until user presses Q.
    /// </summary>
    public void Run()
    {
        _running = true;
        while (_running)
        {
            ProcessPendingLogs();

            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                HandleKey(key);
            }
            else
            {
                Thread.Sleep(30);
            }
        }

        Console.CursorVisible = true;
    }

    private void ProcessPendingLogs()
    {
        bool hasNew = false;
        while (_pendingLogs.TryDequeue(out var entry))
        {
            _logHistory.Add(entry);
            hasNew = true;
        }

        if (hasNew && _scrollOffset == 0)
        {
            RenderRightPane();
        }
    }

    private void HandleKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Q:
                _running = false;
                break;

            case ConsoleKey.R:
                OnRegenerateRequested?.Invoke();
                break;

            case ConsoleKey.UpArrow:
            {
                int maxOffset = Math.Max(0, _logHistory.Count - _logAreaHeight);
                if (_scrollOffset < maxOffset)
                {
                    _scrollOffset++;
                    RenderRightPane();
                }
                break;
            }

            case ConsoleKey.DownArrow:
                if (_scrollOffset > 0)
                {
                    _scrollOffset--;
                    RenderRightPane();
                }
                break;

            case ConsoleKey.PageUp:
            {
                int maxOffset = Math.Max(0, _logHistory.Count - _logAreaHeight);
                _scrollOffset = Math.Min(_scrollOffset + _logAreaHeight - 1, maxOffset);
                RenderRightPane();
                break;
            }

            case ConsoleKey.PageDown:
                _scrollOffset = Math.Max(_scrollOffset - _logAreaHeight + 1, 0);
                RenderRightPane();
                break;

            case ConsoleKey.End:
                _scrollOffset = 0;
                RenderRightPane();
                break;

            case ConsoleKey.LeftArrow:
                if (_ignoreInput) break;
                if (_ips.Count > 1)
                {
                    _currentIpIndex = (_currentIpIndex - 1 + _ips.Count) % _ips.Count;
                    OnIpCycleRequested?.Invoke(-1);
                }
                break;

            case ConsoleKey.RightArrow:
                if (_ignoreInput) break;
                if (_ips.Count > 1)
                {
                    _currentIpIndex = (_currentIpIndex + 1) % _ips.Count;
                    OnIpCycleRequested?.Invoke(1);
                }
                break;
        }
    }

    private void DrawSeparator()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        for (int y = 0; y < _height; y++)
        {
            Console.SetCursorPosition(_sepCol, y);
            Console.Write('│');
        }
        Console.ResetColor();
    }

    private void RenderLeftPane()
    {
        int y = 0;

        Console.SetCursorPosition(0, y);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("Stratagem Deck Server".PadRight(_leftWidth));
        Console.ResetColor();
        y++;

        y++;

        var qrLines = _qrCode.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        bool isModuleQr = qrLines.Length > 0 && qrLines[0].Contains('█');
        if (isModuleQr && qrLines.Length >= 2)
        {
            string[] sqLines = ConvertQrToSquare(qrLines);
            foreach (var line in sqLines)
            {
                if (y >= _height) break;
                Console.SetCursorPosition(0, y);
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write((" " + line).PadRight(_leftWidth));
                Console.ResetColor();
                y++;
            }
        }
        else
        {
            foreach (var line in qrLines)
            {
                if (y >= _height) break;
                Console.SetCursorPosition(0, y);
                Console.Write((" " + line).PadRight(_leftWidth));
                y++;
            }
        }

        y++;

        if (y < _height)
        {
            Console.SetCursorPosition(0, y);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("PIN".PadRight(_leftWidth));
            Console.ResetColor();
            y++;
        }

        if (y < _height)
        {
            Console.SetCursorPosition(0, y);
            Console.ForegroundColor = ConsoleColor.White;
            string pinDisplay = string.Join("   ", _pin.ToCharArray());
            Console.Write(pinDisplay.PadRight(_leftWidth));
            Console.ResetColor();
            y++;
        }

        y++;

        if (y < _height)
        {
            Console.SetCursorPosition(0, y);
            Console.ForegroundColor = ConsoleColor.Gray;
            string ipLine = CurrentIp;
            if (HasMultipleIps)
                ipLine += $"  ({_currentIpIndex + 1}/{_ips.Count})";
            Console.Write(ipLine.PadRight(_leftWidth));
            Console.ResetColor();
            y++;
        }
        if (y < _height)
        {
            Console.SetCursorPosition(0, y);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("Port 12345".PadRight(_leftWidth));
            Console.ResetColor();
            y++;
        }

        y++;

        if (y < _height)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.SetCursorPosition(0, y); Console.Write("R  Regenerate PIN".PadRight(_leftWidth)); y++;
            if (y < _height) { Console.SetCursorPosition(0, y); Console.Write("Q  Quit".PadRight(_leftWidth)); y++; }
            if (y < _height && HasMultipleIps) { Console.SetCursorPosition(0, y); Console.Write("< >  Switch IP".PadRight(_leftWidth)); y++; }
            if (y < _height) { Console.SetCursorPosition(0, y); Console.Write("Up/Dn  Scroll".PadRight(_leftWidth)); y++; }
            Console.ResetColor();
        }

        while (y < _height)
        {
            Console.SetCursorPosition(0, y);
            Console.Write(new string(' ', _leftWidth));
            y++;
        }

        DrawSeparator();
    }

    private static string[] ConvertQrToSquare(string[] rawLines)
    {
        var result = new List<string>();
        for (int r = 0; r < rawLines.Length; r += 2)
        {
            string top = rawLines[r];
            string bot = r + 1 < rawLines.Length ? rawLines[r + 1] : new string(' ', top.Length);

            int commonLen = Math.Min(top.Length, bot.Length);
            var sb = new System.Text.StringBuilder(commonLen);
            for (int c = 0; c < commonLen; c++)
            {
                bool topFilled = top[c] == '█';
                bool botFilled = bot[c] == '█';
                sb.Append((topFilled, botFilled) switch
                {
                    (true, true) => '█',
                    (true, false) => '▀',
                    (false, true) => '▄',
                    (false, false) => ' ',
                });
            }
            result.Add(sb.ToString());
        }

        while (result.Count > 0 && string.IsNullOrWhiteSpace(result[^1]))
            result.RemoveAt(result.Count - 1);

        return [.. result];
    }

    private void RenderRightPane()
    {
        int visibleLines = _logAreaHeight;
        int total = _logHistory.Count;
        int startIdx = Math.Max(0, total - visibleLines - _scrollOffset);
        int endIdx = Math.Min(total, startIdx + visibleLines);

        for (int i = 0; i < visibleLines; i++)
        {
            int y = i;
            Console.SetCursorPosition(_sepCol + 2, y);

            if (startIdx + i < endIdx)
            {
                var entry = _logHistory[startIdx + i];
                Console.ForegroundColor = GetColor(entry.Category);
                string logLine = $"[{entry.Timestamp:HH:mm:ss}] {entry.Message}";
                int maxLen = _rightWidth - 2;
                if (logLine.Length > maxLen)
                    logLine = logLine[..maxLen];
                Console.Write(logLine.PadRight(maxLen));
                Console.ResetColor();
            }
            else
            {
                Console.Write(new string(' ', _rightWidth - 2));
            }
        }

        // Scroll indicator
        Console.SetCursorPosition(_sepCol + 2, _logAreaHeight);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        if (_scrollOffset == 0 && total > 0)
        {
            int shown = Math.Min(visibleLines, total);
            Console.Write($"(auto {shown}/{total})".PadRight(_rightWidth - 2));
        }
        else if (total > 0)
        {
            int shown = Math.Min(visibleLines, total);
            int current = total - _scrollOffset - shown + 1;
            if (current < 1) current = 1;
            Console.Write($"({current}-{current + shown - 1}/{total})".PadRight(_rightWidth - 2));
        }
        else
        {
            Console.Write(new string(' ', _rightWidth - 2));
        }
        Console.ResetColor();
    }

    // ─── Helpers ──────────────────────────────────────────

    private static ConsoleColor GetColor(LogCategory cat) => cat switch
    {
        LogCategory.Network => ConsoleColor.Cyan,
        LogCategory.Stratagem => ConsoleColor.Yellow,
        LogCategory.Success => ConsoleColor.Green,
        LogCategory.Error => ConsoleColor.Red,
        LogCategory.System => ConsoleColor.DarkGray,
        _ => ConsoleColor.Gray
    };

    public void Dispose()
    {
        Console.CursorVisible = true;
        Console.ResetColor();
    }
}
