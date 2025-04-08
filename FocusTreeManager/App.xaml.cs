using System;
using System.Runtime.ExceptionServices;
using System.Windows;
using FocusTreeManager.ViewModel;
using FocusTreeManager.Views;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace FocusTreeManager;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public App()
    {
        AsyncImageLoader.AsyncImageLoader.Worker.StartTheJob();
        //Logging
        var currentDomain = AppDomain.CurrentDomain;
        currentDomain.FirstChanceException += HandleFirstChance;
        currentDomain.UnhandledException += HandleCrashes;
    }

    public static new App Current => (App)Application.Current;
    public IServiceProvider Services { get; } = ConfigureServices();

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddTransient<ImagePickerView>();
        services.AddTransient<ChangeImageViewModel>();
        services.AddTransient<AboutView>();

        return services.BuildServiceProvider();
    }

    private static void HandleFirstChance(object? source, FirstChanceExceptionEventArgs e)
    {
        Log.Error(e.Exception);
    }

    private static void HandleCrashes(object sender, UnhandledExceptionEventArgs e)
    {
        Log.Error(e.ExceptionObject);
    }
}
