using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Community.CsharpSqlite.SQLiteClient;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MetroSQLiteClientTest2
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            SyncContext = SynchronizationContext.Current;
            this.InitializeComponent();
            _testComboBox.Items.Add("Test 1");
            _testComboBox.Items.Add("Test 2");
            _testComboBox.Items.Add("Test 3");
            _testComboBox.Items.Add("Test 4");
            _testComboBox.Items.Add("Test 5");
            _testComboBox.Items.Add("Test 6");
            _testComboBox.Items.Add("Test 7");
            _testComboBox.Items.Add("Issue 65");
            _testComboBox.Items.Add("Issue 76");
            _testComboBox.Items.Add("Issue 86");
            _testComboBox.Items.Add("Issue 119");
            _testComboBox.Items.Add("Issue 124");
            _testComboBox.SelectedIndex = 0;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

                #region Synchronous File Operations

        static private bool FileExists(string path)
        {
            bool exists = true;
            try
            {
                Task<StorageFile> fileTask = StorageFile.GetFileFromPathAsync(path).AsTask<StorageFile>();
                fileTask.Wait();
            }
            catch (Exception e)
            {
                AggregateException ae = e as AggregateException;
                if(ae != null && ae.InnerException is FileNotFoundException)
                    exists = false;
            }
            return exists;
        }
        static private void FileDelete(string path)
        {
            Task<StorageFile> fileTask = StorageFile.GetFileFromPathAsync(path).AsTask<StorageFile>();
            fileTask.Wait();
            fileTask.Result.DeleteAsync().AsTask().Wait();
        }

        #endregion

        #region "Logging" Methods

        private void ConsoleWriteLine(string value)
        {
            if (SynchronizationContext.Current != SyncContext)
            {
                SyncContext.Post(delegate
                {
                    ConsoleWriteLine(value);
                }, null);
                return;
            }

            TextBlock block = new TextBlock();
            block.Text = value;
            _resultsStackPanel.Children.Add(block);
        }

        private void ConsoleWriteError(string value)
        {
            if (SynchronizationContext.Current != SyncContext)
            {
                SyncContext.Post(delegate
                {
                    ConsoleWriteError(value);
                }, null);
                return;
            }

            TextBlock block = new TextBlock();
            block.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
            block.Text = value;
            _resultsStackPanel.Children.Add(block);
        }

        #endregion

        #region Test1

        public IAsyncAction Test1Async()
        {
            return Task.Run(() => { Test1(); }).AsAsyncAction();
        }

        public void Test1()
        {
            try
            {
                ConsoleWriteLine("Test1 Start.");

                ConsoleWriteLine("Create connection...");
                SqliteConnection con = new SqliteConnection();

                string dbFilename = Path.Combine(ApplicationData.Current.LocalFolder.Path, @"SqliteTest3.db");
                string cs = string.Format("Version=3,uri=file:{0}", dbFilename);

                ConsoleWriteLine(String.Format("Set connection String: {0}", cs));

                if (FileExists(dbFilename))
                    FileDelete(dbFilename);

                con.ConnectionString = cs;

                ConsoleWriteLine("Open database...");
                con.Open();

                ConsoleWriteLine("create command...");
                IDbCommand cmd = con.CreateCommand();

                ConsoleWriteLine("create table TEST_TABLE...");
                cmd.CommandText = "CREATE TABLE TEST_TABLE ( COLA INTEGER, COLB TEXT, COLC DATETIME )";
                cmd.ExecuteNonQuery();

                ConsoleWriteLine("insert row 1...");
                cmd.CommandText = "INSERT INTO TEST_TABLE ( COLA, COLB, COLC ) VALUES (123,'ABC','2008-12-31 18:19:20' )";
                cmd.ExecuteNonQuery();

                ConsoleWriteLine("insert row 2...");
                cmd.CommandText = "INSERT INTO TEST_TABLE ( COLA, COLB, COLC ) VALUES (124,'DEF', '2009-11-16 13:35:36' )";
                cmd.ExecuteNonQuery();

                //Console.WriteLine("commit...");
                //cmd.CommandText = "COMMIT";
                //cmd.ExecuteNonQuery();

                ConsoleWriteLine("SELECT data from TEST_TABLE...");
                cmd.CommandText = "SELECT COLA, COLB, COLC FROM TEST_TABLE";
                IDataReader reader = cmd.ExecuteReader();
                int r = 0;
                ConsoleWriteLine("Read the data...");
                while (reader.Read())
                {
                    ConsoleWriteLine(String.Format("  Row: {0}", r));
                    int i = reader.GetInt32(reader.GetOrdinal("COLA"));
                    ConsoleWriteLine(String.Format("    COLA: {0}", i));

                    string s = reader.GetString(reader.GetOrdinal("COLB"));
                    ConsoleWriteLine(String.Format("    COLB: {0}", s));

                    DateTime dt = reader.GetDateTime(reader.GetOrdinal("COLC"));
                    ConsoleWriteLine(String.Format("    COLB: {0}", dt.ToString("MM/dd/yyyy HH:mm:ss")));

                    r++;
                }
                ConsoleWriteLine(String.Format("Rows retrieved: {0}", r));

                ConsoleWriteLine("Close and cleanup...");
                con.Close();
                con = null;

                ConsoleWriteLine("Test1 Done.");
            }
            catch (Exception e)
            {
                ConsoleWriteError("ERROR: " + e.Message);
                ConsoleWriteError(e.StackTrace);
            }
        }

        #endregion

        #region Test2

        public IAsyncAction Test2Async()
        {
            return Task.Run(() => { Test2(); }).AsAsyncAction();
        }

        public void Test2()
        {
            try
            {
                ConsoleWriteLine("Test2 Start.");

                ConsoleWriteLine("Create connection...");
                SqliteConnection con = new SqliteConnection();

                string dbFilename = Path.Combine(ApplicationData.Current.LocalFolder.Path, @"SqliteTest3.db");
                string cs = string.Format("Version=3,uri=file:{0}", dbFilename);

                ConsoleWriteLine(String.Format("Set connection String: {0}", cs));

                if (FileExists(dbFilename))
                    FileDelete(dbFilename);

                con.ConnectionString = cs;

                ConsoleWriteLine("Open database...");
                con.Open();

                ConsoleWriteLine("create command...");
                IDbCommand cmd = con.CreateCommand();

                ConsoleWriteLine("create table TEST_TABLE...");
                cmd.CommandText = "CREATE TABLE TBL ( ID NUMBER, NAME TEXT)";
                cmd.ExecuteNonQuery();

                ConsoleWriteLine("insert row 1...");
                cmd.CommandText = "INSERT INTO TBL ( ID, NAME ) VALUES (1, 'ONE' )";
                cmd.ExecuteNonQuery();

                ConsoleWriteLine("insert row 2...");
                cmd.CommandText = "INSERT INTO TBL ( ID, NAME ) VALUES (2, 'ä¸­æ–‡' )";
                cmd.ExecuteNonQuery();

                //Console.WriteLine("commit...");
                //cmd.CommandText = "COMMIT";
                //cmd.ExecuteNonQuery();

                ConsoleWriteLine("SELECT data from TBL...");
                cmd.CommandText = "SELECT id,NAME FROM tbl WHERE name = 'ä¸­æ–‡'";
                IDataReader reader = cmd.ExecuteReader();
                int r = 0;
                ConsoleWriteLine("Read the data...");
                while (reader.Read())
                {
                    ConsoleWriteLine(String.Format("  Row: {0}", r));
                    int i = reader.GetInt32(reader.GetOrdinal("ID"));
                    ConsoleWriteLine(String.Format("    ID: {0}", i));

                    string s = reader.GetString(reader.GetOrdinal("NAME"));
                    ConsoleWriteLine(String.Format("    NAME: {0} = {1}", s, s == "ä¸­æ–‡"));
                    r++;
                }
                ConsoleWriteLine(String.Format("Rows retrieved: {0}", r));

                ConsoleWriteLine("Close and cleanup...");
                con.Close();
                con = null;

                ConsoleWriteLine("Test2 Done.");
            }
            catch (Exception e)
            {
                ConsoleWriteError("ERROR: " + e.Message);
                ConsoleWriteError(e.StackTrace);
            }
        }

        #endregion

        #region Test3

        public IAsyncAction Test3Async()
        {
            return Task.Run(() => { Test3(); }).AsAsyncAction();
        }

        public void Test3()
        {
            try
            {

                ConsoleWriteLine("Test3 (Date Paramaters) Start.");

                ConsoleWriteLine("Create connection...");
                SqliteConnection con = new SqliteConnection();

                string dbFilename = Path.Combine(ApplicationData.Current.LocalFolder.Path, @"SqliteTest3.db");
                string cs = string.Format("Version=3,uri=file:{0}", dbFilename);

                ConsoleWriteLine(String.Format("Set connection String: {0}", cs));

                if (FileExists(dbFilename))
                    FileDelete(dbFilename);

                con.ConnectionString = cs;

                ConsoleWriteLine("Open database...");
                con.Open();

                ConsoleWriteLine("create command...");
                IDbCommand cmd = con.CreateCommand();

                ConsoleWriteLine("create table TEST_TABLE...");
                cmd.CommandText = "CREATE TABLE TBL ( ID NUMBER, DATE_TEXT REAL)";
                cmd.ExecuteNonQuery();

                ConsoleWriteLine("insert ...");
                cmd.CommandText = "INSERT INTO TBL  ( ID, DATE_TEXT) VALUES ( 1,  @DATETEXT)";
                cmd.Parameters.Add(
                  new SqliteParameter
                  {
                      ParameterName = "@DATETEXT",
                      Value = DateTime.Now
                  }
                  );

                cmd.ExecuteNonQuery();


                ConsoleWriteLine("SELECT data from TBL...");
                cmd.CommandText = "SELECT * FROM tbl";
                IDataReader reader = cmd.ExecuteReader();
                int r = 0;
                ConsoleWriteLine("Read the data...");
                while (reader.Read())
                {
                    ConsoleWriteLine(String.Format("  Row: {0}", r));
                    int i = reader.GetInt32(reader.GetOrdinal("ID"));
                    ConsoleWriteLine(String.Format("    ID: {0}", i));

                    string s = reader.GetString(reader.GetOrdinal("DATE_TEXT"));
                    ConsoleWriteLine(String.Format("    DATE_TEXT: {0}", s));
                    r++;
                }
                ConsoleWriteLine(String.Format("Rows retrieved: {0}", r));


                ConsoleWriteLine("Close and cleanup...");
                con.Close();
                con = null;

                ConsoleWriteLine("Test3 Done.");
            }
            catch (Exception e)
            {
                ConsoleWriteError("ERROR: " + e.Message);
                ConsoleWriteError(e.StackTrace);
            }
        }

        #endregion

        #region Test4

        public IAsyncAction Test4Async()
        {
            return Task.Run(() => { Test4(); }).AsAsyncAction();
        }

        //nSoftware code for Threading
        string connstring_T4;
        public void Test4()
        {
            string dbFilename = Path.Combine(ApplicationData.Current.LocalFolder.Path, @"threading_t4.db");
            if (FileExists(dbFilename))
                FileDelete(dbFilename);
            connstring_T4 = @"Version=3,busy_timeout=100,uri=file:" + dbFilename;

            Setup_T4();
            InsertSameTable_T4(); //concurrent inserts
            SelectorWrite_T4(); //concurrent selects and inserts
            ConsoleWriteLine("Testing for Threading done.");
        }
        private void SelectorWrite_T4()
        {
            //concurrent reads/writes in the same table, if there were only Selects it would be preferable for the sqlite engine not to lock internally.
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                Task worker = Task.Factory.StartNew((state) =>
                {
                    // Cannot use value of i, since it exceeds the scope of this thread and will be 
                    // reused by multiple threads
                    int aValue = 100 + (int)state;
                    int op = aValue % 2;
                    ConsoleWriteLine(String.Format("SELECT/INSERT ON Thread {0}", state));

                    using (SqliteConnection con = new SqliteConnection())
                    {
                        try
                        {
                            con.ConnectionString = connstring_T4;
                            con.Open();
                            IDbCommand cmd = con.CreateCommand();
                            if (op == 0)
                            {
                                cmd.CommandText = String.Format("Select * FROM ATABLE");
                                cmd.ExecuteReader();
                            }
                            else
                            {
                                cmd.CommandText = String.Format("INSERT INTO ATABLE ( A, B, C ) VALUES ({0},'threader', '1' )", aValue);
                                ConsoleWriteLine(cmd.CommandText);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        catch (Exception e)
                        {
                            ConsoleWriteError("ERROR: " + e.Message);
                            ConsoleWriteError(e.StackTrace);
                        }
                    }
                },i);
                tasks.Add(worker);
            }
            ConsoleWriteLine("Waiting for select/write tasks...");
            Task.WaitAll(tasks.ToArray<Task>());
            ConsoleWriteLine("All select/write tasks complete");
        }
        //we need concurrency support on a table level inside of the database file.
        private void InsertSameTable_T4()
        {
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                Task worker = Task.Factory.StartNew((state) =>
                {
                    // Cannot use value of i, since it exceeds the scope of this thread and will be 
                    // reused by multiple threads

                    ConsoleWriteLine(String.Format("INSERTING ON Thread {0}", state));
                    int aValue = (int)state;

                    using (SqliteConnection con = new SqliteConnection())
                    {
                        try
                        {
                            con.ConnectionString = connstring_T4;
                            ConsoleWriteLine(String.Format("About to Open Thread {0}", state));
                            con.Open();
                            ConsoleWriteLine(String.Format("Open complete Thread {0}", state));
                            IDbCommand cmd = con.CreateCommand();
                            cmd = con.CreateCommand();
                            cmd.CommandText = String.Format("INSERT INTO ATABLE ( A, B, C ) VALUES ({0},'threader', '1' )", aValue);
                            ConsoleWriteLine(cmd.CommandText);
                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception e)
                        {
                            ConsoleWriteError("ERROR: " + e.Message);
                            ConsoleWriteError(e.StackTrace);
                        }
                    }
                },i);
                tasks.Add(worker);
            }
            ConsoleWriteLine("Waiting for insert tasks...");
            Task.WaitAll(tasks.ToArray<Task>());
            ConsoleWriteLine("All insert tasks complete");
        }

        private void Setup_T4()
        {
            using (SqliteConnection con = new SqliteConnection())
            {
                con.ConnectionString = connstring_T4;
                con.Open();
                IDbCommand cmd = con.CreateCommand();
                cmd = con.CreateCommand();
                cmd.CommandText = "CREATE TABLE IF NOT EXISTS ATABLE(A integer primary key , B varchar (50), C integer)";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "CREATE TABLE IF NOT EXISTS BTABLE(A integer primary key , B varchar (50), C integer)";
                cmd.ExecuteNonQuery();
                cmd.CommandText = String.Format("INSERT INTO BTABLE ( A, B, C ) VALUES (6,'threader', '1' )");
                cmd.ExecuteNonQuery();
            }
        }
        #endregion

        #region Issue 119

        public IAsyncAction Issue_119Async()
        {
            return Task.Run(() => { Issue_119(); }).AsAsyncAction();
        }

        public void Issue_119()
        {
            try
            {
                ConsoleWriteLine("Issue 119 Start.");

                ConsoleWriteLine("Create connection...");
                SqliteConnection con = new SqliteConnection();

                string dbFilename = Path.Combine(ApplicationData.Current.LocalFolder.Path, @"SqliteTest3.db");
                string cs = string.Format("Version=3,uri=file:{0}", dbFilename);

                ConsoleWriteLine(String.Format("Set connection String: {0}", cs));

                if (FileExists(dbFilename))
                    FileDelete(dbFilename);

                con.ConnectionString = cs;

                ConsoleWriteLine("Open database...");
                con.Open();

                ConsoleWriteLine("create command...");
                IDbCommand cmd = con.CreateCommand();

                ConsoleWriteLine("create table TEST_TABLE...");
                cmd.CommandText = "CREATE TABLE TEST_TABLE ( COLA INTEGER, COLB TEXT, COLC DATETIME )";
                cmd.ExecuteNonQuery();

                ConsoleWriteLine("insert row 1...");
                cmd.CommandText = "INSERT INTO TEST_TABLE ( COLA, COLB, COLC ) VALUES (123,'ABC','2008-12-31 18:19:20' )";
                cmd.ExecuteNonQuery();

                ConsoleWriteLine("insert row 2...");
                cmd.CommandText = "INSERT INTO TEST_TABLE ( COLA, COLB, COLC ) VALUES (124,'DEF', '2009-11-16 13:35:36' )";
                cmd.ExecuteNonQuery();

                ConsoleWriteLine("SELECT data from TEST_TABLE...");
                cmd.CommandText = "SELECT RowID, COLA, COLB, COLC FROM TEST_TABLE";
                IDataReader reader = cmd.ExecuteReader();
                int r = 0;
                ConsoleWriteLine("Read the data...");
                while (reader.Read())
                {
                    ConsoleWriteLine(String.Format("  Row: {0}", r));
                    int rowid = reader.GetInt32(reader.GetOrdinal("RowID"));
                    ConsoleWriteLine(String.Format("    RowID: {0}", rowid));

                    int i = reader.GetInt32(reader.GetOrdinal("COLA"));
                    ConsoleWriteLine(String.Format("    COLA: {0}", i));

                    string s = reader.GetString(reader.GetOrdinal("COLB"));
                    ConsoleWriteLine(String.Format("    COLB: {0}", s));

                    DateTime dt = reader.GetDateTime(reader.GetOrdinal("COLC"));
                    ConsoleWriteLine(String.Format("    COLB: {0}", dt.ToString("MM/dd/yyyy HH:mm:ss")));

                    r++;
                }

                ConsoleWriteLine("Close and cleanup...");
                con.Close();
                con = null;

                ConsoleWriteLine("Issue 119 Done.");
            }
            catch (Exception e)
            {
                ConsoleWriteError("ERROR: " + e.Message);
                ConsoleWriteError(e.StackTrace);
            }
        }
        #endregion

        public SynchronizationContext SyncContext { get; set; }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            _runButton.IsEnabled = false;
            _resultsStackPanel.Children.Clear();
            switch (_testComboBox.SelectedItem.ToString())
            {
                case "Test 1":
                    await Test1Async();
                    break;
                case "Test 2":
                    await Test2Async();
                    break;
                case "Test 3":
                    await Test3Async();
                    break;
                case "Test 4":
                    await Test4Async();
                    break;
                case "Issue 119":
                    await Issue_119Async();
                    break;

                default:
                    await new MessageDialog(_testComboBox.SelectedItem.ToString() + " Not Implemented Yet").ShowAsync();
                    break;
            }
            _runButton.IsEnabled = true;
        }

    }
}
