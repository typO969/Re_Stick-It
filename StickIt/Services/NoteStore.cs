using System.Collections.Generic;
using StickIt.Models;

namespace StickIt.Services
{
    public static class NoteStore
    {
        private static readonly List<NoteModel> _notes = new();

        public static IEnumerable<NoteModel> LoadAll() => _notes;

        public static NoteModel CreateNew()
        {
            var n = new NoteModel();
            _notes.Add(n);
            return n;
        }
    }
}
