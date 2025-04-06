using GalaSoft.MvvmLight.Messaging;
using MahApps.Metro.Controls;
using FocusTreeManager.ViewModel;
using FocusTreeManager.Helper;

namespace FocusTreeManager.Views;

public partial class EditScript : MetroWindow
{

    public EditScript()
    {
        InitializeComponent();
        loadLocales();
        DataContext = new ViewModelLocator().Scripter;
        Messenger.Default.Register<NotificationMessage>(this, NotificationMessageReceived);
    }

    private void NotificationMessageReceived(NotificationMessage msg)
    {
        if (msg.Notification == "ChangeLanguage")
        {
            loadLocales();
        }
    }

    private void loadLocales()
    {
        Resources.MergedDictionaries.Add(LocalizationHelper.getLocale());
    }
}