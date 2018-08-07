using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.IO;
using System.Data;


namespace DataProcessTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();            
            DB.ProcessChanged += ProcessChangedHandler; //Присваиваем обработчик событию ProcessChanged класса DB

            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.DoWork += BackgroundWorker1_DoWork;
            backgroundWorker.ProgressChanged += BackgroundWorker1_ProgressChanged;
            backgroundWorker.RunWorkerCompleted += BackgroundWorker1_RunWorkerCompleted;
            Progressbar.Visibility  = Visibility.Collapsed;
            textBox.Text = "D:\\1\\input.csv";
        }


        

        DataTable dt = new DataTable();
        DataTable Filtered_dt = null;
        public static BackgroundWorker backgroundWorker = new BackgroundWorker();


        //Обработчик события изменения прогресса
        public  void ProcessChangedHandler(long progress)
        {
            try
            {
                Progressbar.Value = progress;
            }
            catch
            {
                Action action = () => { Progressbar.Value = progress; };
                this.Dispatcher.Invoke(action);
            }

            
        }

        //Кнопка обзор - выбор файла cvs
        private void button_Click(object sender, RoutedEventArgs e)
        {        

            System.Windows.Forms.OpenFileDialog OFileDlg = new System.Windows.Forms.OpenFileDialog();
            OFileDlg.DefaultExt = "*.csv";
            OFileDlg.Filter = "Text files (*.txt;*.csv)|*.txt;*.csv|All Files (*.*)|*.*";
            OFileDlg.FilterIndex = 1;
            OFileDlg.RestoreDirectory = true;
            OFileDlg.FileName = "";

            System.Windows.Forms.DialogResult result = OFileDlg.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                textBox.Text = OFileDlg.FileName;
            }
        }
        //Кнопка запуска обработки CSV
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            //Выключаем кнопку
            button1.IsEnabled = false;
            button1.Tag = button1.Content;
            button1.Content = "Processing...";

            string FilePath = textBox.Text;


            if (File.Exists(FilePath) && backgroundWorker.IsBusy != true)
            {
                backgroundWorker.RunWorkerAsync(FilePath);
            }
            
        }

        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            string FilePath = (string)e.Argument; // получаем ключ из аргументов


            //Включаем видимость прогрессбара и задаем его максимальное число
            FileInfo fi = new FileInfo(FilePath);
            int FileSize = (int)fi.Length;
            Action action = () => { Progressbar.Maximum = FileSize ; Progressbar.Visibility = Visibility.Visible; };
            this.Dispatcher.Invoke(action);

            dt = new DataTable();

            dt = FillTable.FillFromCSV(FilePath);
            
            if (dt == null) //Если вернулся null значит была запрошена отмена
            { 
            e.Cancel = true; //Выставляем флаг отмены
            return; //И выходим
            }

            e.Result = dt;

        }
        private void BackgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Progressbar.Value = e.ProgressPercentage;
        }
        private void BackgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Включаем кнопку
            button1.IsEnabled = true;
            button1.Content = button1.Tag;
            button1.Tag = "";

            if (e.Cancelled) //Если операция была отменена
            {
                Progressbar.Value = 0; Progressbar.Visibility = Visibility.Collapsed;                
            }
            else if (e.Error != null) //Если какие то ошибки
            {
                MessageBox.Show(e.Error.ToString());
            }
            else //Если всё ок
            {
                dt = (DataTable)e.Result;
                dataGrid.ItemsSource = dt.AsDataView();

                Progressbar.Value = Progressbar.Maximum;
            }
        }

        //Кнопка отмены
        private void button2_Click(object sender, RoutedEventArgs e)
        {
            backgroundWorker.CancelAsync();
            DB db = new DB();
            db.Cancel();


            // Включаем все кнопки и возвращаем им их исхожное значение Content
           // IEnumerable<Button> buttons = Grid.Children.OfType<Button>(); //Получаем все кнопки в коллекцию
          //  foreach (Button button in buttons)
           // {                           
           //     button.IsEnabled = true;
           //     button.Content = button.Tag;
           //     button.Tag = "";
           // }

        }




        //Кнопка сохранения в Access
        private async void button3_Click(object sender, RoutedEventArgs e)
        {            
            string FilePath = "";
            
            System.Windows.Forms.SaveFileDialog SFileDlg = new System.Windows.Forms.SaveFileDialog();
            SFileDlg.DefaultExt = "*.mdb";
            SFileDlg.Filter = "Export Database (*.mdb)|*.mdb";
            SFileDlg.FilterIndex = 2;
            SFileDlg.RestoreDirectory = true;
            SFileDlg.CheckFileExists = false;
            SFileDlg.CheckPathExists = true;
            SFileDlg.OverwritePrompt = true;
            SFileDlg.Title = "Select file path";
            SFileDlg.FileName = "MyDB.mdb";

            System.Windows.Forms.DialogResult result = SFileDlg.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                //Выключаем кнопку
                button3.IsEnabled = false;
                button3.Tag = button3.Content;
                button3.Content = "Processing...";


                FilePath = SFileDlg.FileName;

                //Копируем пустой файл export.mdb из ресурсов
                System.Windows.Resources.StreamResourceInfo res = Application.GetResourceStream(new Uri("Resources\\export.mdb", UriKind.Relative));

                using (FileStream fstream = new FileStream(FilePath, FileMode.OpenOrCreate))
                {
                    res.Stream.CopyTo(fstream);
                }


                DB db = new DB();
                int Completed;
                if (Filtered_dt == null)
                {
                    // Включаем видимость прогрессбара и задаем его максимальное число             
                    Progressbar.Maximum = dt.Rows.Count - 1; Progressbar.Visibility = Visibility.Visible;


                    Completed = await db.SaveDataTableToAccessAsync(dt, "MyTable", FilePath);
                }
                else
                {
                    // Включаем видимость прогрессбара и задаем его максимальное число             
                    Progressbar.Maximum = Filtered_dt.Rows.Count - 1; Progressbar.Visibility = Visibility.Visible;

                    Completed = await db.SaveDataTableToAccessAsync(Filtered_dt, "MyTable", FilePath);
                }

                if (Completed > 0)
                {
                    Progressbar.Value = Progressbar.Maximum;
                    MessageBox.Show("Export Complete. " + Completed  + " rows added.");
                }


            }
            else { return; }

            //Включаем кнопку
            button3.IsEnabled = true;
            button3.Content = button3.Tag;
            button3.Tag = "";
        }

        //Кнопка загрузки из Access
        private async void button4_Click(object sender, RoutedEventArgs e)
        {
            
            System.Windows.Forms.OpenFileDialog OFileDlg = new System.Windows.Forms.OpenFileDialog();
            OFileDlg.DefaultExt = "*.mdb";
            OFileDlg.Filter = "Access files (*.mdb)|*.mdb|All Files (*.*)|*.*";
            OFileDlg.FilterIndex = 1;
            OFileDlg.RestoreDirectory = true;
            OFileDlg.FileName = "";

            System.Windows.Forms.DialogResult result = OFileDlg.ShowDialog();

            string FilePath = "";
            if (result == System.Windows.Forms.DialogResult.OK)
            {


                //Выключаем кнопку
                button4.IsEnabled = false;
                button4.Tag = button4.Content;
                button4.Content = "Processing...";



                FilePath = OFileDlg.FileName;

                dt = new DataTable();
                DB db = new DB();
                dt = await db.GetDatatableFromAccessAsync("MyTable", FilePath);



                dataGrid.ItemsSource = null;
                dataGrid.ItemsSource = dt.AsDataView();

                Progressbar.Value = Progressbar.Maximum;
                MessageBox.Show("Import Complete.");

            }
            else { return; }
            //Включаем кнопку
            button4.IsEnabled = true;
            button4.Content = button4.Tag;
            button4.Tag = "";



        }

        //Кнопка очистки datagrid
        private void button5_Click(object sender, RoutedEventArgs e)
        {
            dataGrid.ItemsSource = null;
            dt = new DataTable();
        }

        //Кнопка загрузки из MySQL
        private async void button6_Click(object sender, RoutedEventArgs e)
        {
            //Выключаем кнопку
            button6.IsEnabled = false;
            button6.Tag = button6.Content;
            button6.Content = "Processing...";
            

            dt = new DataTable();
            DB db = new DB();
            SQLConnectionParams SqlConnParams = new SQLConnectionParams();



            SqlConnParams.tablename = textBox7.Text;
            SqlConnParams.hostname = textBox3.Text;
            SqlConnParams.database = textBox4.Text;
            SqlConnParams.user = textBox5.Text;
            SqlConnParams.passwd = textBox6.Text;
            


            dt = await db.GetDatatableFromMySQLAsync(SqlConnParams);

           

            dataGrid.ItemsSource = null;
            dataGrid.ItemsSource = dt.AsDataView();

            Progressbar.Value = Progressbar.Maximum;
            MessageBox.Show("Import Complete.");
            //Включаем кнопку
            button6.IsEnabled = true;
            button6.Content = button6.Tag;
            button6.Tag = "";


        }

        //Кнопка сохранения в MySQL
        private async void button7_Click(object sender, RoutedEventArgs e)
        {
            //Выключаем кнопку
            button7.IsEnabled = false;
            button7.Tag = button7.Content;
            button7.Content = "Processing...";


            SQLConnectionParams SqlConnParams = new SQLConnectionParams();
            DB db = new DB();


            SqlConnParams.tablename = textBox1.Text;
            SqlConnParams.hostname = textBox3.Text;
            SqlConnParams.database = textBox4.Text;
            SqlConnParams.user = textBox5.Text;
            SqlConnParams.passwd = textBox6.Text;


            int Completed;
            if (Filtered_dt == null)
            {
                // Включаем видимость прогрессбара и задаем его максимальное число             
                Progressbar.Maximum = dt.Rows.Count - 1; Progressbar.Visibility = Visibility.Visible;


                Completed = await db.SaveDataTableToMySQLAsync(dt, SqlConnParams); 
            }
            else
            {
                // Включаем видимость прогрессбара и задаем его максимальное число             
                Progressbar.Maximum = Filtered_dt.Rows.Count - 1; Progressbar.Visibility = Visibility.Visible;

                Completed = await db.SaveDataTableToMySQLAsync(Filtered_dt, SqlConnParams);
            }

            if (Completed > 0)
            {
                Progressbar.Value = Progressbar.Maximum;
                MessageBox.Show("Export Complete. " + Completed + " rows added.");   
            }

            //Включаем кнопку
            button7.IsEnabled = true;
            button7.Content = button7.Tag;
            button7.Tag = "";

        }


        //Кнопка сохранения в MySQL в существующую таблицу
        private async void button8_Click(object sender, RoutedEventArgs e)
        {
            //Выключаем кнопку
            button8.IsEnabled = false;
            button8.Tag = button8.Content;
            button8.Content = "Processing...";

            SQLConnectionParams SqlConnParams = new SQLConnectionParams();
            DB db = new DB();


            SqlConnParams.tablename = textBox2.Text;
            SqlConnParams.hostname = textBox3.Text;
            SqlConnParams.database = textBox4.Text;
            SqlConnParams.user = textBox5.Text;
            SqlConnParams.passwd = textBox6.Text;

            int Completed;
            if (Filtered_dt == null)
            {
                // Включаем видимость прогрессбара и задаем его максимальное число             
                Progressbar.Maximum = dt.Rows.Count - 1; Progressbar.Visibility = Visibility.Visible;


                Completed = await db.SaveToExistingTableAsync(dt, SqlConnParams);
            }
            else
            {
                // Включаем видимость прогрессбара и задаем его максимальное число             
                Progressbar.Maximum = Filtered_dt.Rows.Count - 1; Progressbar.Visibility = Visibility.Visible;

                Completed = await db.SaveToExistingTableAsync(Filtered_dt, SqlConnParams);
            }

            if (Completed > 0)
            {
                Progressbar.Value = Progressbar.Maximum;
                MessageBox.Show("Export Complete. " + Completed + " rows added.");               
            }
            //Включаем кнопку
            button8.IsEnabled = true;
            button8.Content = button8.Tag;
            button8.Tag = "";


        }


        
        //Событие изменение текстбокса фильтра
        private async void textBox8_TextChanged(object sender, TextChangedEventArgs e)
        {            
            if (textBox8.Text != "")
            {
                Filter filter = new Filter();                
                Filtered_dt = await filter.FilterTableByWordAsync(dt, textBox8.Text);
                dataGrid.ItemsSource = Filtered_dt.AsDataView(); //Указываем новый дататейбл в качестве источника
            }
            else
            {
                dataGrid.ItemsSource = dt.AsDataView();
                Filtered_dt = null;
            }

        }
    }
}
