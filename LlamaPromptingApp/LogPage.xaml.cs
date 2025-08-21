using CommunityToolkit.Maui.Views;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace LlamaPromptingApp;

public partial class LogPage : ContentPage
{
    private Action<PromptResponseSystem.PromptResponse> OnDelete;
    private Action OnSave;
    private Action<string> OnSaveAs;
    private Action<ObservableCollection<PromptResponseSystem.PromptResponse>,DateTime,string> OnLoad;
    public LogPage(PromptResponseSystem prs)
	{
		InitializeComponent();
        prs.BindCollectionView(this.LogsCollection);
        this.OnDelete = (pr) => prs.OnDelete(pr);
        this.OnSave = () => prs.OnSave();
        this.OnLoad = (pr,dt,fn) => prs.OnLoad(pr,dt,fn);
        this.OnSaveAs = (fn) => prs.OnSaveAs(fn);
	}

    private async void OnLogSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is PromptResponseSystem.PromptResponse pr)
        {
            var popup = new EditPopup(pr,this.Width);
            var result = await this.ShowPopupAsync(popup);

            if (pr.IsDeleted)
                this.OnDelete(pr);
        }
        this.LogsCollection.SelectedItem = null;
    }

    private void OnSaveClicked(object sender , EventArgs e)
	{
        this.OnSave();
	}

    private void OnSaveAsClicked(object sender, EventArgs e)
    {
        string filename = this.entrySaveName.Text;
        this.OnSaveAs(filename);
    }
    private async void OnLoadClicked(object sender, EventArgs e)
    {
        var popup = new LoadLogPopup(
                this.Width,
                this.Height,
                onLoad: (path) =>
                {
                    var token = Json.Read(path);
                    var (created, history) = PromptHistoryJsonHelper.FromJToken(token);
                    this.OnLoad(history,created,Path.GetFileName(path));
                    // replace your history
                    this.LogsCollection.ItemsSource = history;
                },
                onDelete: (path) =>
                {
                    // optional clean-up action
                });

        await this.ShowPopupAsync(popup);
    }
    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

}