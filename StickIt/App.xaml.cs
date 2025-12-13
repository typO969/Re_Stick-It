using System.Linq;
using System.Windows;
using StickIt.Services;

namespace StickIt
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var notes = NoteStore.LoadAll().ToList();
            if (notes.Count == 0)
                new NoteWindow(NoteStore.CreateNew()).Show();
            else
                foreach (var n in notes) new NoteWindow(n).Show();
        }
    }
}
