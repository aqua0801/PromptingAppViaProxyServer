using CommunityToolkit.Maui.Views;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LlamaPromptingApp
{
    public class LoadLogPopup : Popup
    {
        public class SaveFileEntry
        {
            public string FileName { get; set; } = string.Empty;
            public DateTime CreatedDateTime { get; set; }
            public string Path { get; set; } = string.Empty;
        }

        private readonly StackLayout _stack;

        public LoadLogPopup(double parentWidth ,double parentHeight , Action<string> onLoad, Action<string> onDelete)
        {
            this.CanBeDismissedByTappingOutsideOfPopup = true;

            _stack = new StackLayout
            {
                Spacing = 12,
                Padding = new Thickness(16),
                VerticalOptions = LayoutOptions.Start
            };

            var scroll = new ScrollView
            {
                Content = _stack
            };

            this.Content = new Border
            {
                Stroke = Colors.Gray,
                StrokeThickness = 1,
                BackgroundColor = Colors.LightBlue,
                Padding = 12,
                WidthRequest = parentWidth * 0.9,
                HeightRequest = parentHeight * 0.7,
                Content = scroll
            };

            LoadFiles(onLoad, onDelete);
        }

        private void LoadFiles(Action<string> onLoad, Action<string> onDelete)
        {
            string root = FileSystem.AppDataDirectory;
            var files = Directory.GetFiles(root, "*.json");

            var entries = new List<SaveFileEntry>();

            foreach (var file in files)
            {
                try
                {
                    var text = File.ReadAllText(file);
                    var token = JToken.Parse(text);

                    var (created, _) = PromptHistoryJsonHelper.FromJToken(token);

                    entries.Add(new SaveFileEntry
                    {
                        FileName = Path.GetFileNameWithoutExtension(file),
                        Path = file,
                        CreatedDateTime = created
                    });
                }
                catch { /* ignore bad files */ }
            }

            // latest first
            foreach (var entry in entries.OrderByDescending(e => e.CreatedDateTime))
            {
                var row = new Grid
                {
                    ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                    Padding = new Thickness(0, 6)
                };

                // datetime
                var dtLabel = new Label
                {
                    Text = entry.CreatedDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    FontSize = 12,
                    VerticalOptions = LayoutOptions.Center
                };

                // filename (click → load)
                var fileBtn = new Button
                {
                    Text = entry.FileName,
                    FontSize = 14,
                    BackgroundColor = Colors.Transparent,
                    TextColor = Colors.Black
                };
                fileBtn.Clicked += (_, __) =>
                {
                    onLoad(entry.Path);
                    this.Close();
                };

                // delete button
                var delBtn = new Button
                {
                    Text = "Delete",
                    FontSize = 12,
                    BackgroundColor = Colors.Red,
                    TextColor = Colors.White
                };
                delBtn.Clicked += (_, __) =>
                {
                    File.Delete(entry.Path);
                    onDelete(entry.Path);
                    _stack.Children.Remove(row);
                };

                row.Add(dtLabel, 0, 0);
                row.Add(fileBtn, 1, 0);
                row.Add(delBtn, 2, 0);

                _stack.Children.Add(row);
            }
        }


    }
}
