using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilityClasses;

namespace UtilityClasses
    {
    /// <summary>
    /// Base class for handling program arguments
    /// </summary>
    /// <remarks>
    /// 
    /// Note: this uses a BackgroundWorker thread for a reason, which is that it will 
    /// be automatically killed when the application closes.
    /// Normal Thread items are foreground threads, which have to be manually killed
    /// when the application ends or the domain isn't unloaded and the app never ends.
    /// That means the caller doesn't have to know how this all works: it kicks us off
    /// running, and forgets about us until we send 'em a new argument.
    /// It also has the advantage that it handles the Invoke required to signal events
    /// on the user thread (generally the UI thread) rather than ours.
    /// 
    /// There is documentation on this on Codeproject:
    ///    https://www.codeproject.com/Articles/5247406/Double-clicking-a-file-in-explorer-and-adding-it-t
    /// But the minimum details are below:
    /// 
    /// When you double click a file, Windows opens the associated app, and passes the 
    /// full file path as a command line argument. This works fine, and has done for many, 
    /// many versions of Windows - right back to V1.0, I believe. 
    ///
    /// And it's easy to get that argument - there are two ways.
    ///
    /// For a Console app, they are passed as a string array to the Main method.
    /// For a GUI app, they are available as an array of strings via the 
    ///    Environment.GetCommandLineArgs method.
    /// 
    /// The problem is ... once your app is running, that collection can't be added to, 
    /// or changed in any way - Windows can't add "new arguments" to the array, and has 
    /// no way to "alert you" that a new one is available.
    /// 
    /// So ... what you have to do is
    /// 
    /// Check if an instance of your app is running already.
    /// If it isn't, then handle the argument yourself, and prepare yourself in case the 
    ///    user tries to open another.
    /// If it is, pass the argument to the other instance, and close yourself down.
    /// And that means Sockets and TCP stuff.Yeuch.
    /// 
    /// So, I created a DynamicArgument class that handles it all for you, and provides 
    /// an event to tell you there is an argument available.It processes the arguments 
    /// and raises an event for each in turn. If it's the first instance, it creates a 
    /// thread which creates a Listener, and waits for information from other instances. 
    /// When it gets a new argument it raises an event for it.
    /// 
    /// To use it is pretty simple.
    /// 
    /// Add to your main form Load event:
    /// 
    ///         private void FrmMain_Load(object sender, FormClosingEventArgs e)
    ///         {
    ///         DynamicArgument da = DynamicArgument.Create();
    ///         da.ArgumentReceived += Application_ArgumentReceived;
    ///         bool result = da.Start();
    ///         if (!result) Close();
    ///         }
    /// DynamicArgument is a Singleton class, because it would get difficult and messy 
    /// if there were two Listeners trying to process arguments - so you have to Create 
    /// it rather than use the new constructor.
    /// 
    /// Add your event handler, and Start the system. If this was the only instance 
    /// running it returns true - if not then probably you want to close the form, which 
    /// will close the app - I'd do it for you, but this way you get the option to clean 
    /// up after yourself if you need to, which Application.Exit doesn't let you do.
    /// 
    /// Then just handle your event and do what you need to with the arguments:
    /// 
    /// private void Application_ArgumentReceived(object sender, DynamicArgument.DynamicArgmentEventArgs e)
    ///         {
    ///         tbData.Text += "\r\n" + e.Argument;
    ///         }
    /// </remarks>
    public class DynamicArgument
        {
        #region Constants
        /// <summary>
        /// Sending a command code: Timestamp
        /// </summary>
        private const byte CC_TimeStamp = (byte)'T';
        /// <summary>
        /// Sending a command code: Argument
        /// </summary>
        private const byte CC_Argument = (byte)'A';
        #endregion

        #region Fields
        #region Internal
        /// <summary>
        ///  The only one there will ever be.
        /// </summary>
        private static readonly DynamicArgument theOneAndOnly = new DynamicArgument();
        /// <summary>
        /// Arguments waiting to be dealt with.
        /// </summary>
        private readonly Queue<string> Arguments = new Queue<string>();
        #endregion

        #region Property bases
        #endregion
        #endregion

        #region Properties
        /// <summary>
        /// I need a port number, so this will do ... it's in the user space.
        /// Overwrite it if something on your system uses that port.
        /// </summary>
        public int ListenPort { get; set; } = 55191;
        #endregion

        #region Regular Expressions
        #endregion

        #region Enums
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        private DynamicArgument()
            {
            string[] args = Environment.GetCommandLineArgs();
            // First argument is application EXE, so skip that.
            for (int i = 1; i < args.Length; i++)
                {
                Arguments.Enqueue(args[i]);
                }
            }
        #endregion

        #region Events
        #region Event Arguments
        //public delegate void DynamicArgumentEventHandler(object sender, DynamicArgmentEventArgs e);
        /// <summary>
        /// Extends EventArgs to add the program argument string
        /// </summary>
        public class DynamicArgmentEventArgs : EventArgs
            {
            /// <summary>
            /// The argument passed to the app
            /// </summary>
            public string Argument { get; set; }
            }
        #endregion

        #region Event Constructors
        /// <summary>
        /// Event to indicate program argument available
        /// </summary>
        public event EventHandler<DynamicArgmentEventArgs> ArgumentReceived;
        /// <summary>
        /// Called to signal to subscribers that program argument available
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnArgumentReceived(DynamicArgmentEventArgs e)
            {
            ArgumentReceived?.Invoke(this, e);
            }
        #endregion

        #region Event Handlers
        #endregion
        #endregion

        #region Threads
        /// <summary>
        /// Listens for other applications trying to process arguments for us
        /// </summary>
        /// <remarks>
        /// 
        /// Note: this thread has no exit conditions: it's a background thread for a reason, 
        /// which is that it will be automatically killed when the app closes.
        /// That means the caller doesn't have to know how this all works: it kicks us off
        /// running, and forgets about us until we send 'em a new argument.
        /// </remarks>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Listener_DoWork(object sender, DoWorkEventArgs e)
            {
            if (sender is BackgroundWorker work)
                {
                IPAddress ipAddress = GetMyIP();
                while (true)
                    {
                    TcpListener listener = new TcpListener(ipAddress, ListenPort);
                    listener.Start();
                    while (true)
                        {
                        try
                            {
                            using (Socket s = listener.AcceptSocket())
                                {
                                int length = s.Available;
                                if (length > 0)
                                    {
                                    byte[] buffer = new byte[length];
                                    s.Receive(buffer);
                                    // Simple data format:
                                    //    Command Code (byte)
                                    //    Number of arguments (int32)
                                    //    Arguments (string)
                                    ByteArrayBuilder bab = new ByteArrayBuilder(buffer);
                                    byte cc = bab.GetByte();
                                    if (cc == CC_Argument)
                                        {
                                        int argCount = bab.GetInt();
                                        for (int i = 0; i < argCount; i++)
                                            {
                                            string arg = bab.GetString();
                                            Arguments.Enqueue(arg);
                                            }
                                        work.ReportProgress(0, 0);
                                        }
                                    }
                                }
                            }
                        catch (Exception ex)
                            {
                            Debug.WriteLine(ex.ToString());
                            }
                        }
                    }
                }
            }
        /// <summary>
        /// New argument received - add it to the collection, and pass 'em all up.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Listener_NewArguments(object sender, ProgressChangedEventArgs e)
            {
            if (e.UserState is string arg)
                {
                Arguments.Enqueue(arg);
                }
            PassUpAllArguments();
            }
        #endregion

        #region Public Methods
        /// <summary>
        /// Returns the singleton instance.
        /// </summary>
        /// <returns></returns>
        public static DynamicArgument Create()
            {
            return theOneAndOnly;
            }
        /// <summary>
        /// Start the arguments system
        /// </summary>
        /// <returns>
        /// False if this was not the only instance
        /// Under normal circumstances, your app should probably close if this
        /// returns false: but this allows you to do any clean up you need to 
        /// before your app kills itself.
        /// </returns>
        public bool Start()
            {
            Process otherInstance = GetOtherInstance();
            if (otherInstance == null)
                {
                // Just us. Start listener, and signal our arguments.
                BackgroundWorker listener = new BackgroundWorker();
                listener.DoWork += Listener_DoWork;
                listener.WorkerReportsProgress = true;
                listener.ProgressChanged += Listener_NewArguments;
                listener.RunWorkerAsync();
                PassUpAllArguments();
                return true;
                }
            // Other process exists - send arguments to it.

            // Kick the bugger into life! 
            // Omit this, and the first message is lost ...
            SendTime();
            String strHostName = Dns.GetHostName();
            IPHostEntry ipEntry = Dns.GetHostEntry(strHostName);
            IPAddress ipAddress = ipEntry.AddressList[0];
            using (TcpClient tc = new TcpClient(strHostName, ListenPort))
                {
                NetworkStream stream = tc.GetStream();
                ByteArrayBuilder bab = new ByteArrayBuilder();
                bab.Append(CC_Argument);
                int argCount = Arguments.Count;
                bab.Append(argCount);
                while (argCount-- > 0)
                    {
                    bab.Append(Arguments.Dequeue());
                    }
                byte[] data = bab.ToArray();
                stream.Write(data, 0, data.Length);
                stream.Flush();
                }
            return false;
            }
        #endregion

        #region Overrides
        #endregion

        #region Private Methods
        /// <summary>
        /// Gets the IP address of this system
        /// </summary>
        /// <returns></returns>
        private static IPAddress GetMyIP()
            {
            String strHostName = Dns.GetHostName();
            IPHostEntry ipEntry = Dns.GetHostEntry(strHostName);
            IPAddress ipAddress = ipEntry.AddressList[0];
            return ipAddress;
            }
        /// <summary>
        /// Checks that just a single instance of an application is running.
        /// </summary>
        /// <remarks>
        /// Checks if this is the only executing example of this process.
        /// </remarks>
        /// <returns>
        /// Process for other instance if any.
        /// </returns>
        public static Process GetOtherInstance()
            {
            Process thisProcess = Process.GetCurrentProcess();
            foreach (Process proc in Process.GetProcessesByName(thisProcess.ProcessName))
                {
                if (proc.Id != thisProcess.Id)
                    {
                    return proc;
                    }
                }
            return null;
            }
        /// <summary>
        /// Pass all arguments up to caller.
        /// </summary>
        private void PassUpAllArguments()
            {
            while (Arguments.Count > 0)
                {
                OnArgumentReceived(new DynamicArgmentEventArgs() { Argument = Arguments.Dequeue() });
                }
            }
        /// <summary>
        /// Send a timestamp to the Listener
        /// </summary>
        private void SendTime()
            {
            String strHostName = Dns.GetHostName();
            using (TcpClient tc = new TcpClient(strHostName, ListenPort))
                {
                NetworkStream stream = tc.GetStream();
                ByteArrayBuilder bab = new ByteArrayBuilder();
                bab.Append(CC_TimeStamp);
                bab.Append(1);
                bab.Append(DateTime.Now.ToString("hh:mm:ss"));
                byte[] data = bab.ToArray();
                stream.Write(data, 0, data.Length);
                }
            }
        #endregion
        }
    }
