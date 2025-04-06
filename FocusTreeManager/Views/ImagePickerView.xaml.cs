using FocusTreeManager.Helper;
using FocusTreeManager.ViewModel;
using GalaSoft.MvvmLight.Messaging;
using MahApps.Metro.Controls;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace FocusTreeManager.Views;

/// <summary>
/// Description for ChangeImage.
/// </summary>
public partial class ImagePickerView : MetroWindow
{
    public ChangeImageViewModel ViewModel => (ChangeImageViewModel)DataContext;
    /// <summary>
    /// Initializes a new instance of the ChangeImage class.
    /// </summary>
    public ImagePickerView()
    {
        InitializeComponent();
        loadLocales();
        Messenger.Default.Register<NotificationMessage>(this, NotificationMessageReceived);
        MaxHeight = SystemParameters.PrimaryScreenHeight * 0.90;
        MaxWidth = SystemParameters.PrimaryScreenWidth * 0.90;
        DataContext = App.Current.Services.GetRequiredService<ChangeImageViewModel>();
    }

    private void NotificationMessageReceived(NotificationMessage msg)
    {
        if (msg.Notification == "HideChangeImage")
        {
            Hide();
        }
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