using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows.Media;
using FocusTreeManager.Helper;

namespace FocusTreeManager.AsyncImageLoader;

public class AsyncImageLoader
{
    private readonly BackgroundWorker _starterWorker;

    private readonly BackgroundWorker _modWorker;

    private bool _restartStarterWorker;

    private static readonly Lock MyLock = new();

    private static AsyncImageLoader _backWorkerSingleton = new();

    public Dictionary<string, ImageSource> Focuses { get; set; }

    public Dictionary<string, ImageSource> Events { get; set; }

    private AsyncImageLoader()
    {
        _starterWorker = new BackgroundWorker();
        _starterWorker.WorkerSupportsCancellation = true;
        _starterWorker.DoWork += StarterWorker_DoWork;
        _starterWorker.RunWorkerCompleted += StarterWorker_RunWorkerCompleted;
        _modWorker = new BackgroundWorker();
        _modWorker.DoWork += ModWorker_DoWork;
    }

    private void StarterWorker_RunWorkerCompleted(
        object sender,
        RunWorkerCompletedEventArgs runWorkerCompletedEventArgs
    )
    {
        if (_restartStarterWorker)
        {
            _restartStarterWorker = false;
            StartTheJob();
        }
    }

    private void StarterWorker_DoWork(object sender, DoWorkEventArgs e)
    {
        Focuses = ImageHelper.FindAllGameImages(ImageType.Goal);
        Events = ImageHelper.FindAllGameImages(ImageType.Event);
    }

    private void ModWorker_DoWork(object sender, DoWorkEventArgs e)
    {
        foreach (var images in ImageHelper.RefreshFromMods(ImageType.Goal))
        {
            Focuses[images.Key] = images.Value;
        }
        foreach (var images in ImageHelper.RefreshFromMods(ImageType.Event))
        {
            Events[images.Key] = images.Value;
        }
    }

    public void RestartStarterWorker()
    {
        if (!_starterWorker.IsBusy)
        {
            StartTheJob();
            return;
        }

        _restartStarterWorker = true;
        _starterWorker.CancelAsync();
    }

    public void StartTheJob()
    {
        _starterWorker.RunWorkerAsync();
    }

    public void RefreshFromMods()
    {
        ImageHelper.ClearImageGcHandle();
        _modWorker.RunWorkerAsync();
    }

    public static AsyncImageLoader Worker
    {
        get
        {
            lock (MyLock)
            {
                _backWorkerSingleton ??= new AsyncImageLoader();
            }
            return _backWorkerSingleton;
        }
    }
}