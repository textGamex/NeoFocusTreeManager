using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;

namespace FocusTreeManager.ViewModel;

/// <summary>
/// This class contains properties that a View can data bind to.
/// <para>
/// See http://www.galasoft.ch/mvvm
/// </para>
/// </summary>
public sealed class ChangeImageViewModel : ViewModelBase
{
    public sealed class ImageData
    {
        public ImageSource Image { get; set; }
        public string Filename { get; set; }
        public int MaxWidth { get; set; }
    }

    public RelayCommand<string> SelectCommand { get; private set; }

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
        ImageList = new ObservableCollection<ImageData>();
        SelectCommand = new RelayCommand<string>(SelectFocus);
    }

    public void LoadImages(string SubFolder, string CurrentImage)
    {
        int maxWidth;
        Dictionary<string, ImageSource> images;
        switch (SubFolder)
        {
            case "Focus":
                maxWidth = 100;
                images = AsyncImageLoader.AsyncImageLoader.Worker.Focuses;
                break;
            case "Event":
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
        _focusImage = CurrentImage;
        RaisePropertyChanged(() => ImageList);
    }

    public void SelectFocus(string path)
    {
        FocusImage = path;
        Messenger.Default.Send(new NotificationMessage(this, "HideChangeImage"));
    }
}