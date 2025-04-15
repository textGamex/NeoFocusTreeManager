using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FocusTreeManager.ViewModel;
using NLog;
using Pfim;

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
        var imageSources = new Dictionary<string, ImageSource>();
        string rightFolder = source switch
        {
            ImageType.Goal => GfxGoalFolder,
            ImageType.Event => GfxEventFolder,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };
        var locator = new ViewModelLocator();
        //既有可能是 MainViewModel，也有可能是 ProjectEditorViewModel
        var model = locator.Main.Project;
        model ??= locator.ProjectEditor.Project;
        if (model?.ListModFolders == null)
        {
            return imageSources;
        }

        foreach (string modPath in model.ListModFolders)
        {
            string fullPath = Path.Combine(modPath, rightFolder);
            foreach (var image in GetImages(fullPath))
            {
                imageSources[image.Key] = image.Value;
            }
        }

        return imageSources;
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
                    ? GetImageSource(filePath)
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

    private sealed class Allocator : IImageAllocator
    {
        public byte[] Rent(int size)
        {
            return ArrayPool<byte>.Shared.Rent(size);
        }

        public void Return(byte[] data)
        {
            ArrayPool<byte>.Shared.Return(data);
        }
    }

    private static readonly PfimConfig Config = new(32768, TargetFormat.Native, true, new Allocator());

    private static BitmapSource GetImageSource(string filePath)
    {
        using var image = Pfimage.FromFile(filePath, Config);

        var bsource = BitmapSource.Create(
            image.Width,
            image.Height,
            96.0,
            96.0,
            PixelFormat(image),
            null,
            image.Data,
            image.Stride
        );

        return bsource;
    }

    private static PixelFormat PixelFormat(IImage image)
    {
        return image.Format switch
        {
            ImageFormat.Rgb24 => PixelFormats.Bgr24,
            ImageFormat.Rgba32 => PixelFormats.Bgra32,
            ImageFormat.Rgb8 => PixelFormats.Gray8,
            ImageFormat.R5g5b5a1 or ImageFormat.R5g5b5 => PixelFormats.Bgr555,
            ImageFormat.R5g6b5 => PixelFormats.Bgr565,
            _ => throw new Exception($"Unable to convert {image.Format} to WPF PixelFormat")
        };
    }
}
