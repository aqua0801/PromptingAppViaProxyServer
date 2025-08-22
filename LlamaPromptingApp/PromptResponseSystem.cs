using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LlamaPromptingApp
{
    public static class PromptHistoryJsonHelper
    {
        public static JToken ToJToken(ObservableCollection<PromptResponseSystem.PromptResponse> history , DateTime dt)
        {
            JArray arr = new JArray();
            arr.Add(dt.ToString("yyyy-MM-dd HH:mm:ss"));
            foreach (var item in history)
            {
                JObject obj = new JObject
                {
                    ["Prompt"] = item.Prompt,
                    ["Response"] = item.Response
                };
                arr.Add(obj);
            }
            return arr;
        }

        public static DateTime ToDT(JToken token)
        {
            DateTime createdDataTime = DateTime.MinValue;
            if (token is JArray arr && arr.Count > 0)
            {
                createdDataTime = DateTime.TryParse(arr[0]?.ToString(), out var dt) ? dt : DateTime.MinValue;
            }
            return createdDataTime;
        }


        public static (DateTime SavedAt, ObservableCollection<PromptResponseSystem.PromptResponse> History) FromJToken(JToken token)
        {
            var result = new ObservableCollection<PromptResponseSystem.PromptResponse>();
            DateTime createdDataTime = DateTime.MinValue;

            if (token is JArray arr && arr.Count > 0)
            {
                // first element is datetime
                createdDataTime = DateTime.TryParse(arr[0]?.ToString(), out var dt) ? dt : DateTime.MinValue;

                // rest are history items
                foreach (var obj in arr.Skip(1))
                {
                    if (obj is JObject jobj)
                    {
                        result.Add(new PromptResponseSystem.PromptResponse
                        {
                            Prompt = jobj.Value<string>("Prompt") ?? string.Empty,
                            Response = jobj.Value<string>("Response") ?? string.Empty
                        });
                    }
                }
            }

            return (createdDataTime, result);
        }
    }
    public class PromptResponseSystem
    {
        public record PromptResponse : INotifyPropertyChanged
        {
            private string prompt = "";
            public string Prompt
            {
                get => prompt;
                set
                {
                    if (prompt != value)
                    {
                        prompt = value;
                        this.OnPropertyChanged();
                    }
                }
            }

            private string response = "";
            public string Response
            {
                get => response;
                set
                {
                    if (response != value)
                    {
                        response = value;
                        this.OnPropertyChanged();
                    }
                }
            }

            private bool isDeleted = false;
            public bool IsDeleted
            {
                get => isDeleted;
                set { if (isDeleted != value) { isDeleted = value; OnPropertyChanged(); } }
            }
            public string GetMergedPrompt()
            {
                return $"User : {this.Prompt} , Response : {this.Response}";
            }

            public int GetTotalLength()
            {
                return this.GetMergedPrompt().Length;
            }

            public event PropertyChangedEventHandler PropertyChanged;
            void OnPropertyChanged([CallerMemberName] string name = null)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        private const int _maxToken = 1000, _maxPromptLength = _maxToken * 4;
        private const string _protocol = "tcp";

        private string _filename = null;
        private DateTime _createDateTime;
        private Stopwatch _sw = new Stopwatch();
        private ObservableCollection<PromptResponse> _promptResponse = new ObservableCollection<PromptResponse>();

        public Action<string, TimeSpan> OnConnectionStatusUpdate;

        public PromptResponseSystem()
        {
            _createDateTime = DateTime.Now;
        }

        public void BindCollectionView(CollectionView clv)
        {
            clv.ItemsSource = this._promptResponse;
        }

        public async Task<string> SendPromptAsync(string newPrompt, string url, int port, int timeoutMs = 100000)
        {
            try
            {
                using var requester = new RequestSocket();
                requester.Connect($"{PromptResponseSystem._protocol}://{url}:{port}");

                string prompt = this.GetMergedPrompt(newPrompt);
                this._sw.Restart();
                requester.SendFrame(prompt);

                var receiveTask = Task.Run(() => requester.ReceiveFrameString());
                if (await Task.WhenAny(receiveTask, Task.Delay(timeoutMs)) == receiveTask)
                {
                    string response = await receiveTask;
                    this.OnConnectionStatusUpdate?.Invoke("Success",TimeSpan.FromMilliseconds(this._sw.ElapsedMilliseconds));
                    this.StorePromptContext(newPrompt, response);
                    return response;
                }
                else
                {
                    throw new TimeoutException();
                }
            }
            catch (TimeoutException)
            {
                this.OnConnectionStatusUpdate?.Invoke("Timeout", TimeSpan.FromMilliseconds(-1));
                return $"Unable to receive response within {timeoutMs} ms !";
            }
            catch (NetMQ.NetMQException ex)
            {
                this.OnConnectionStatusUpdate?.Invoke("Fail to connect", TimeSpan.FromMilliseconds(-1));
                return $"Fail to connect to proxy server !{Environment.NewLine}Exception message : {ex.Message}";
            }
            catch (Exception ex)
            {
                this.OnConnectionStatusUpdate?.Invoke("Unknow error", TimeSpan.FromMilliseconds(-1));
                return $"Encounter unknown issue !{Environment.NewLine}Exception message : {ex.Message}";
            }
            finally
            {
                this._sw.Stop();
            }
        }


        private string GetMergedPrompt(string newPrompt)
        {
            StringBuilder promptBuilder = new StringBuilder();

            if(this._promptResponse.Count > 0)
            {
                promptBuilder.AppendLine("Chat History : ");

                foreach (var prompt in this._promptResponse)
                    promptBuilder.AppendLine(prompt.GetMergedPrompt());

                promptBuilder.AppendLine("New prompt : ");
            }

            promptBuilder.AppendLine(newPrompt);

            return promptBuilder.ToString();
        }

        private void StorePromptContext(string prompt, string response)
        {
            this._promptResponse.Add
                (
                    new PromptResponse { Prompt = prompt, Response = response }
                );

            int totalLength = this._promptResponse.Sum(prompt => prompt.GetTotalLength());

            while (totalLength > PromptResponseSystem._maxPromptLength)
            {
                totalLength -= this._promptResponse.First().GetTotalLength();
                this._promptResponse.RemoveAt(0);
            }
        }

        public void OnDelete(PromptResponse pr)
        {
            this._promptResponse.Remove(pr);
        }

        public void OnSave()
        {
            if (String.IsNullOrEmpty(this._filename))
                return;
            this.OnSaveAs(this._filename);
        }

        public void OnSaveAs(string filename)
        {
            this._filename = filename;
            var jobj = PromptHistoryJsonHelper.ToJToken(this._promptResponse,this._createDateTime);
            //overwriting
            Json.Write(jobj, this._filename);
        }

        public void OnLoad(ObservableCollection<PromptResponseSystem.PromptResponse> pr , DateTime dt , string fn)
        {
            this._createDateTime = dt;
            this._promptResponse = pr;
            this._filename = fn;
        }


    }
}
