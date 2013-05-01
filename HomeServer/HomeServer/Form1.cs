using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using MailHelper;

namespace HomeServer
{
    public partial class Form1 : Form
    {
        mbed mbed1 = new mbed();
        mbed mbed2 = new mbed();
        int locked = -1;
        float dist = -1;
        float temperature = -1;
        int lamp = -1;
        int oven = -1;
        int light = -1;
        public delegate void guiCallback();
        public guiCallback g;

        public Form1()
        {
            InitializeComponent();
            SetupMbeds();
            //label4.Text = "None Taken Yet";
            Console.Write("This is the end of Form1()");
        }
        public void SetupMbeds()
        {
            mbed1.device.PortName = "COM10";        //will need to be updated with the ports that the Atom uses for them
            mbed2.device.PortName = "COM11";
            mbed1.number = 1;
            mbed2.number = 2;

            Thread m1Thread = new Thread(mbed1loop); // create new thread.
            m1Thread.IsBackground = true;
            m1Thread.Start(); // start new thread.
            
            Thread m2Thread = new Thread(mbed2loop); // create new thread.
            m2Thread.IsBackground = true;
            m2Thread.Start(); // start new thread.

            //mailClient mailObject = new mailClient();
            Thread mailThread = new Thread(runMail); // create new thread.
            mailThread.IsBackground = true;
            mailThread.Start(); // start new thread.



            Thread.Sleep(500);
            if (mbed1.device.IsOpen)
            {
                label1.Text = "mbed1 Opened on " + mbed1.getPort();
                mbed1.getFullStatus();
            }
            else
            {
                label1.Text = "mbed1 failed to open";
            }

            if (mbed2.device.IsOpen) {
                label2.Text = "mbed2 Opened on " + mbed2.getPort();
                mbed2.getFullStatus();
            }
            else 
            {
                label2.Text = "mbed2 failed to open";
            }
            
            
            
        }
        public void runMail()
        {
            mailClient mailObject = new mailClient();
            string CurDir = Directory.GetCurrentDirectory();
            string FileName = CurDir + "\\MessageText1.txt";
            string re = "";
            bool proc = false;
            while (true)
            {
                MailHelper.mailClient.Receive();
                if (File.Exists(FileName))
                {
                    proc = true;
                    foreach (string s in mailObject.emailParse(FileName))
                    {
                        emailParse(s);
                    }
                }
                if (proc)
                {
                    //MessageBox.Show("Sending : \n" + writeBody());
                    //MessageBox.Show("\nSending To" + re);
                    //mailClient.Send(re,"SmartHouse Response", writeBody(), null);
                    proc = false;
                }
                Thread.Sleep(5000); // sleep for 5 seconds.
            }
        }
        public void mbed1loop()
        {
            mbed1.setup();
            string temp;
            while (true)
            {
                if (mbed1.device.BytesToRead != 0)
                {
                    temp = mbed1.getLine();
                    Console.Write(temp);
                    mbedParse(temp);
                }
                Thread.Sleep(30);
            }
        }
        public void mbed2loop()
        {
            mbed2.setup();
            string temp;
            while (true)
            {
                if (mbed2.device.BytesToRead != 0)
                {
                    temp = mbed2.getLine();
                    Console.Write(temp);
                    mbedParse(temp);
                }
                Thread.Sleep(30);
            }
        }
        public void updateGUI()
        {
            guiLock();
            guiLamp();
            guiLights();
            guiOven();
            guiTemperature();
            guiDist();
        }
        public string writeBody()
        {
            string temp = "";
            temp = temp + "Locked: " + locked + "\n";
            temp = temp + "Dist: " + dist + "\n";
            temp = temp + "Temperature: " + temperature + "F\n";
            temp = temp + "Lamp: " + lamp + "\n";
            temp = temp + "Oven: " + oven + "\n";
            temp = temp + "Lights: " + light + "\n";
            return temp;
        }
        /*
        private void mbed1DataHandler(object sender, SerialDataReceivedEventArgs e) // used to have "static" after private. Deleted to get rid of "mbedParse" error.
        {
            Console.Write("mbed1 Handler Called");
            SerialPort sp = (SerialPort)sender;
         //   string data = sp.ReadLine();
            string data = mbed1.device.ReadExisting();
            Console.Write(data);
            mbedParse(data);
        }
        private void mbed2DataHandler(object sender, SerialDataReceivedEventArgs e) // smae as upper comment.
        {
            Console.Write("mbed2 Handler Called");
            SerialPort sp = (SerialPort)sender;
            //string data = sp.ReadLine();
            string data = mbed2.device.ReadExisting();
            Console.Write(data);
            mbedParse(data);
        }
         */
        public void mbedParse(string s)
        {
            //MessageBox.Show("mbedParsing " + s);
            string[] temp = s.Split(':');
            string val = "";
            try
            {
                val = temp[1];
            }
            catch (Exception e)
            {
                return;
            }
            switch (temp[0])        //still needs cases to be written
            {
                case "B":   //breakin detected
                    MessageBox.Show("There has been a breakin!");
                    break;
                case "D":   //distance updated
                    dist = Int32.Parse(val);
                    break;
                case "A":   //lamp updated
                    lamp = Int32.Parse(val);
                    //guiLamp();
                    break;
                case "I":   //lights updated
                    light = Int32.Parse(val);
                    //guiLights();
                    break;
                case "X":   //lock updated
                    locked = Int32.Parse(val);
                    //guiLock();
                    break;
                case "O":   //oven updated
                    oven = Int32.Parse(val);
                    //guiOven();
                    break;
                case "P":   //Pic command
                    getPicture();
                    break;
                case "T":   //Temperature updated
                    temperature = Single.Parse(val);
                    //guiTemperature();
                    break;
                case "E":
                    break;
            }
            Thread.Sleep(100);
             g = new guiCallback(updateGUI);
             this.Invoke(this.g);
        }

        public void emailParse(string s)
        {
            //MessageBox.Show("emailParsing " + s);
            string[] temp = s.Split(':');
            string val = "";
            try
            {
                val = temp[1];
            }
            catch (Exception e)
            {
                return;
            }
            switch (temp[0])        //still needs cases to be written
            {
                case "B":   //breakin detected
                    break;
                case "D":   //distance updated
                    dist = Single.Parse(val);
                    break;
                case "A":   //lamp updated
                    lamp = Int32.Parse(val);
                    if(lamp==1){
                        mbed2.turnOnLamp();
                    }
                    if (lamp == 0)
                    {
                        mbed2.turnOffLamp();
                    }
                    //guiLamp();
                    break;
                case "I":   //lights updated
                    light = Int32.Parse(val);
                    if (light == 1)
                    {
                        mbed2.turnOnLights();
                    }
                    if (light == 0)
                    {
                        mbed2.turnOffLights();
                    }
                    //guiLights();
                    break;
                case "X":   //lock updated
                    locked = Int32.Parse(val);
                    if (locked == 1)
                    {
                        mbed1.lockDoor();
                    }
                    if (locked == 0)
                    {
                        mbed1.unlockDoor();
                    }
                    //guiLock();
                    break;
                case "O":   //oven updated
                    oven = Int32.Parse(val);
                    if (oven == 1)
                    {
                        mbed2.turnOnOven();
                    }
                    if (oven == 0)
                    {
                        mbed2.turnOffOven();
                    }
                    //guiOven();
                    break;
                case "P":   //Pic command
                    getPicture();
                    break;
                case "T":   //Temperature updated
                    mbed2.getTemperature();
                    break;
                case "E":
                    break;
            }
        }

        public void getPicture()
        {
            string temp;
            string CurDir = Directory.GetCurrentDirectory() + "\\picture.jpg";  // Current Directory String for saving EmailText
            FileStream pic = File.Create(CurDir);
            do {
                temp = mbed1.device.ReadTo("SENT");
                pic.Write(Encoding.ASCII.GetBytes(temp),0,temp.Length*sizeof(char));
            } while( temp.IndexOf("SENT") == -1 );
            //label4.Text = "Taken at " + DateTime.Now.ToString("HH:mm:ss tt");
        }

        public void updateStatus()
        {
            //locked = mbed1.getLock();
            //dist = mbed1.getDistance();
            //temp = mbed2.getTemperature();
            //lamp = mbed2.getLamp();
            //oven = mbed2.getOven();
            //light = mbed2.getLights();
            //updateGUI();
        }
       
        
        
        public void guiLock()
        {
            if (locked == 1)
            {
                label3.Text = "Door Locked";
            }
            else 
            {
                label3.Text = "Door Unlocked";
            }

        }
        public void guiDist()
        {
            if (dist == 1)
            {
                label9.Text = "Door Open";
            }
            else
            {
                label9.Text = "Door Closed";
            }

        }
        public void guiLamp()
        {
            if (lamp == 1)
            {
                label5.Text = "Lamp On";
            }
            else
            {
                label5.Text = "Lamp Off";
            }
        }
        public void guiLights()
        {
            if (light == 1)
            {
                label6.Text = "Lights On";
            }
            else
            {
                label6.Text = "Lights Off";
            }
        }
        public void guiOven()
        {
            if (oven == 1)
            {
                label7.Text = "Oven On";
            }
            else
            {
                label7.Text = "Oven Off";
            }
        }
        public void guiTemperature()
        {
            label8.Text = temperature + "F";
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            //here we should do some initial checks to figure out the status of the mbeds
            updateStatus();
        }

        private void maskedTextBox1_MaskInputRejected(object sender, MaskInputRejectedEventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {
            label1.Text = "mbed1 Port = " + mbed1.getPort();
        }

        private void maskedTextBox2_MaskInputRejected(object sender, MaskInputRejectedEventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {
            label2.Text = "mbed2 Port = " + mbed2.getPort();
        }
        public class mbed       //class used to itneract with the mbed
        {
            public SerialPort device = new SerialPort();   //declare that there is a SerialPort for all functions
            public int number = 0;
            public bool setup()
            {       //function to set up the mbed communication
                device.BaudRate = 115200;
                device.ReadTimeout = 500;
                device.WriteTimeout = 500;
                device.ReceivedBytesThreshold = 200;
                device.Open();   //open a new serial port at the location specified
                return (device.IsOpen);                        //return 1 if working, 0 if not working
            }
            public void initialize()     //find out which commport has mbed1 on it
            {
                try
                {
                    setup();
                }
                catch (Exception e)
                {
                    Console.WriteLine("mbed{0} failed to initialize", number);
                    Console.WriteLine("Exception: " + e.Message);
                }
            }
            public int getID()      //figure out if this is mbed 1 or mbed 2
            {
                string temp;
                send("o");           //------------------needs the character that returns which mbed this is
                temp = getLine();
                temp = Regex.Match(temp, @"\d+").Value;
                return Int32.Parse(temp);
            }
            public void send(string c)     //function to send a message to the mbed
            {
                //device.Write(c.ToCharArray(0, 1), 0, 1);
                device.Write(c);
            }
            public string getLine()
            {
                return device.ReadLine();
            }
            public string getPort()
            {
                return device.PortName;
            }
            public void getFullStatus()
            {
                send("p");
            }
            public void getDistance()
            {
                if (number == 1)
                {
                    send("q");
                    //temp = getLine();
                    //temp = Regex.Match(temp, @"\d+").Value;
                    //return Single.Parse(temp);
                }
            }
            public void getLock()
            {
                if (number == 1)
                {
                    send("w");
                    //temp = getLine();
                    //temp = Regex.Match(temp, @"\d+").Value;
                    //return Int32.Parse(temp);
                }
            }
            public void lockDoor()
            {
                if (number == 1)
                {
                    send("s");
                }
            }
            public void unlockDoor()
            {
                if (number == 1)
                {
                    send("x");
                }
            }
            public void getPicture()
            {
                if (number == 1)
                {
                    send("t");
                }
            }
            public void getLamp()
            {
                if (number == 2)
                {
                    send("q");
                    //temp = getLine();
                    //temp = Regex.Match(temp, @"\d+").Value;
                    //return Int32.Parse(temp);
                }
            }
            public void turnOnLamp()
            {
                if (number == 2)
                {
                    send("a");
                }
            }
            public void turnOffLamp()
            {
                if (number == 2)
                {
                    send("z");
                }
            }
            public void getOven()
            {
                if (number == 2)
                {
                    send("w");
                    //temp = getLine();
                    //temp = Regex.Match(temp, @"\d+").Value;
                    //return Int32.Parse(temp);
                }
            }
            public void turnOnOven()
            {
                if (number == 2)
                {
                    send("s");
                }
            }
            public void turnOffOven()
            {
                if (number == 2)
                {
                    send("x");
                }
            }
            public void getLights()
            {
                if (number == 2)
                {
                    send("e");
                    //temp = getLine();
                    //temp = Regex.Match(temp, @"\d+").Value;
                    //return Int32.Parse(temp);
                }
            }
            public void turnOnLights()
            {
                if (number == 2)
                {
                    send("d");
                }
            }
            public void turnOffLights()
            {
                if (number == 2)
                {
                    send("c");
                }
            }
            public void getTemperature()
            {
                if (number == 2)
                {
                    send("r");
                    //temp = getLine();
                    //temp = Regex.Match(temp, @"\d+").Value;
                    //return Single.Parse(temp);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (locked == 1)
            {
                mbed1.unlockDoor();                
            }
            else
            {
                mbed1.lockDoor();                
            }
            Thread.Sleep(100);
            mbed1.getLock();
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            mbed1.getPicture();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (lamp == 1)
            {
                mbed2.turnOffLamp();
            }
            else
            {
                mbed2.turnOnLamp();
            }
            Thread.Sleep(100);
            mbed2.getLamp();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (light == 1)
            {
                mbed2.turnOffLights();
            }
            else
            {
                mbed2.turnOnLights();
            }
            Thread.Sleep(100);
            mbed2.getLights();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (oven == 1)
            {
                mbed2.turnOffOven();
            }
            else
            {
                mbed2.turnOnOven();
            }
            Thread.Sleep(100);
            mbed2.getOven();
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void button6_Click(object sender, EventArgs e)
        {
            mbed2.getTemperature();
        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void button7_Click(object sender, EventArgs e)
        {
            mbed1.getDistance();
        }

        private void label9_Click(object sender, EventArgs e)
        {

        }
    }
}