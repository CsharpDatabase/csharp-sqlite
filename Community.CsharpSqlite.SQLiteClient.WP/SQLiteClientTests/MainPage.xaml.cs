using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Data;
using Community.CsharpSqlite.SQLiteClient;
using test;

namespace Test.WP
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);
        }

        public void WriteLine(String value)
        {
            this.listBox1.Items.Add(value);// + Environment.NewLine;
        }

        protected void MainPage_Loaded(object sender, EventArgs e)
         {

             //SQLiteClientTests.SQLiteClientTestDriver.Main(null);
             IDbConnection cnn;

             try
             {
                 System.IO.IsolatedStorage.IsolatedStorageFile.GetUserStoreForApplication().DeleteFile("test.db3");
             }
             catch { }


             using (cnn = new SqliteConnection())
             {
                 TestCases tests = new TestCases();

                 cnn.ConnectionString = "data source=test.db3,password=0x01010101010101010101010101010101";
                 cnn.Open();
                 tests.Run(cnn, this);
             }
         }
    }
}