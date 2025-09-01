using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LudusaviRestic
{
    public class BackupSnapshot : INotifyPropertyChanged
    {
        public string Id { get; set; }
        public DateTime Date { get; set; }
        public string ShortId => Id?.Length > 8 ? Id.Substring(0, 8) : Id;
        public List<string> Tags { get; set; } = new List<string>();
        public string TagsDisplay => string.Join(", ", Tags);
        public string GameName => Tags.Count > 0 ? Tags[0] : "Unknown";

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
