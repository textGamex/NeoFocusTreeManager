using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FocusTreeManager.ViewModel;
using Imaging.DDSReader;
using NLog;

namespace FocusTreeManager.Helper;

public enum ImageType : byte
{
    Goal,
    Event
}

public static class ImageHelper
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static readonly string[] ArrayFileName =
    [
        "goal_support_democracy",
        "goal_support_fascism",
        "goal_support_communism",
        "goal_generic_occupy_states_coastal",
        "goal_generic_occupy_start_war",
        "goal_generic_occupy_states_ongoing_war",
        "goal_generic_build_navy"
    ];

    private static readonly string[] ArrayAssociatedTypo =
    [
        "goal_generic_support_democracy",
        "goal_generic_support_fascism",
        "goal_generic_support_communism",
        "goal_generic_occypy_states_coastal",
        "goal_generic_occypy_start_war",
        "goal_generic_occypy_states_ongoing_war",
        "goal_generic_build_nay"
    ];

    private const string GfxGoalFolder = @"gfx\interface\goals\";

    private const string GfxEventFolder = @"gfx\event_pictures";

    private const string GfxErrorImage = @"GFX\Focus\goal_unknown.png";

    private static readonly string[] ImageDoNotLoad = ["shine_mask", "shine_overlay"];

    private static readonly ImageSource ErrorSource = new BitmapImage(
        new Uri(Path.Combine(Environment.CurrentDirectory, GfxErrorImage))
    );

    public static ImageSource GetImageFromGame(string imageName, ImageType source)
    {
        //Try to obtain the image
        try
        {
            var value = source switch
            {
                ImageType.Goal
                    => AsyncImageLoader
                        .AsyncImageLoader.Worker.Focuses.LastOrDefault(f => f.Key == imageName)
                        .Value,
                ImageType.Event
                    => AsyncImageLoader
                        .AsyncImageLoader.Worker.Events.LastOrDefault(f => f.Key == imageName)
                        .Value,
                _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
            };
            //Make sure the value is set, if not, return the error image.
            return value ?? ErrorSource;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load image");
            //If an error occurred, return the error image
            return ErrorSource;
        }
    }

    public static Dictionary<string, ImageSource> FindAllGameImages(ImageType source)
    {
        string rightFolder = source switch
        {
            ImageType.Goal => GfxGoalFolder,
            ImageType.Event => GfxEventFolder,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };

        string fullPath = Path.Combine(Configurator.GetGamePath(), rightFolder);
        return GetImages(fullPath);
    }

    public static Dictionary<string, ImageSource> RefreshFromMods(ImageType source)
    {
        var list = new Dictionary<string, ImageSource>();
        string rightFolder = source switch
        {
            ImageType.Goal => GfxGoalFolder,
            ImageType.Event => GfxEventFolder,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };
        //For each file in add mod folders
        var model = new ViewModelLocator().Main.Project;
        if (model?.ListModFolders == null)
        {
            return list;
        }

        foreach (string modPath in model.ListModFolders)
        {
            string fullPath = Path.Combine(modPath, rightFolder);
            foreach (var image in GetImages(fullPath))
            {
                list[image.Key] = image.Value;
            }
        }

        return list;
    }

    private static Dictionary<string, ImageSource> GetImages(string path)
    {
        var map = new Dictionary<string, ImageSource>();
        if (!Directory.Exists(path))
        {
            return map;
        }

        foreach (string filePath in Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly))
        {
            var extensionName = Path.GetExtension(filePath.AsSpan());
            if (
                !extensionName.Equals(".dds", StringComparison.OrdinalIgnoreCase)
                && !extensionName.Equals(".png", StringComparison.OrdinalIgnoreCase)
            )
            {
                continue;
            }

            if (ImageDoNotLoad.Any(filePath.Contains))
            {
                continue;
            }

            try
            {
                string imageName = Path.GetFileNameWithoutExtension(filePath);
                //try to replace potential broken links because of typos in the file names.
                imageName =
                    Array.IndexOf(ArrayAssociatedTypo, imageName) != -1
                        ? ArrayFileName[Array.IndexOf(ArrayAssociatedTypo, imageName)]
                        : imageName;
                var result = extensionName.Equals(".dds", StringComparison.OrdinalIgnoreCase)
                    ? ImageSourceForBitmap(DDS.LoadImage(filePath))
                    : new BitmapImage(new Uri(filePath));

                result.Freeze();
                map[imageName] = result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "加载图片异常");
            }
        }

        return map;
    }

    [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject([In] IntPtr hObject);

    private static ImageSource ImageSourceForBitmap(Bitmap bmp)
    {
        IntPtr handle = bmp.GetHbitmap();
        try
        {
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                handle,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions()
            );
        }
        finally
        {
            DeleteObject(handle);
        }
    }
}
