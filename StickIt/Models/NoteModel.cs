using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

using StickIt.Services;

namespace StickIt.Models
{
	public class NoteModel : INotifyPropertyChanged
{
    public NoteProperties Props { get; } = new NoteProperties();

    public string Id => Props.Id;

    public string Title
    {
        get => Props.Title;
        set { if (Props.Title == value) return; Props.Title = value; OnPropertyChanged(); }
    }

    public NoteColors.NoteColor ColorKey
    {
        get => Props.ColorKey;
        set
        {
            if (Props.ColorKey == value) return;
            Props.ColorKey = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PaperBrush));
        }
    }

    public string? SkinId
    {
        get => Props.SkinId;
        set
        {
            if (Props.SkinId == value) return;
            Props.SkinId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PaperBrush));
        }
    }

    public string FontFamily
    {
        get => Props.FontFamily;
        set { if (Props.FontFamily == value) return; Props.FontFamily = value; OnPropertyChanged(); }
    }

    public double FontSize
    {
			get => Props.FontSize;
			set { if (Math.Abs(Props.FontSize - value) < 0.001) return; Props.FontSize = value; OnPropertyChanged(); }
		}

    public System.Windows.Media.Brush PaperBrush
    {
        get
        {
            var skin = GetResolvedSkin();
            var imageBrush = TryCreatePaperImageBrush(skin?.PaperImagePath);
            if (imageBrush != null)
                return imageBrush;

            var hex = skin?.PaperHex ?? NoteColors.Hex[ColorKey];
            var brush = TryCreateSolidBrush(hex);
            if (brush != null)
                return brush;

            return TryCreateSolidBrush(NoteColors.Hex[ColorKey]) ?? System.Windows.Media.Brushes.Transparent;
        }
    }

    private static System.Windows.Media.Brush? TryCreateSolidBrush(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return null;

        try
        {
            var brush = (System.Windows.Media.Brush)(new System.Windows.Media.BrushConverter().ConvertFromString(hex)!);
            if (brush.CanFreeze)
                brush.Freeze();
            return brush;
        }
        catch
        {
            return null;
        }
    }

    private static System.Windows.Media.Brush? TryCreatePaperImageBrush(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return null;

        try
        {
            string resolvedPath = imagePath;
            if (!Path.IsPathRooted(resolvedPath))
            {
                resolvedPath = Path.GetFullPath(resolvedPath);
            }

            if (!File.Exists(resolvedPath))
                return null;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(resolvedPath, UriKind.Absolute);
            bitmap.EndInit();
            if (bitmap.CanFreeze)
                bitmap.Freeze();

            var imageBrush = new System.Windows.Media.ImageBrush(bitmap)
            {
                Stretch = System.Windows.Media.Stretch.UniformToFill,
                AlignmentX = System.Windows.Media.AlignmentX.Center,
                AlignmentY = System.Windows.Media.AlignmentY.Center
            };

            if (imageBrush.CanFreeze)
                imageBrush.Freeze();

            return imageBrush;
        }
        catch
        {
            return null;
        }
    }

    public NoteSkin? GetResolvedSkin()
    {
        if (System.Windows.Application.Current is not StickIt.App app)
            return null;

        return app.Skins.ResolveOrNull(SkinId);
    }

    public void RefreshAppearanceBindings()
    {
        OnPropertyChanged(nameof(ColorKey));
        OnPropertyChanged(nameof(SkinId));
        OnPropertyChanged(nameof(PaperBrush));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

}
