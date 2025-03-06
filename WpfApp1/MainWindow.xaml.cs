using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        HubConnection _connection;
        private string opponent;
        private bool myTurn;
        string myChar = string.Empty;
        private string nickName;

        public string NickName {
            get => nickName;
            set
            {
                nickName = value;
                Signal();
            }
        }

        public string Opponent
        {
            get => opponent;
            set
            {
                opponent = value;
                Signal();
            }
        }
        public bool MyTurn
        {
            get => myTurn;
            set
            {
                myTurn = value;
                Signal();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        void Signal([CallerMemberName] string prop = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        public MainWindow()
        {
            InitializeComponent();

            CreateConnection();
            HubMethods();
            DataContext = this;
        }

        string gameid = string.Empty;

        private void HubMethods()
        {
            _connection.On<string>("Hello", s =>
            {
                Dispatcher.Invoke(() =>
                {
                    var win = new WinSetNick(_connection, s);
                    win.ShowDialog();
                    NickName = win.Nick;
                });
            });
            _connection.On<string, string>("opponent", (s, id) =>
            {
                gameid = id;
                Opponent = s;
            });
            _connection.On<string>("maketurn", s =>
            {
                myChar = s;
                MyTurn = true;
            });
            _connection.On<Turn>("opponent_turn", s =>
            {
                Dispatcher.Invoke(() =>
                {
                    var button = FindName(s.Button) as Button;
                    button.Content = s.Char;
                });
            });
            _connection.On<string>("gameresult", async s =>
            {
                if (s == "nobody")
                {
                    MessageBox.Show("Ничья");
                }
                else if (s == "win")
                {
                    MessageBox.Show("Вы победили");
                }
                else
                {
                    MessageBox.Show("Вы проиграли");
                }
                string nextgame = "nope";
                if (MessageBox.Show("Еще раз?", "Играем", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    nextgame = "yeap";
                    Dispatcher.Invoke(() =>
                    {
                        foreach (Button b in gridButtons.Children)
                        {
                            b.Content = null;
                        }
                    });
                    MyTurn = false;
                }
                await _connection.SendAsync("NextGame", nextgame, NickName);
                if (nextgame == "nope")
                {
                    await _connection.StopAsync();
                    Dispatcher.Invoke(() =>
                    {
                        Close();
                    });
                }
            });
        }

        private void CreateConnection()
        {
            var win = new WinOptions();
            if (win.ShowDialog() != true)
            {
                Close();
            }
            string address = win.Address;
            _connection = new HubConnectionBuilder().
                            AddJsonProtocol(s =>
                            {
                                s.PayloadSerializerOptions.ReferenceHandler =
                                System.Text.Json.Serialization.ReferenceHandler.Preserve;
                            }
                            ).
                        WithUrl(address + "/game").
                        Build();

            _connection.StartAsync();

            Unloaded += async (s, e) => await _connection.StopAsync();
        }


        private async void MakeTurn(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button.Content == null)
            {
                button.Content = myChar;
                MyTurn = false;
                await _connection.SendAsync("MakeTurn", new Turn { GameId = gameid, Button = button.Name, Char = myChar });
            }
        }
    }
}
