using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;

using StickIt.Models;
using StickIt.Services;
using StickIt.Sticky.Services;

using WpfApplication = System.Windows.Application;

namespace StickIt
{
   public partial class NoteWindow : Window, INotifyPropertyChanged
   {
      private readonly NoteModel? _note;

      private int _noteStuckMode = 0;
      private double? _stickyOffsetXPx;
      private double? _stickyOffsetYPx;

      private readonly DispatcherTimer _followTimer = new DispatcherTimer();
      private double? _lastTargetX;
      private double? _lastTargetY;

      private StickIt.Sticky.Services.StickyWinEventHookService? _winEventHook;
      private bool _winEventSubscribed;

      private DispatcherTimer? _stickyCoalesceTimer;
      private bool _stickySnapPending;

      private IntPtr _prevWin32Owner = IntPtr.Zero;

      private static double PointsToDip(double pt) => pt * 96.0 / 72.0; // pt * 4/3
      private static double DipToPoints(double dip) => dip * 72.0 / 96.0;

      private bool _suppressTextChanged;



      public event PropertyChangedEventHandler? PropertyChanged;
      private void OnPropertyChanged([CallerMemberName] string? name = null) =>
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

      public string NoteId { get; private set; } = Guid.NewGuid().ToString("N");
      public NoteColors.NoteColor GetColorKey() => _note?.ColorKey ?? NoteColors.NoteColor.ThreeMYellow;
      public string GetTitle() => _note?.Title ?? "Untitled";

      private System.Windows.Point GetNoteTopLeftPx()
      {
         return this.PointToScreen(new System.Windows.Point(0, 0));
      }

      public int StuckMode
      {
         get => _noteStuckMode;
         private set
         {
            if (_noteStuckMode == value) return;
            _noteStuckMode = value;
            OnPropertyChanged();
         }
      }

      public void SetFontFamily(string family)
      {
         if (_note != null)
            _note.FontFamily = string.IsNullOrWhiteSpace(family) ? "Helvetica" : family;
      }
      public void SetFontSize(double size)
      {
         if (_note != null)
            _note.FontSize = size <= 0 ? 14.0 : size;
      }
      public string GetFontFamily() => _note?.FontFamily ?? "Helvetica";
      public double GetFontSize() => _note?.FontSize ?? 14.0;

      public void SetTitle(string title)
      {
         if (_note != null)
            _note.Title = title ?? "Untitled";
      }

      public static readonly RoutedUICommand CmdBold =
   new RoutedUICommand("Bold", "CmdBold", typeof(NoteWindow),
      new InputGestureCollection { new KeyGesture(Key.B, ModifierKeys.Control) });

      public static readonly RoutedUICommand CmdItalic =
         new RoutedUICommand("Italic", "CmdItalic", typeof(NoteWindow),
            new InputGestureCollection { new KeyGesture(Key.I, ModifierKeys.Control) });

      public static readonly RoutedUICommand CmdUnderline =
         new RoutedUICommand("Underline", "CmdUnderline", typeof(NoteWindow),
            new InputGestureCollection { new KeyGesture(Key.U, ModifierKeys.Control) });



      private void RequestStickySnap()
      {
         if (_stickyCoalesceTimer == null)
         {
            _stickyCoalesceTimer = new System.Windows.Threading.DispatcherTimer(
               System.Windows.Threading.DispatcherPriority.Background,
               Dispatcher);

            // ~60fps. If you want less CPU, change to 33ms (~30fps).
            _stickyCoalesceTimer.Interval = TimeSpan.FromMilliseconds(16);

            _stickyCoalesceTimer.Tick += (s, e) =>
            {
               _stickyCoalesceTimer.Stop();

               if (!_stickySnapPending) return;
               _stickySnapPending = false;

               if (_noteStuckMode != 2) return;
               if (_stickyTarget == null || _stickyTarget.Hwnd == IntPtr.Zero) return;

               SnapToStickyTargetNow();
            };
         }

         _stickySnapPending = true;

         // restart timer (coalesces multiple requests)
         _stickyCoalesceTimer.Stop();
         _stickyCoalesceTimer.Start();
      }



      public int GetStuckMode() => _noteStuckMode;        // see below
      public void SetStuckMode(int mode) => ApplyStuckMode(mode);

      public bool GetIsMinimized() => this.WindowState == WindowState.Minimized;
      public void SetIsMinimized(bool minimized)
      {
         WindowState = minimized ? WindowState.Minimized : WindowState.Normal;
      }

      public event EventHandler? NoteTextChanged;

      public NoteWindow() : this(new NoteModel())
      {
      }

      public NoteWindow(NoteModel note)
      {
         InitializeComponent();

         Loaded += (_, __) => EnsureStickyTargetOnLoad();

         Loaded += (_, __) =>
         {
            // best-effort: resolves HWND once, no hooks, no timers
            TryRebindStickyTarget();
         };

         _followTimer.Interval = TimeSpan.FromMilliseconds(150);
         _followTimer.Tick += (_, __) =>
         {
            if (_noteStuckMode == 2)
               SnapToStickyTargetNow();
         };
         _followTimer.Start();

         txtNoteContent.SelectionChanged += txtNoteContent_SelectionChanged;

         CommandBindings.Add(new CommandBinding(CmdBold, (_, __) => ToggleBold(), (_, e) => e.CanExecute = true));
         CommandBindings.Add(new CommandBinding(CmdItalic, (_, __) => ToggleItalic(), (_, e) => e.CanExecute = true));
         CommandBindings.Add(new CommandBinding(CmdUnderline, (_, __) => ToggleUnderline(), (_, e) => e.CanExecute = true));



         txtNoteContent.PreviewKeyDown += (s, e) =>
         {
            if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == (ModifierKeys.Control | ModifierKeys.Shift)
                     && e.Key == Key.X)
            {
               ToggleStrikethrough(); // Ctrl+Shift+X
               e.Handled = true;
            }

         };





         UpdateStickyVisuals();


         _note = note;
         DataContext = _note;

         // Keep autosave behavior consistent regardless of constructor use
         txtNoteContent.TextChanged += (_, __) =>
         {
            if (_suppressTextChanged) return;
            NoteTextChanged?.Invoke(this, EventArgs.Empty);
         };


         ControlBar.MouseLeftButtonDown += (_, __) => DragMove();

         KeyDown += (_, e) =>
         {
            // Ctrl+N / Ctrl+W / Ctrl+M
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
               switch (e.Key)
               {
                  case Key.N:
                     AppInstance.CreateNewNoteNear(this);
                     e.Handled = true;
                     return;

                  case Key.W:
                     Close(); // close = delete
                     e.Handled = true;
                     return;

                  case Key.M:
                     WindowState = WindowState.Minimized;
                     AppInstance.QueueSaveFromWindow();
                     e.Handled = true;
                     return;
               }
            }


            if (e.Key == Key.F12)
            {
               new DebugColorsWindow(_note!) { Owner = this }.Show();
               e.Handled = true;
            }

         };
      }

      public void SetNoteId(string id) => NoteId = string.IsNullOrWhiteSpace(id) ? NoteId : id;

      public void SetText(string text)
      {
         WithSuppressedTextChanged(() =>
         {
            txtNoteContent.Document.Blocks.Clear();
            txtNoteContent.Document.Blocks.Add(new Paragraph(new Run(text ?? "")));

            InitializeTypingDefaultsFromNote(); // typing defaults only
         });
      }


      protected override void OnClosed(EventArgs e)
      {
         try { ClearLocalAotOwner(); } catch { }
         base.OnClosed(e);
      }

      private void Menu_ToggleBold(object sender, RoutedEventArgs e) => ToggleBold();
      private void Menu_ToggleItalic(object sender, RoutedEventArgs e) => ToggleItalic();
      private void Menu_ToggleUnderline(object sender, RoutedEventArgs e) => ToggleUnderline();
      private void Menu_ToggleStrikethrough(object sender, RoutedEventArgs e) => ToggleStrikethrough();


      private void GetSelectionFontInfoWpf(out string family, out double sizeDip, out bool bold, out bool italic)
      {
         var sel = txtNoteContent.Selection;

         object ff = sel.GetPropertyValue(TextElement.FontFamilyProperty);
         object fs = sel.GetPropertyValue(TextElement.FontSizeProperty);
         object fw = sel.GetPropertyValue(TextElement.FontWeightProperty);
         object fst = sel.GetPropertyValue(TextElement.FontStyleProperty);

         family = (ff is System.Windows.Media.FontFamily fam) ? fam.Source : (string.IsNullOrWhiteSpace(_note?.FontFamily) ? "Segoe UI" : _note.FontFamily);
         sizeDip = (fs is double d && d > 0) ? d : ((_note?.FontSize ?? 0) > 0 ? _note!.FontSize : 16.0);

         bold = (fw is FontWeight w) && (w == FontWeights.Bold);
         italic = (fst is System.Windows.FontStyle s) && (s == System.Windows.FontStyles.Italic);

      }

      private static TextDecorationCollection GetDecorationsOrEmpty(object v)
      {
         if (v is TextDecorationCollection col)
         {
            // clone so we don't mutate a shared instance
            return new TextDecorationCollection(col.Select(d => d.Clone()));
         }
         return new TextDecorationCollection();
      }

      private static bool HasDecorationLocation(TextDecorationCollection col, TextDecorationLocation loc)
         => col.Any(d => d.Location == loc);

      private static void RemoveDecorationLocation(TextDecorationCollection col, TextDecorationLocation loc)
      {
         for (int i = col.Count - 1; i >= 0; i--)
         {
            if (col[i].Location == loc)
               col.RemoveAt(i);
         }
      }

      private void ApplyFontToSelectionWpf(string familyName, double sizeDip, bool bold, bool italic)
      {
         var sel = txtNoteContent.Selection;
         var range = new TextRange(sel.Start, sel.End); // empty selection => typing style only

         range.ApplyPropertyValue(TextElement.FontFamilyProperty, new System.Windows.Media.FontFamily(familyName));
         range.ApplyPropertyValue(TextElement.FontSizeProperty, sizeDip);
         range.ApplyPropertyValue(TextElement.FontWeightProperty, bold ? FontWeights.Bold : FontWeights.Normal);
         range.ApplyPropertyValue(TextElement.FontStyleProperty, italic ? FontStyles.Italic : FontStyles.Normal);

         // Persist per-note defaults (store DIPs)
         if (_note != null)
         {
            _note.FontFamily = familyName;
            _note.FontSize = sizeDip;
         }

         NoteTextChanged?.Invoke(this, EventArgs.Empty);
      }



      private void WithSuppressedTextChanged(Action action)
      {
         _suppressTextChanged = true;
         try { action(); }
         finally { _suppressTextChanged = false; }
      }

      private static bool IsUnset(object v) => v == DependencyProperty.UnsetValue || v == null;

      private bool GetSelectionBold()
      {
         var v = txtNoteContent.Selection.GetPropertyValue(TextElement.FontWeightProperty);
         return (v is FontWeight w) && (w == FontWeights.Bold);
      }

      private bool GetSelectionItalic()
      {
         var v = txtNoteContent.Selection.GetPropertyValue(TextElement.FontStyleProperty);
         return (v is System.Windows.FontStyle s) && (s == FontStyles.Italic);
      }

      private static bool HasDecoration(object v, TextDecorationLocation loc)
      {
         if (v is not TextDecorationCollection col) return false;
         return col.Any(d => d.Location == loc);
      }


      private void ToggleBold()
      {
         GetSelectionFontInfoWpf(out var family, out var sizeDip, out var bold, out var italic);
         ApplyFontToSelectionWpf(family, sizeDip, !bold, italic);
      }

      private void ToggleItalic()
      {
         GetSelectionFontInfoWpf(out var family, out var sizeDip, out var bold, out var italic);
         ApplyFontToSelectionWpf(family, sizeDip, bold, !italic);
      }

      private void ToggleUnderline()
   => ToggleDecoration(TextDecorationLocation.Underline, TextDecorations.Underline);

      private void ToggleStrikethrough()
         => ToggleDecoration(TextDecorationLocation.Strikethrough, TextDecorations.Strikethrough);


      private static bool HasDecoration(object v, TextDecorationCollection deco)
      {
         if (v == null || v == DependencyProperty.UnsetValue) return false;
         if (v is not TextDecorationCollection col) return false;

         // reference equality works here for the standard sets (Underline / Strikethrough)
         return col == deco;
      }

      private void ToggleDecoration(TextDecorationLocation loc, TextDecorationCollection addSet)
      {
         var sel = txtNoteContent.Selection;

         object v = sel.GetPropertyValue(Inline.TextDecorationsProperty);
         var col = GetDecorationsOrEmpty(v);

         if (HasDecorationLocation(col, loc))
            RemoveDecorationLocation(col, loc);
         else
            foreach (var d in addSet) col.Add(d.Clone());

         var range = new TextRange(sel.Start, sel.End);
         range.ApplyPropertyValue(Inline.TextDecorationsProperty, col.Count == 0 ? null : col);

         NoteTextChanged?.Invoke(this, EventArgs.Empty);
      }




      private void txtNoteContent_SelectionChanged(object sender, RoutedEventArgs e)
      {
         miBold.IsChecked =
            txtNoteContent.Selection.GetPropertyValue(TextElement.FontWeightProperty) is FontWeight w
            && w == FontWeights.Bold;

         miItalic.IsChecked =
            txtNoteContent.Selection.GetPropertyValue(TextElement.FontStyleProperty) is System.Windows.FontStyle s
            && s == System.Windows.FontStyles.Italic;

         var deco = txtNoteContent.Selection.GetPropertyValue(Inline.TextDecorationsProperty);

         miUnderline.IsChecked = HasDecoration(deco, TextDecorationLocation.Underline);

         // if you have a Strikethrough menu item:
         // miStrike.IsChecked = HasDecoration(deco, TextDecorationLocation.Strikethrough);
      }





      public string GetText()
      {
         var range = new TextRange(
            txtNoteContent.Document.ContentStart,
            txtNoteContent.Document.ContentEnd);

         return range.Text.TrimEnd('\r', '\n');
      }

      public void SetColorKey(string keyName)
      {
         if (_note != null && Enum.TryParse(keyName, out NoteColors.NoteColor key))
            _note.ColorKey = key;
      }

      private void ColorMenuItem_Click(object sender, RoutedEventArgs e)
      {
         if (_note == null)
            return;

         if (sender is not MenuItem menuItem)
            return;

         if (menuItem.Tag is string tag && Enum.TryParse(tag, out NoteColors.NoteColor color))
            _note.ColorKey = color;
      }

      private void btnClose_Click(object sender, RoutedEventArgs e)
      {
         Close();
         _followTimer.Stop();

      }

      private void btnMinimize_Click(object sender, RoutedEventArgs e)
      {
         WindowState = WindowState.Minimized;
      }

      private void btnPinCycle_Click(object sender, RoutedEventArgs e)
      {
         // Cycle: 0 -> 1 -> 2 -> 0 ...
         var next = (_noteStuckMode + 1) % 3;

         if (next == 2)
         {
            if (StickToWindowUnderMe())
               return;

            var dlg = new StickIt.Sticky.StickyTargetPickerWindow { Owner = this };
            if (dlg.ShowDialog() == true && dlg.SelectedTarget != null)
            {
               EnterStuckMode2WithTarget(dlg.SelectedTarget);
               return;
            }

            var desk = StickIt.Sticky.Services.DesktopTargetService.TryGetDesktopTarget();
            if (desk != null)
            {
               EnterStuckMode2WithTarget(desk);
               return;
            }

            ApplyStuckMode(0);
            _stickyTarget = null;
            _stickyOffsetXPx = null;
            _stickyOffsetYPx = null;
            StopHook();
            AppInstance.QueueSaveFromWindow();
            return;
         }

         ApplyStuckMode(next);
         StopHook();

         if (next == 0)
         {
            _stickyTarget = null;
            _stickyOffsetXPx = null;
            _stickyOffsetYPx = null;
         }

         AppInstance.QueueSaveFromWindow();
      }

      private void ApplyFontToSelection(string familyName, double sizePt, bool bold, bool italic)
      {
         var rtb = txtNoteContent;
         var sel = rtb.Selection;

         // Selection empty => applies to caret typing style (does NOT reformat whole note)
         TextRange range = new TextRange(sel.Start, sel.End);

         double sizeDip = PointsToDip(sizePt);

         range.ApplyPropertyValue(TextElement.FontFamilyProperty, new System.Windows.Media.FontFamily(familyName));
         range.ApplyPropertyValue(TextElement.FontSizeProperty, sizePt);
         range.ApplyPropertyValue(TextElement.FontWeightProperty, bold ? FontWeights.Bold : FontWeights.Normal);
         range.ApplyPropertyValue(TextElement.FontStyleProperty, italic ? FontStyles.Italic : FontStyles.Normal);

         // Persist per-note defaults
         _note!.FontFamily = familyName;
         _note!.FontSize = sizePt;

         NoteTextChanged?.Invoke(this, EventArgs.Empty);
      }

      private void InitializeTypingDefaultsFromNote()
      {

         if (_note == null) return;

         // fallbacks
         string familyName = string.IsNullOrWhiteSpace(_note.FontFamily) ? "Segoe UI" : _note.FontFamily;
         double sizePt = (_note.FontSize > 0) ? _note.FontSize : 12.0;

         var rtb = txtNoteContent;

         // IMPORTANT: apply to an *empty* range at the caret/document start
         // so existing content is not reformatted.
         TextPointer start = rtb.CaretPosition ?? rtb.Document.ContentStart;
         if (start == null) start = rtb.Document.ContentStart;

         // ensure we're at an insertion position
         start = start.GetInsertionPosition(LogicalDirection.Forward) ?? rtb.Document.ContentStart;

         var range = new TextRange(start, start); // empty range == typing style only
         range.ApplyPropertyValue(TextElement.FontFamilyProperty, new System.Windows.Media.FontFamily(familyName));
         range.ApplyPropertyValue(TextElement.FontSizeProperty, sizePt);

      }









      public string? GetRtf()
      {
         try
         {
            var range = new TextRange(
                txtNoteContent.Document.ContentStart,
                txtNoteContent.Document.ContentEnd);

            using var ms = new MemoryStream();
            range.Save(ms, System.Windows.DataFormats.Rtf);

            // Avoid writing meaningless empty RTF blobs
            if (ms.Length == 0)
               return null;

            return Encoding.UTF8.GetString(ms.ToArray());
         }
         catch
         {
            return null;
         }
      }

      public void SetRtf(string? rtf)
      {
         WithSuppressedTextChanged(() =>
         {
            txtNoteContent.Document.Blocks.Clear();

            if (string.IsNullOrWhiteSpace(rtf))
            {
               InitializeTypingDefaultsFromNote(); // still set defaults for new typing
               return;
            }

            var bytes = Encoding.UTF8.GetBytes(rtf);

            using var ms = new MemoryStream(bytes);
            var range = new TextRange(txtNoteContent.Document.ContentStart, txtNoteContent.Document.ContentEnd);

            try
            {
               range.Load(ms, System.Windows.DataFormats.Rtf);
            }
            catch
            {
               txtNoteContent.Document.Blocks.Clear();
            }

            InitializeTypingDefaultsFromNote(); // typing defaults only
         });
      }



      private void txtNoteContent_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
      {
         e.Handled = true;
         if (this.Content is FrameworkElement fe && fe.ContextMenu != null)
            fe.ContextMenu.IsOpen = true;
      }



      public DateTime GetCreatedUtc() => _note?.Props.CreatedUtc ?? default;

      private void TouchModifiedUtc()
      {
         if (_note != null)
            _note.Props.ModifiedUtc = DateTime.UtcNow;
      }

      private App AppInstance => (App)System.Windows.Application.Current;

      private void Menu_NewNote(object sender, RoutedEventArgs e)
      {
         AppInstance.CreateNewNoteNear(this);
      }

      private void Menu_Color(object sender, RoutedEventArgs e)
      {
         if (sender is not MenuItem mi || mi.Tag is not string keyName) return;

         if (Enum.TryParse(keyName, out NoteColors.NoteColor key))
         {
            if (_note != null)
               _note.ColorKey = key;
            AppInstance.QueueSaveFromWindow(); // we’ll add this tiny helper in App
         }
      }

      private void Menu_Minimize(object sender, RoutedEventArgs e)
      {
         SetIsMinimized(true);
         AppInstance.QueueSaveFromWindow();
      }

      private void Menu_Restore(object sender, RoutedEventArgs e)
      {
         SetIsMinimized(false);
         AppInstance.QueueSaveFromWindow();
      }

      private void Menu_Debug(object sender, RoutedEventArgs e)
      {
         new DebugColorsWindow(_note!) { Owner = this }.Show();
      }

      private void Menu_Delete(object sender, RoutedEventArgs e)
      {
         Close(); // close = delete per your rule
         StopHook();
      }

      private void Menu_Exit(object sender, RoutedEventArgs e)
      {
         AppInstance.ShutdownRequested();
      }


      private void Menu_Cut(object sender, RoutedEventArgs e) => txtNoteContent.Cut();
      private void Menu_Copy(object sender, RoutedEventArgs e) => txtNoteContent.Copy();
      private void Menu_Paste(object sender, RoutedEventArgs e) => txtNoteContent.Paste();

      private void Menu_SaveNow(object sender, RoutedEventArgs e)
      {
         AppInstance.QueueSaveFromWindow(); // triggers debounce
      }

      private void Menu_MinimizeAll(object sender, RoutedEventArgs e)
      {
         AppInstance.MinimizeAllNotes();
      }

      private void Menu_RestoreAll(object sender, RoutedEventArgs e)
      {
         AppInstance.RestoreHiddenNotes();
      }

      // Placeholders (disabled now; handler exists in case you enable later)
      private void Menu_FontSettings(object sender, RoutedEventArgs e)
      {
         var cur = GetSelectionFontInfo(); // this still returns System.Drawing.Font (points)

         using (var dlg = new System.Windows.Forms.FontDialog())
         {
            dlg.ShowColor = false;
            dlg.ShowEffects = false;
            dlg.Font = cur;

            if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
               return;

            System.Drawing.Font f = dlg.Font;

            ApplyFontToSelectionWpf(
               f.FontFamily.Name,
               PointsToDip(f.SizeInPoints),   // convert once here
               f.Bold,
               f.Italic);
         }
      }


      private void Menu_LoadNotes(object sender, RoutedEventArgs e) { }
      private void Menu_Preferences(object sender, RoutedEventArgs e) { }
      private void Menu_NoteManager(object sender, RoutedEventArgs e) { }

      private System.Drawing.Font GetSelectionFontInfo()
      {
         var sel = txtNoteContent.Selection;

         object ff = sel.GetPropertyValue(TextElement.FontFamilyProperty);
         object fs = sel.GetPropertyValue(TextElement.FontSizeProperty);
         object fw = sel.GetPropertyValue(TextElement.FontWeightProperty);
         object fst = sel.GetPropertyValue(TextElement.FontStyleProperty);

         string family = 
            (ff is System.Windows.Media.FontFamily fam) ? fam.Source :
            (!string.IsNullOrWhiteSpace(_note?.FontFamily) ? _note.FontFamily : "Segoe UI");

         // WPF size is DIPs; may be UnsetValue on mixed selection
         double sizeDip =
            (fs is double d && d > 0) ? d :
            (_note?.FontSize > 0 ? _note.FontSize : 16.0);

         float sizePt = (float)DipToPoints(sizeDip);

         bool bold = (fw is FontWeight w) && (w == FontWeights.Bold);
         bool italic =   (fst is System.Windows.FontStyle s) && (s == System.Windows.FontStyles.Italic);

         var style = System.Drawing.FontStyle.Regular;
         if (bold) style |= System.Drawing.FontStyle.Bold;
         if (italic) style |= System.Drawing.FontStyle.Italic;

         try
         {
            return new System.Drawing.Font(family, sizePt, style);
         }
         catch
         {
            // absolute fallback if a font name is invalid on this machine
            return new System.Drawing.Font("Segoe UI", sizePt, style);
         }
      }




      private void txtNoteTitle_select(object sender, MouseButtonEventArgs e)
      {
         // Ensure we have a note and the TextBox exists
         if (_note == null || txtNoteTitle == null) return;

         // Clear only if the current title is still the default
         // Prefer checking the TextBox to reflect the UI state; fallback to model if needed
         var isDefaultTitle =
            string.Equals(txtNoteTitle.Text, "Untitled", StringComparison.Ordinal) ||
            string.Equals(_note.Title, "Untitled", StringComparison.Ordinal);

         if (!isDefaultTitle) return;

         // Clear the title and focus the TextBox
         txtNoteTitle.Clear();
         txtNoteTitle.Focus();

         // Update the model in case binding isn't immediate
         _note.Title = string.Empty;

         // Mark as handled so the click doesn't re-trigger other handlers
         e.Handled = true;
      }

      private void NoteColors_SubmenuOpened(object sender, RoutedEventArgs e)
      {
         if (_note == null) return;

         var current = _note.ColorKey;

         foreach (var item in miNoteColors.Items.OfType<MenuItem>())
         {
            if (item.Tag is not string tag) continue;
            if (!Enum.TryParse(tag, out NoteColors.NoteColor key)) continue;

            var isCurrent = (key == current);

            item.IsEnabled = !isCurrent;

            // optional, but nice UX:
            item.IsCheckable = true;
            item.IsChecked = isCurrent;
         }
      }

      private void Menu_StickToWindow_Picker(object sender, RoutedEventArgs e)
      {
         var dlg = new StickIt.Sticky.StickyTargetPickerWindow { Owner = this };
         if (dlg.ShowDialog() != true || dlg.SelectedTarget == null) return;

         EnterStuckMode2WithTarget(dlg.SelectedTarget);
      }

      public bool SnapToStickyTargetNow()
      {
         if (_noteStuckMode != 2) return false;

         // Ensure we have a live hwnd
         if (_stickyTarget == null || _stickyTarget.Hwnd == IntPtr.Zero)
         {
            if (!TryRebindStickyTarget()) return false;
         }
         if (_stickyTarget == null || _stickyTarget.Hwnd == IntPtr.Zero)
            return false;

         // Target rect (pixels)
         if (!StickIt.Sticky.Services.WindowRectService.TryGetWindowRect(_stickyTarget.Hwnd, out var tr))
            return false;

         // Skip work if target didn’t move (pixel compare)
         if (_lastTargetX.HasValue && _lastTargetY.HasValue &&
            _lastTargetX.Value == tr.X && _lastTargetY.Value == tr.Y)
         {
            return true;
         }

         _lastTargetX = tr.X;
         _lastTargetY = tr.Y;

         // Cache offset once (note-top-left relative to target-top-left, in pixels)
         if (!_stickyOffsetXPx.HasValue || !_stickyOffsetYPx.HasValue)
         {
            var notePx = GetNoteTopLeftPx();
            _stickyOffsetXPx = notePx.X - tr.X;
            _stickyOffsetYPx = notePx.Y - tr.Y;
         }

         int newX = (int)(tr.X + (int)Math.Round(_stickyOffsetXPx.Value));
         int newY = (int)(tr.Y + (int)Math.Round(_stickyOffsetYPx.Value));

         // Clamp to virtual desktop so it can’t “disappear”
         int minX = (int)SystemParameters.VirtualScreenLeft;
         int minY = (int)SystemParameters.VirtualScreenTop;
         int maxX = (int)(SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - this.Width);
         int maxY = (int)(SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - this.Height);

         if (newX < minX) newX = minX;
         if (newY < minY) newY = minY;
         if (newX > maxX) newX = maxX;
         if (newY > maxY) newY = maxY;

         var myHwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;

         // IMPORTANT: insert-after = target hwnd, so we stay above it without being Topmost
         WindowMoveService.MoveWindow(myHwnd, newX, newY, _stickyTarget.Hwnd);


         return true;
      }




      #region LOCAL_AOT (Owned window)

      private void ApplyLocalAotOwner(IntPtr targetHwnd)
      {
         var myHwnd = new WindowInteropHelper(this).Handle;
         if (myHwnd == IntPtr.Zero || targetHwnd == IntPtr.Zero) return;

         // save previous owner once per "enter mode 2"
         if (_prevWin32Owner == IntPtr.Zero)
            _prevWin32Owner = StickIt.Services.LocalAotOwnerService.GetOwner(myHwnd);

         StickIt.Services.LocalAotOwnerService.SetOwner(myHwnd, targetHwnd);
      }

      private void ClearLocalAotOwner()
      {
         var myHwnd = new WindowInteropHelper(this).Handle;
         if (myHwnd == IntPtr.Zero) return;

         // restore previous owner (usually zero)
         StickIt.Services.LocalAotOwnerService.SetOwner(myHwnd, _prevWin32Owner);
         _prevWin32Owner = IntPtr.Zero;
      }

      #endregion




      private StickIt.Sticky.StickyTargetInfo? _stickyTarget;

      public void SetStickyTarget(StickIt.Persistence.StickyTargetPersist? p)
      {
         if (p == null)
         {
            _stickyTarget = null;
            _stickyOffsetXPx = null;
            _stickyOffsetYPx = null;
            return;
         }

         _stickyTarget = new StickIt.Sticky.StickyTargetInfo
         {
            Hwnd = IntPtr.Zero,
            ProcessId = p.ProcessId,
            ProcessName = p.ProcessName,
            WindowTitle = p.WindowTitle,
            ClassName = p.ClassName,
            CapturedUtc = p.CapturedUtc
         };

         _stickyOffsetXPx = p.OffsetX;
         _stickyOffsetYPx = p.OffsetY;
      }


      public StickIt.Persistence.StickyTargetPersist? GetStickyTargetPersist()
      {
         if (_stickyTarget == null) return null;

         return new StickIt.Persistence.StickyTargetPersist
         {
            ProcessId = _stickyTarget.ProcessId,
            ProcessName = _stickyTarget.ProcessName,
            WindowTitle = _stickyTarget.WindowTitle,
            ClassName = _stickyTarget.ClassName,
            CapturedUtc = _stickyTarget.CapturedUtc,
            OffsetX = _stickyOffsetXPx,
            OffsetY = _stickyOffsetYPx
         };
      }

      public void ClearStickyTarget()
      {
         StuckMode = 0;
         Topmost = false;
         _stickyTarget = null;
         _stickyOffsetXPx = null;
         _stickyOffsetYPx = null;
      }



      public bool TryRebindStickyTarget()
      {
         // Only meaningful in mode 2
         if (_noteStuckMode != 2) return false;

         // Nothing to rebind
         if (_stickyTarget == null) return false;

         // Already bound
         if (_stickyTarget.Hwnd != IntPtr.Zero) return true;

         // Build a persist-hints object from what we know (PID first, then title/class)
         var p = new StickIt.Persistence.StickyTargetPersist
         {
            ProcessId = _stickyTarget.ProcessId,
            ProcessName = _stickyTarget.ProcessName,
            WindowTitle = _stickyTarget.WindowTitle,
            ClassName = _stickyTarget.ClassName,
            CapturedUtc = _stickyTarget.CapturedUtc,

            // offsets are irrelevant to rebind, but keep them if present
            OffsetX = _stickyOffsetXPx,
            OffsetY = _stickyOffsetYPx
         };

         var resolved = StickIt.Sticky.StickyTargetResolver.TryResolve(p);
         if (resolved == null || resolved.Hwnd == IntPtr.Zero)
            return false;

         _stickyTarget = resolved;

         // Force next snap to actually move (don’t short-circuit on cached target coords)
         _lastTargetX = null;
         _lastTargetY = null;

         // Ensure hook is running for follow (once we add it fully, this becomes important)
         EnsureHookForStickyTarget();

         return true;
      }


      public bool StickToWindowUnderMe()
      {
         var target = TryGetTargetWindowUnderNote();
         if (target == null)
            return false;

         EnterStuckMode2WithTarget(target);
         return true;
      }




      private void EnterStuckMode2WithTarget(StickIt.Sticky.StickyTargetInfo target)
      {
         _stickyTarget = target;

         // LOCAL AOT: make note owned by target (stays above target, not globally TopMost)
         ApplyLocalAotOwner(target.Hwnd);

         StuckMode = 2;
         Topmost = false;

         _stickyOffsetXPx = null;
         _stickyOffsetYPx = null;
         _lastTargetX = null;
         _lastTargetY = null;

         EnsureHookForStickyTarget();
         UpdateStickyVisuals();

         // ONE-SHOT snap so user sees it stick immediately and z-order is corrected.
         var ok = SnapToStickyTargetNow();

         // TEMP: set the menu item header so you can see success without breakpoints
         if (miSticky_SnapNow != null)
            miSticky_SnapNow.Header = ok ? "Snap to target now (OK)" : "Snap to target now (FAILED)";

         AppInstance.QueueSaveFromWindow();
      }






      private void Sticky_Auto_Click(object sender, RoutedEventArgs e)
      {
         // Get a target by hit test (no side effects)
         var t = TryGetTargetWindowUnderNote(); // (we’ll implement/fix next)
         if (t == null)
         {
            // Optional: fallback to picker if auto fails
            Sticky_Pick_Click(sender, e);
            return;
         }

         EnterStuckMode2WithTarget(t);
      }
      private void Sticky_Pick_Click(object sender, RoutedEventArgs e)
      {
         var dlg = new StickIt.Sticky.StickyTargetPickerWindow { Owner = this };
         if (dlg.ShowDialog() == true && dlg.SelectedTarget != null)
         {
            EnterStuckMode2WithTarget(dlg.SelectedTarget);
         }
      }
      private StickIt.Sticky.StickyTargetInfo? TryGetTargetWindowUnderNote()
      {
         // Pick a point inside the note (client area), expressed in SCREEN PIXELS.
         // Using PointToScreen keeps us consistent with your pixel-only sticky math.
         var pt = PointToScreen(new System.Windows.Point(24, 48)); // small inset from top-left
         int x = (int)Math.Round(pt.X);
         int y = (int)Math.Round(pt.Y);

         var myHwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;

         return StickIt.Sticky.Services.StickyHitTestService.GetTopmostWindowUnderPoint(
            x, y,
            System.Diagnostics.Process.GetCurrentProcess().Id,
            myHwnd);
      }






      private void ApplyStuckMode(int mode)
      {
         if (mode < 0 || mode > 2) mode = 0;

         // if we are leaving mode 2, undo local AOT owner
         if (_noteStuckMode == 2 && mode != 2)
            ClearLocalAotOwner();

         StuckMode = mode;
         Topmost = (mode == 1);
         UpdateStickyVisuals();
      }

      private void Sticky_NotStuck_Click(object sender, RoutedEventArgs e)
      {
         ApplyStuckMode(0);
         _stickyTarget = null;
         _stickyOffsetXPx = null;
         _stickyOffsetYPx = null;
         AppInstance.QueueSaveFromWindow();
      }

      private void Sticky_AOT_Click(object sender, RoutedEventArgs e)
      {
         ApplyStuckMode(1);
         // Leaving mode 2 -> clear target/offset
         _stickyTarget = null;
         _stickyOffsetXPx = null;
         _stickyOffsetYPx = null;
         AppInstance.QueueSaveFromWindow();
         StopHook();
      }

      public void EnsureStickyTargetOnLoad()
      {
         if (_noteStuckMode != 2)
            return;

         // 1) Try persisted target rebind
         if (TryRebindStickyTarget())
            return;

         // 2) If that fails, try “window under note”
         if (StickToWindowUnderMe())
            return;

         // 3) Desktop fallback
         var desk = StickIt.Sticky.Services.DesktopTargetService.TryGetDesktopTarget();
         if (desk != null)
         {
            EnterStuckMode2WithTarget(desk);
            return;
         }

         // 4) Last resort: mode 0
         ApplyStuckMode(0);
         _stickyTarget = null;
         _stickyOffsetXPx = null;
         _stickyOffsetYPx = null;
         StopHook();
         AppInstance.QueueSaveFromWindow();
      }


      private void Sticky_SnapNow_Click(object sender, RoutedEventArgs e)
      {
         var ok = SnapToStickyTargetNow();
         miSticky_SnapNow.Header = ok ? "Snap to target now (OK)" : "Snap to target now (FAILED)";
         if (ok) AppInstance.QueueSaveFromWindow();
      }



      private void Sticky_SubmenuOpened(object sender, RoutedEventArgs e)
      {
         // Checkmarks
         miSticky_NotStuck.IsCheckable = true;
         miSticky_AOT.IsCheckable = true;
         miSticky_Auto.IsCheckable = true;
         miSticky_Pick.IsCheckable = true;

         miSticky_Auto.IsChecked = (_noteStuckMode == 2);
         miSticky_Pick.IsChecked = false; // picker is an action, not a state
         miSticky_NotStuck.IsChecked = (_noteStuckMode == 0);  
         miSticky_AOT.IsChecked = (_noteStuckMode == 1);

         // When stuck-to-window, make “Not stuck” read as “Un-stick note”
         miSticky_NotStuck.Header = (_noteStuckMode == 2) ? "Un-stick note" : "Not stuck";

         // Optional: disable AOT checkbox while in mode 2? (your call)
         // I’d leave it enabled; it becomes an explicit mode switch.
         miSticky_SnapNow.IsEnabled = (_noteStuckMode == 2);

      }
      private void UpdateStickyVisuals()
      {
         if (NoteChrome == null) return;

         // NOTE: do NOT touch NoteChrome.BorderBrush (it’s bound to ColorKey pipeline)

         switch (_noteStuckMode)
         {
            case 1: // AOT
               NoteChrome.BorderThickness = new Thickness(6);
               NoteChrome.Effect = new DropShadowEffect { BlurRadius = 24, ShadowDepth = 3, Opacity = 0.45 };
               if (StickyAccent != null) StickyAccent.BorderBrush = System.Windows.Media.Brushes.Transparent;
               break;

            case 2: // Stuck
               NoteChrome.BorderThickness = new Thickness(12);
               NoteChrome.Effect = new DropShadowEffect { BlurRadius = 34, ShadowDepth = 0, Opacity = 0.45 };
               if (StickyAccent != null)
               {
                  StickyAccent.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(140, 255, 200, 80));
                  StickyAccent.BorderThickness = new Thickness(6);
               }
               break;

            default: // Normal
               NoteChrome.BorderThickness = new Thickness(2);
               NoteChrome.Effect = new DropShadowEffect { BlurRadius = 14, ShadowDepth = 2, Opacity = 0.25 };
               if (StickyAccent != null) StickyAccent.BorderBrush = System.Windows.Media.Brushes.Transparent;
               break;
         }
      }



      private void EnsureHookForStickyTarget()
      {
         // Only needed in Mode 2 and only if we have a target
         if (_noteStuckMode != 2) { StopHook(); return; }
         if (_stickyTarget == null) { StopHook(); return; }

         if (_winEventHook == null)
            _winEventHook = new StickIt.Sticky.Services.StickyWinEventHookService();

         if (!_winEventSubscribed)
         {
            _winEventHook.TargetMoved += WinEvent_TargetMoved;
            _winEventSubscribed = true;
         }

         _winEventHook.Hook();
      }

      private void WinEvent_TargetMoved(IntPtr hwnd)
      {
         if (_noteStuckMode != 2) return;
         if (_stickyTarget == null) return;
         if (_stickyTarget.Hwnd == IntPtr.Zero) return;

         // Must marshal to UI thread
         Dispatcher.BeginInvoke(new Action(() =>
         {
            if (_noteStuckMode != 2) return;
            if (_stickyTarget == null) return;
            if (_stickyTarget.Hwnd == IntPtr.Zero) return;

            // Foreground/reorder events can arrive with hwnd != target.
            // For sticky mode 2, always re-assert our position just above the target.
            if (hwnd == _stickyTarget.Hwnd)
               RequestStickySnap();
            else
               SnapToStickyTargetNow();
         }));
      }

      private void StopHook()
      {
         if (_winEventHook == null) return;

         if (_winEventSubscribed)
         {
            _winEventHook.TargetMoved -= WinEvent_TargetMoved;
            _winEventSubscribed = false;
         }

         _winEventHook.Dispose();
         _winEventHook = null;
      }




      // !!!!  -------------  NOTE PROPERTIES TEMP START ---------- !!!!!!!!
      private void NoteMenu_Opened(object sender, RoutedEventArgs e)
      {
         // ID
         if (_note == null)
            return;
         miProp_Id.Header = $"Note ID: {_note.Props.Id}";

         // Contains: chars, words, lines
         var text = GetText();
         var chars = text.Length;
         var words = CountWords(text);
         var lines = CountLines(text);
         miProp_Contains.Header = $"Contains: {chars} chars, {words} words, {lines} lines";

         // Timestamps (UTC; you can format later)
         miProp_Created.Header = $"Created: {_note.Props.CreatedUtc:u}";
         miProp_Modified.Header = $"Modified: {_note.Props.ModifiedUtc:u}";

         // Position
         miProp_Position.Header = $"Position: X={Left:0}, Y={Top:0}";

         // Color
         miProp_Color.Header = $"Color: {_note.ColorKey}";

         // Sticky
         miProp_Sticky.Header = $"Sticky: {StickyLabel(GetStuckMode())}";

         // Font
         miProp_Font.Header = $"Font: {_note.FontFamily}, {_note.FontSize:0.#} pt";
      }

      private static int CountWords(string s)
      {
         if (string.IsNullOrWhiteSpace(s)) return 0;
         return s.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
      }

      private static int CountLines(string s)
      {
         if (string.IsNullOrEmpty(s)) return 0;
         return s.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).Length;
      }

      private static string StickyLabel(int mode) => mode switch
      {
         0 => "Not stuck",
         1 => "Always on top",
         2 => "Stick to application (future)",
         _ => $"Unknown ({mode})"
      };
      // !!!!!!!!!!! ----------- NOTE PROPERTIES TEMP END ---------- !!!!!!!!!!!



      // +++++++++ +++++++++++++ BEGIN TEMP STICKY TARGET DEBUGGING ++++++++++++++++++++
      private void Menu_RebindStickyTarget(object sender, RoutedEventArgs e)
      {
         var ok = TryRebindStickyTarget();
         // Optional: you can show a tiny MessageBox if you want.
         // If you do, keep it subtle (or just do nothing).
         if (ok) AppInstance.QueueSaveFromWindow();
      }

      // +++++++++ +++++++++++++ END TEMP STICKY TARGET DEBUGGING ++++++++++++++++++++

   }
}
