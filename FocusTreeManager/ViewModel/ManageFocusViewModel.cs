using System;
using System.Windows;
using System.Windows.Input;
using FocusTreeManager.Model;
using FocusTreeManager.Model.TabModels;
using FocusTreeManager.Views;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace FocusTreeManager.ViewModel;

public sealed class ManageFocusViewModel : ViewModelBase
{
    private FocusModel _focus;

    public FocusModel Focus
    {
        get => _focus;
        private set
        {
            _focus = value;
            RaisePropertyChanged(() => Focus);
        }
    }

    public FocusGridModel sender { get; set; }

    public RelayCommand FocusCommand { get; private set; }

    public RelayCommand ChangeImageCommand { get; private set; }

    public RelayCommand EditScriptCommand { get; private set; }

    /// <summary>
    /// Initializes a new instance of the EditFocusViewModel class.
    /// </summary>
    public ManageFocusViewModel()
    {
        FocusCommand = new RelayCommand(CloseEditFocus);
        ChangeImageCommand = new RelayCommand(ChangeImage);
        EditScriptCommand = new RelayCommand(EditScript);
        Messenger.Default.Register<NotificationMessage>(this, NotificationMessageReceived);
    }

    public ManageFocusViewModel SetupFlyout(object sender, ModeType Mode)
    {
        if (Mode == ModeType.Create)
        {
            Point mousePos = Mouse.GetPosition((IInputElement)sender);
            FocusModel localFocus = new FocusModel();
            FocusGridModel firstOrDefault = new ViewModelLocator().Main.SelectedTab as FocusGridModel;
            if (firstOrDefault != null)
            {
                localFocus.setDefaults(firstOrDefault.FociList.Count);
                Focus = localFocus;
                //minus 0.4 because if you hit the border of a cell, it will add it to the next one... Annoying
                Focus.X = (int)Math.Floor(mousePos.X / 89 - 0.4);
                Focus.Y = (int)Math.Floor(mousePos.Y / 140);
            }
        }
        else
        {
            Focus = (FocusModel)sender;
        }
        RaisePropertyChanged(() => Focus);
        return this;
    }

    public void CloseEditFocus()
    {
        Messenger.Default.Send(
            new NotificationMessage(this, new ViewModelLocator().Main.SelectedTab, "CloseEditFocus")
        );
    }

    private ImagePickerView? _changeImageView;

    public void ChangeImage()
    {
        if (_changeImageView is null)
        {
            _changeImageView = App.Current.Services.GetRequiredService<ImagePickerView>();
            _changeImageView!.ViewModel.LoadImages(ChangeImageViewModel.FolderType.Focus, Focus.Image);
        }

        _changeImageView.ShowDialog();
        Focus.Image = _changeImageView.ViewModel.FocusImage;
    }

    public void EditScript()
    {
        ScripterViewModel ViewModel = new ViewModelLocator().Scripter;
        ViewModel.ScriptType = ScripterType.FocusTree;
        ViewModel.ManagedScript = _focus.InternalScript;
        EditScript dialog = new EditScript();
        dialog.ShowDialog();
        _focus.InternalScript = ViewModel.ManagedScript;
    }

    private static void NotificationMessageReceived(NotificationMessage msg) { }
}
