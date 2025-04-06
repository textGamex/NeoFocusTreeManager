using FocusTreeManager.Helper;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace FocusTreeManager.ViewModel;

/// <summary>
/// This class contains properties that a View can data bind to.
/// <para>
/// See http://www.galasoft.ch/mvvm
/// </para>
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    private LanguageSelector selectedLanguage;

    public List<LanguageSelector> AvailableLanguages { get; }

    public LanguageSelector SelectedLanguage
    {
        get
        {
            return selectedLanguage;
        }
        set
        {
            selectedLanguage = value;
            Configurator.setLanguage(value.FileName);
            Messenger.Default.Send(new NotificationMessage(this, "ChangeLanguage"));
            RaisePropertyChanged(() => SelectedLanguage);
        }
    }

    public string GamePath
    {
        get
        {
            return Configurator.getGamePath();
        }
        set
        {
            Configurator.setGamePath(value);
            RaisePropertyChanged(() => GamePath);
            AsyncImageLoader.AsyncImageLoader.Worker.RestartStarterWorker();
        }
    }

    public string ScripterPreference
    {
        get
        {
            return Configurator.getScripterPreference();
        }
        set
        {
            Configurator.setScripterPreference(value);
            RaisePropertyChanged(() => ScripterPreference);
        }
    }

    public string Message { get; }

    public RelayCommand FindCommand { get; private set; }

    /// <summary>
    /// Initializes a new instance of the SettingsViewModel class.
    /// </summary>
    public SettingsViewModel()
    {
        AvailableLanguages = Configurator.returnAllLanguages();
        selectedLanguage = AvailableLanguages.SingleOrDefault(l => l.FileName ==
                                                                   Configurator.getLanguage());
        FindCommand = new RelayCommand(SelectGameFolder);
        if (!Configurator.getFirstStart())
        {
            Message = LocalizationHelper.getValueForKey("First_Start");
        }
        else
        {
            Message = "";
        }
    }

    public void SelectGameFolder()
    {
        var dialog = new OpenFolderDialog()
        {
            Title = LocalizationHelper.getValueForKey("Game_Path_Title"),
            InitialDirectory = "C:",
            DefaultDirectory = "C:",
            Multiselect = false
        };
        if (dialog.ShowDialog() == true)
        {
            GamePath = dialog.FolderName;
        }
        Activate();
    }

    private void Activate()
    {
        foreach (Window window in Application.Current.Windows)
        {
            if (window.DataContext == this)
            {
                window.Activate();
            }
        }
    }
}