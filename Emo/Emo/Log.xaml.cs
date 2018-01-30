using System;
using System.Collections.ObjectModel;
using SQLite;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Emo
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Log : ContentPage
    {
        private ObservableCollection<SavedEmotion> _Items;
        private SQLiteAsyncConnection _connection;

        public Log()
        {
            InitializeComponent();
            _connection = DependencyService.Get<ISQLiteDb>().GetConnection();
            
        }

        protected override async void OnAppearing()
        {
            var savedEmotions = await _connection.Table<SavedEmotion>().ToListAsync();
            _Items = new ObservableCollection<SavedEmotion>(savedEmotions);
            LogListView.ItemsSource = _Items;

            base.OnAppearing();
        }

        private async void Btn_Delete(object sender, EventArgs e)
        {
            var log = _Items[0];
            await _connection.DeleteAsync(log);
            _Items.Remove(log);
        }
    }
}
