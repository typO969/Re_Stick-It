using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

using StickIt.Services;

namespace StickIt.Models
{
	public class NoteModel : INotifyPropertyChanged
	{
		public Guid Id { get; set; } = Guid.NewGuid();

		private string _title = "Untitled";
		public string Title
		{
			get => _title;	
			set { _title = value; OnPropertyChanged(); }
		}

		private NoteColors.NoteColor _colorKey = NoteColors.NoteColor.ThreeMYellow;
		public NoteColors.NoteColor ColorKey
		{
			get => _colorKey;
			set
			{
				_colorKey = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(PaperBrush));
			}
		}

		public System.Windows.Media.Brush PaperBrush
		{
			get
			{
				var hex = NoteColors.Hex[ColorKey];
				return (System.Windows.Media.SolidColorBrush)(new BrushConverter().ConvertFromString(hex)!);
			}
		}

		public event PropertyChangedEventHandler? PropertyChanged;
		private void OnPropertyChanged([CallerMemberName] string? name = null) =>
			 PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}
