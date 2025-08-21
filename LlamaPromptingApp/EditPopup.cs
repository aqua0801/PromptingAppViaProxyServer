using CommunityToolkit.Maui.Views;

namespace LlamaPromptingApp
{
    public class EditPopup : Popup
    {
        public PromptResponseSystem.PromptResponse Log { get; private set; }

        public EditPopup(PromptResponseSystem.PromptResponse log , double parentWidth)
        {
            Log = log;

            // Popup content
            var entryPrompt = new Entry { Text = Log.Prompt, Placeholder = "Prompt" , TextColor = Colors.Black};
            entryPrompt.TextChanged += (s, e) => this.Log.Prompt = e.NewTextValue;

            var entryResponse = new Entry { Text = Log.Response, Placeholder = "Response", TextColor = Colors.Black };
            entryResponse.TextChanged += (s, e) => Log.Response = e.NewTextValue;

            Content = new Frame
            {
                Padding = 20,
                BackgroundColor = Colors.White,
                CornerRadius = 10,
                HasShadow = true,
                WidthRequest = parentWidth * 0.9,
                Content = new VerticalStackLayout
                {
                    Spacing = 10,
                    Children =
                {
                    entryPrompt,
                    entryResponse
                    ,
                    new Button
                    {
                        Text = "Delete",
                        BackgroundColor = Colors.Red,
                        TextColor = Colors.White,
                        Command = new Command(() => {
                            Log.IsDeleted = true;
                            Close(Log);
                        })
                    },
                    new Button
                    {
                        Text = "Close",
                        Command = new Command(() => Close(Log))
                    }
                }
                }
            };
        }
    }
}
