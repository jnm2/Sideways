﻿namespace Sideways
{
    using System;
    using System.Collections.Immutable;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Text.Json;
    using System.Windows;
    using Microsoft.Win32;

    public sealed class BookmarksFile : INotifyPropertyChanged
    {
        public static readonly string Directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Sideways", "Bookmarks");

        private FileInfo? file;
        private ImmutableList<Bookmark> bookmarks;

        public BookmarksFile(FileInfo? file, ImmutableList<Bookmark> bookmarks)
        {
            this.file = file;
            this.bookmarks = bookmarks;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Name => this.file is { FullName: { } fullName } ? Path.GetFileNameWithoutExtension(fullName) : "Not saved";

        public ImmutableList<Bookmark> Bookmarks
        {
            get => this.bookmarks;
            private set
            {
                if (ReferenceEquals(value, this.bookmarks))
                {
                    return;
                }

                this.bookmarks = value;
                this.OnPropertyChanged();
            }
        }

        public void Save()
        {
            if (!System.IO.Directory.Exists(Directory))
            {
                System.IO.Directory.CreateDirectory(Directory);
            }

            if (File() is { FullName: { } fileName })
            {
                System.IO.File.WriteAllText(fileName, JsonSerializer.Serialize(this.bookmarks));
            }

            FileInfo? File()
            {
                if (this.file is { } file)
                {
                    return file;
                }

                var dialog = new SaveFileDialog
                {
                    InitialDirectory = Directory,
                    FileName = $"Bookmarks {DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}",
                    DefaultExt = ".bookmarks",
                    Filter = "Bookmark files|*.bookmarks",
                };

                if (dialog.ShowDialog() is true)
                {
                    this.file = new FileInfo(dialog.FileName);
                    this.OnPropertyChanged(nameof(this.Name));
                    return new(dialog.FileName);
                }

                return null;
            }
        }

        public bool IsDirty()
        {
            if (this.file is null)
            {
                return true;
            }

            return this.file is { FullName: { } fileName } &&
                   JsonSerializer.Serialize(this.bookmarks, new JsonSerializerOptions { WriteIndented = true }) != File.ReadAllText(fileName);
        }

        public void AskSave()
        {
            if (this.IsDirty() &&
                ShowMessageBox("Do you want to save bookmarks?", "Save", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                this.Save();
            }
        }

        private static MessageBoxResult ShowMessageBox(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage messageBoxImage = MessageBoxImage.None)
        {
            if (Application.Current.MainWindow is { } window)
            {
                return MessageBox.Show(window, messageBoxText, caption, button, messageBoxImage);
            }

            return MessageBox.Show(messageBoxText, caption, button, messageBoxImage);
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
