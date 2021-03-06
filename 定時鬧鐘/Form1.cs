using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace 定時鬧鐘
{
    public partial class Form1 : Form
    {
        bool isPlaying = false;
        bool shouldStopForAWhile = false;
        Thread playerThread = null;
        Thread scheduleThread = null;
        Thread blockMusicThread = null;
        SoundPlayer player = new SoundPlayer();
        Dictionary<int, string> schedule = new Dictionary<int, string>();

        public Form1()
        {
            InitializeComponent();

            this.readSchedule(@".\schedule.txt");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.scheduleThread = new Thread(this.monitorScheduledEvents);
            this.scheduleThread.Start();
            this.notifyIcon1.Visible = false;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //DialogResult dResult = MessageBox.Show("確定要離開系統?", "離開系統", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            DialogResult dResult = MessageBox.Show("這樣做會導致排程鬧鐘停止運作，確定要關閉程式嗎？", "關閉程式", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (dResult == DialogResult.No)
            {
                e.Cancel = true;
            }
            else
            {
                this.stopPlayingMusic(false);
                this.DeInitialize();
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.notifyIcon1.Visible = true;
                this.Hide();
            }
            else
            {
                this.notifyIcon1.Visible = false;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.blockMusicThread = new Thread(() => this.stopPlayingMusic(true));
            this.blockMusicThread.Start();
        }

        private void label1_Click(object sender, EventArgs e)
        {
        }

        private void notifyIcon1_MouseMove(object sender, MouseEventArgs e)
        {
            //this.notifyIcon1.ShowBalloonTip(3000);
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.notifyIcon1.Visible = false;
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        void DeInitialize()
        {
            if (this.blockMusicThread != null)
            {
                this.blockMusicThread.Abort();
                this.blockMusicThread = null;
            }

            if (this.scheduleThread != null)
            {
                this.scheduleThread.Abort();
                this.scheduleThread = null;
            }

            if (this.playerThread != null)
            {
                this.playerThread.Abort();
                this.playerThread = null;
            }
        }

        async void stopPlayingMusic(bool blockAlarm)
        {
            if (isPlaying)
            {
                if (this.player != null)
                {
                    this.player.Stop();
                }
                this.isPlaying = false;
            }

            if (blockAlarm)
            {
                this.shouldStopForAWhile = true;
                await waitFewSeconds(59);
                this.shouldStopForAWhile = false;
            }
        }

        async void playMusic()
        {
            this.isPlaying = true;
            this.player = new SoundPlayer();
            this.player.SoundLocation = @".\music.wav";
            this.player.Play();
            await waitFewSeconds(22); // 22秒鐘 
            this.player.Stop();
            await waitFewSeconds(38); // 38秒鐘 
            this.isPlaying = false;
        }

        Task<int> waitFewSeconds(int seconds)
        {
            Thread.Sleep(1000 * seconds);
            return Task<int>.Factory.StartNew(() => seconds);
        }

        void readSchedule(string scheduleFilePath)
        {
            using (StreamReader sr = new StreamReader(scheduleFilePath))
            {
                var text = sr.ReadToEnd();
                text = text.Trim(' ').Trim('\n').Trim('\t');
                var tsArr = text.Split(';');
                foreach (string s in tsArr)
                {
                    if (string.IsNullOrEmpty(s)) continue;
                    try
                    {
                        var tsArr2 = s.Split('#');
                        var comment = tsArr2[1];
                        var tsArr3 = tsArr2[0].Split(':');
                        var hour = int.Parse(tsArr3[0]);
                        var minute = int.Parse(tsArr3[1]);
                        var time = hour * 60 + minute;
                        this.schedule[time] = comment;
                    }
                    catch (Exception ex)
                    {
                        textBox1.Text += ex.ToString() + "\r\n";
                    }
                }
            }
        }
        
        async void monitorScheduledEvents()
        {
            while (true)
            {
                var now = DateTime.Now;
                var hour = now.Hour;
                var minute = now.Minute;
                var time = hour * 60 + minute;
                if (this.schedule.ContainsKey(time))
                {
                    if (!isPlaying)
                    {
                        this.playerThread = new Thread(this.playMusic);
                        this.playerThread.Start();
                        //textBox1.Text += this.schedule[time] + "\r\n";
                        //this.schedule.Remove(time);
                    }
                }
                await this.waitFewSeconds(10);
            }
        }

    }
}
