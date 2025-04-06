using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;

namespace FocusTreeManager.ViewModel;

/// <summary>
/// This class contains properties that a View can data bind to.
/// <para>
/// See http://www.galasoft.ch/mvvm
/// </para>
/// </summary>
public sealed partial class ChangeImageViewModel : ViewModelBase
{
    public enum FolderType : byte
    {
        Focus,
        Event
    }

    public sealed class ImageData
    {
        public ImageSource Image { get; set; }
        public string Filename { get; set; }
        public int MaxWidth { get; set; }
    }

    private string _focusImage;

    public string FocusImage
    {
        get => _focusImage;
        private set
        {
            _focusImage = value;
            RaisePropertyChanged(() => FocusImage);
        }
    }

    public ObservableCollection<ImageData> ImageList { get; }

    /// <summary>
    /// Initializes a new instance of the ChangeImageViewModel class.
    /// </summary>
    public ChangeImageViewModel()
    {
        ImageList = [];
    }

    public void LoadImages(FolderType folderType, string currentImage)
    {
        int maxWidth;
        Dictionary<string, ImageSource> images;
        switch (folderType)
        {
            case FolderType.Focus:
                maxWidth = 100;
                images = AsyncImageLoader.AsyncImageLoader.Worker.Focuses;
                break;
            case FolderType.Event:
                maxWidth = 250;
                images = AsyncImageLoader.AsyncImageLoader.Worker.Events;
                break;
            default:
                throw new System.ArgumentException("Invalid SubFolder");
        }
        ImageList.Clear();
        foreach (var source in images)
        {
            ImageList.Add(
                new ImageData
                {
                    Image = source.Value,
                    Filename = source.Key,
                    MaxWidth = maxWidth
                }
            );
        }
        _focusImage = currentImage;
        RaisePropertyChanged(() => ImageList);
    }

    [RelayCommand]
    private void SelectFocus(string path)
    {
        FocusImage = path;
        Messenger.Default.Send(new NotificationMessage(this, "HideChangeImage"));
    }
}
