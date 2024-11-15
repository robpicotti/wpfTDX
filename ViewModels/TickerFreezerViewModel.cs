using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace wpfTDX
{
    public class TickerFreezerViewModel :INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        private ObservableCollection<TickerFreezerDataModel> _tickerfreezer { get; set; }
        public ObservableCollection<TickerFreezerDataModel> TickerFreezer
        {
            get => _tickerfreezer;
            set
            {
                if (_tickerfreezer != value)
                {
                    _tickerfreezer = value;
                    OnPropertyChanged(nameof(TickerFreezer));
                }
            }
        }
        public TickerFreezerViewModel()
        {
            
        }
        public async Task Refresh()
        {
            await ProcessTickerFreezer();
        }
    
        private async Task<string> GetTickerFreezer()
        {
            string url = "http://localhost:5001/get_unresolved_tickerfreezer";
            string jsonResponse = "";
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.PostAsync(url, null);
                response.EnsureSuccessStatusCode(); // Ensures that the response was successful

                jsonResponse = await response.Content.ReadAsStringAsync();
            }
            return jsonResponse;
        }
        private async Task ProcessTickerFreezer()
        {
            string jsonResponse = await GetTickerFreezer();
            try
            {
                // Parse the response as a JArray since it's a list of dictionaries
                JArray tickerfreezer = JArray.Parse(jsonResponse);

                // Deserialize the list of dictionaries directly into your DataModel
                var tickerfreezerList = JsonConvert.DeserializeObject<List<TickerFreezerDataModel>>(tickerfreezer.ToString());

                if (TickerFreezer == null)
                {
                    TickerFreezer = new ObservableCollection<TickerFreezerDataModel>();
                }
                else
                {
                    TickerFreezer.Clear();
                }

                // Add each job to the ObservableCollection
                foreach (var item in tickerfreezerList)
                {
                    TickerFreezer.Add(item);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
