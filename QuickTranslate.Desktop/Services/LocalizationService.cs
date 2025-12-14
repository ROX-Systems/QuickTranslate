using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Windows;
using QuickTranslate.Core.Interfaces;

namespace QuickTranslate.Desktop.Services
{
    public class LocalizationService : INotifyPropertyChanged
    {
        private static readonly Lazy<LocalizationService> _instance = new(() => new LocalizationService());
        public static LocalizationService Instance => _instance.Value;

        private ResourceManager _resourceManager;
        private CultureInfo _currentCulture;

        public event PropertyChangedEventHandler? PropertyChanged;

        private LocalizationService()
        {
            _resourceManager = new ResourceManager("QuickTranslate.Desktop.Resources", typeof(LocalizationService).Assembly);
            
            // Try to get language from settings first, otherwise detect system language
            var settings = new QuickTranslate.Core.Services.SettingsStore();
            var savedLanguage = settings.Load().InterfaceLanguage;
            
            if (string.IsNullOrEmpty(savedLanguage))
            {
                // First run - detect system language
                _currentCulture = DetectSystemLanguage();
                // Save the detected language
                var appSettings = settings.Load();
                appSettings.InterfaceLanguage = _currentCulture.TwoLetterISOLanguageName;
                settings.Save(appSettings);
            }
            else
            {
                _currentCulture = new CultureInfo(savedLanguage);
            }
            
            // Apply the culture
            Thread.CurrentThread.CurrentUICulture = _currentCulture;
            Thread.CurrentThread.CurrentCulture = _currentCulture;
        }

        private static CultureInfo DetectSystemLanguage()
        {
            var systemLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            
            // Map to supported languages
            return systemLanguage switch
            {
                "ru" => new CultureInfo("ru"),
                "os" => new CultureInfo("os"),
                "en" => new CultureInfo("en"),
                _ => new CultureInfo("en") // Default to English for unsupported languages
            };
        }

        public string this[string key]
        {
            get
            {
                try
                {
                    return _resourceManager.GetString(key, _currentCulture) ?? key;
                }
                catch
                {
                    return key;
                }
            }
        }

        public void SetCulture(string cultureCode)
        {
            var culture = new CultureInfo(cultureCode);
            if (_currentCulture.Name != culture.Name)
            {
                _currentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
                Thread.CurrentThread.CurrentCulture = culture;
                
                // Notify all properties changed
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
            }
        }

        public string CurrentLanguage => _currentCulture.Name;
        
        public string CurrentLanguageDisplayName => _currentCulture.NativeName;
    }

    public static class Loc
    {
        public static LocalizationService Instance => LocalizationService.Instance;
    }
}
