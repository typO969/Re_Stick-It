using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

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
            var hex = NoteColors.Hex[ColorKey];
            return (SolidColorBrush)(new BrushConverter().ConvertFromString(hex)!);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

}
