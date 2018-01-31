using System;
using System.Linq;
using Plugin.Media;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Plugin.Media.Abstractions;
using Newtonsoft.Json;
using SQLite;
using System.Collections.ObjectModel;

namespace Emo
{
    public class SavedEmotion
    {
        [PrimaryKey, AutoIncrement]
        public long Date { get; set; }
        [MaxLength(255)]
        public string Emotion { get; set; }
    }

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class EmotionPage : ContentPage
    {
        static string ApiKey = "8a8748c42dcc4309951bdaa072bbe7f6";
        public string imagePath;
        private SQLiteAsyncConnection _connection;
        public string highestEmotion = null;


        public EmotionPage()
        {
            InitializeComponent();
            _connection = DependencyService.Get<ISQLiteDb>().GetConnection();
        }


        private async void Btn_save (object sender, EventArgs e)
        {
            if (highestEmotion == null)
            {
                await DisplayAlert("Sorry", "Get Emotion Results first", "ok");
            }
            else
            {
                await _connection.CreateTableAsync<SavedEmotion>();
                var savedEmotions = await _connection.Table<SavedEmotion>().ToListAsync();
                var savedEmotion = new SavedEmotion { Date = DateTime.Now.Ticks, Emotion = highestEmotion };
                await _connection.InsertAsync(savedEmotion);
                await DisplayAlert("Done", "Results Saved", "ok");
            }
        }


        private async void Btn_log (object sender, EventArgs e)
        {
            await Navigation.PushAsync(new Log());
        }



        private async void Btn_selectImage(object sender, EventArgs e)
        {
            var action = await DisplayActionSheet("Select Image Options", "Cancel", null, "Select From Gallary", "Take Picture");
            await CrossMedia.Current.Initialize();


            switch (action)
            {
                case "Select From Gallary":

                    if (!CrossMedia.Current.IsPickPhotoSupported)
                    {
                        await DisplayAlert("Sorry", "Accessing Gallery is not supported", "ok");
                        return;
                    }

                    var file = await CrossMedia.Current.PickPhotoAsync();

                    if (file == null)
                        return;

                    imagePath = file.Path;
                    SelectedImage.Source = ImageSource.FromStream(() =>
                    {
                        var stream = file.GetStream();
                        file.Dispose();
                        return stream;

                    });
                    break;


                case "Take Picture":

                    await DisplayAlert("Sorry", "This feature will be added to Emo in next version", "ok");

                    if (!CrossMedia.Current.IsCameraAvailable
                                    || !CrossMedia.Current.IsTakePhotoSupported)
                    {
                        await DisplayAlert("Sorry", "No Camera available", "ok");
                        return;
                    }

                    var file2 = await CrossMedia.Current.TakePhotoAsync(
                        new StoreCameraMediaOptions
                        {
                            SaveToAlbum = true,
                            Directory = "Emo",

                        });
                    if (file2 == null)
                        return;

                    imagePath = file2.Path;
                    SelectedImage.Source = ImageSource.FromStream(() =>
                    {
                        var stream = file2.GetStream();
                        file2.Dispose();
                        return stream;

                    });

                    break;

                default:
                    break;
            }
        }

        static byte[] GetImageAsByteArray(string imageFilePath)
        {
            FileStream fileStream = new FileStream(imageFilePath, FileMode.Open, FileAccess.Read);
            BinaryReader binaryReader = new BinaryReader(fileStream);
            return binaryReader.ReadBytes((int)fileStream.Length);
        }

        private async void Btn_GetEmotion(object sender, EventArgs e)
        {
            if (SelectedImage.Source == null)
            {
                await DisplayAlert("Alert", "Please Select an Image First ", "ok");
                return;
            }
            else
            {

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", ApiKey);
                string uri = "https://westus.api.cognitive.microsoft.com/emotion/v1.0/recognize?";
                HttpResponseMessage response;
                string responseContent;

                byte[] byteData = GetImageAsByteArray(imagePath);

                using (var content = new ByteArrayContent(byteData))
                {

                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    response = await client.PostAsync(uri, content);
                    responseContent = response.Content.ReadAsStringAsync().Result;
                }

                JToken rootToken = JArray.Parse(responseContent).First;
                JToken scoresToken = rootToken.Last;

                JEnumerable<JToken> scoreList = scoresToken.Children();

                var emotions = JsonConvert.DeserializeObject<Emotion[]>(responseContent);
                var scores = emotions[0].scores;
                var highestScore = scores.Values.OrderByDescending(score => score).First();
                highestEmotion = scores.Keys.First(key => scores[key] == highestScore);

                foreach (var score in scoreList)
                {
                    EmotionResults.Text = "Detected Emotion: " + highestEmotion
                     +'\n' +"\n Full Analysis:\n"+ score.ToString();
                }


            }
        }
    }
}