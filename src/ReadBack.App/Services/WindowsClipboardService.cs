using WpfClipboard = System.Windows.Clipboard;

namespace ReadBack.App.Services;

public class WindowsClipboardService : IClipboardService
{
    public string? GetText()
    {
        for (int i = 0; i < 5; i++)
        {
            try
            {
                if (WpfClipboard.ContainsText())
                {
                    string text = WpfClipboard.GetText();
                    if (!string.IsNullOrWhiteSpace(text))
                        return text;
                }
                return null;
            }
            catch
            {
                Thread.Sleep(40);
            }
        }
        return null;
    }
}
