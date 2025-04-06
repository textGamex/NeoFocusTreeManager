using System;
using System.Runtime.ExceptionServices;
using System.Windows;
using FocusTreeManager.Helper;
using FocusTreeManager.ViewModel;
using FocusTreeManager.Views;
using Microsoft.Extensions.DependencyInjection;

namespace FocusTreeManager;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    //Log only if not in debug
#if DEBUG
    public bool toLog = false;
#else
    public bool toLog = true;
#endif

    public App()
    {
        AsyncImageLoader.AsyncImageLoader.Worker.StartTheJob();
        //Logging
        AppDomain currentDomain = AppDomain.CurrentDomain;
        currentDomain.FirstChanceException += HandleFirstChance;
        currentDomain.UnhandledException += HandleCrashes;
    }

    public static new App Current => (App)Application.Current;
    public IServiceProvider Services { get; } = ConfigureServices();

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddTransient<ImagePickerView>();
        services.AddTransient<ChangeImageViewModel>();
        
        return services.BuildServiceProvider();
    }

    private void HandleFirstChance(object source, FirstChanceExceptionEventArgs e)
    {
        if (!toLog)
            return;
        LoggingHelper.LogException(e.Exception);
    }

    private void HandleCrashes(object sender, UnhandledExceptionEventArgs e)
    {
        if (!toLog)
            return;
        LoggingHelper.LogCrash((Exception)e.ExceptionObject);
    }
}
