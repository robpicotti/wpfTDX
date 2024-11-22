using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows.Input;
using System.Windows;

namespace wpfTDX
{
    public class WebServiceData
    {
        private string _url = "http://localhost:5001/";
        public WebServiceData()
        {

        }
        public async Task<string> GetFundsDataASync()
        {
            string fundsurl =  _url + "get_funds";


            string jsonResponse = "";
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.PostAsync(fundsurl, null);
                response.EnsureSuccessStatusCode(); // Ensures that the response was successful

                jsonResponse = await response.Content.ReadAsStringAsync();
            }
            return jsonResponse;
        }
    }
}
