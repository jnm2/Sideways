﻿namespace Sideways
{
    using System;
    using System.Collections.Immutable;
    using System.IO;
    using System.Text.Json;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    using Microsoft.Win32;

    public partial class BookmarksView : UserControl
    {
        public BookmarksView()
        {
            this.InitializeComponent();
        }

        private static MessageBoxResult ShowMessageBox(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage messageBoxImage = MessageBoxImage.None)
        {
            if (Application.Current.MainWindow is { } window)
            {
                return MessageBox.Show(window, messageBoxText, caption, button, messageBoxImage);
            }

            return MessageBox.Show(messageBoxText, caption, button, messageBoxImage);
        }

        private void OnNew(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.DataContext is BookmarksViewModel viewModel)
            {
                viewModel.Add(new BookmarksFile(null, ImmutableList<Bookmark>.Empty));
                e.Handled = true;
            }
        }

        private void OnOpen(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.DataContext is BookmarksViewModel viewModel)
            {
                if (!Directory.Exists(BookmarksFile.Directory))
                {
                    Directory.CreateDirectory(BookmarksFile.Directory);
                }

                var dialog = new OpenFileDialog
                {
                    InitialDirectory = BookmarksFile.Directory,
                    Filter = "Bookmark files|*.bookmarks",
                };

                if (dialog.ShowDialog() is true)
                {
                    try
                    {
                        viewModel.Add(
                            new BookmarksFile(
                                new FileInfo(dialog.FileName),
                                JsonSerializer.Deserialize<ImmutableList<Bookmark>>(File.ReadAllText(dialog.FileName)) ?? ImmutableList<Bookmark>.Empty));
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception exception)
#pragma warning restore CA1031 // Do not catch general exception types
                    {
                        _ = ShowMessageBox(exception.Message, "Invalid bookmark file.", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    e.Handled = true;
                }
            }
        }

        private void OnClose(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.DataContext is BookmarksViewModel { SelectedBookmarkFile: { } selected } viewModel)
            {
                selected.AskSave();
                viewModel.Remove(selected);
                e.Handled = true;
            }
        }

        private void OnCanSaveOrClose(object sender, CanExecuteRoutedEventArgs e)
        {
            if (this.DataContext is BookmarksViewModel { SelectedBookmarkFile: { } } )
            {
                e.CanExecute = true;
                e.Handled = true;
            }
        }

        private void OnSave(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.DataContext is BookmarksViewModel { SelectedBookmarkFile: { } bookmarks })
            {
                bookmarks.Save();
                e.Handled = true;
            }
        }
    }
}
