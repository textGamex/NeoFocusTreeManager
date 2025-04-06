using FocusTreeManager.Helper;
using GalaSoft.MvvmLight.Messaging;
using MahApps.Metro.Controls;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;

namespace FocusTreeManager.Views;

public partial class ProjectEditor : MetroWindow
{
    public ProjectEditor()
    {
        InitializeComponent();
        loadLocales();
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

    private void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = LocalizationHelper.getValueForKey("Project_Select"),
            InitialDirectory = "C:",
            DefaultDirectory = "C:",
            Filter = "Project (*.xh4prj)|*.xh4prj",
            Multiselect = false
        };
        if (dialog.ShowDialog() == true)
        {
            string filename = dialog.FileName;
            if (string.IsNullOrEmpty(Path.GetExtension(filename)))
            {
                filename += ".xh4prj";
            }
            ((TextBox)sender).Text = filename;
            var bindingExpression = ((TextBox)sender)
                .GetBindingExpression(TextBox.TextProperty);
            bindingExpression?.UpdateSource();
        }
        Activate();
    }
}