using ReadBack.Core.Models;

namespace ReadBack.Core.Services;

public interface ISettingsService
{
    AppSettings CurrentSettings { get; }
    void Save();
    event EventHandler<AppSettings>? SettingsChanged;
}
