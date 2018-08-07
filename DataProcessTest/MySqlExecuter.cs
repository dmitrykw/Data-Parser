using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Windows;
using MySql.Data.MySqlClient;
using System.Collections;

namespace DataProcessTest
{
    class MySqlExecuter
    {

        private SQLConnectionParams SqlConnParams;


        public MySqlExecuter(SQLConnectionParams SqlConnParams) //В конструкторе при создании экземпляра класса получаем на вход параметры SQL соединеия нашего типа SQLConnectionParams
        {
            this.SqlConnParams = SqlConnParams;
        }

        MySqlConnection conn = null; //Объявляем переменную нашего Connection



        public void ConnectionOpen() //Метод для открытия соединения
        {

            try
            {
                string conString = @"Data Source=" + SqlConnParams.hostname + ";port=3306;Initial Catalog=" + SqlConnParams.database + ";User Id=" + SqlConnParams.user + ";password=" + SqlConnParams.passwd + "";
                conn = new MySqlConnection(conString);


                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();// Открываем соединиение
                }

            }
            catch { MessageBox.Show("Error to estsablish the Mysql Connection"); }
        }

        public void ExecSQL(string sqlCommand) //Метод для выполнения SQL команды
        {
            try
            {
                MySqlCommand cmd = new MySqlCommand(sqlCommand, conn);
            cmd.CommandType = CommandType.Text;
            cmd.ExecuteNonQuery();
            }
            catch { MessageBox.Show("Error to executing Mysql Command"); }
        }
        
        //Метод для выполнения SQL команды - перегруженный метод для параметризированных запросов. Принимаем коллекцию ключ значение содержащую название параметра (например @name)и значение параметра (например "John Иванович Smith")
        public void ExecSQL(string sqlCommand, Dictionary<string, string> MySQLParametersList) 
        {
            try
            {
                MySqlCommand cmd = new MySqlCommand(sqlCommand, conn);
                cmd.CommandType = CommandType.Text;

                //Перебираем коллекцию параметров и добавляем каждый 
                foreach (KeyValuePair<string, string> MySQLParameter in MySQLParametersList)
                {
                    //аргументы - название параметра, тип данных в поле SQL, длинна строки. Value - соответсвенно значение.
                cmd.Parameters.Add(new MySqlParameter(MySQLParameter.Key, MySqlDbType.VarChar, 255) { Value = MySQLParameter.Value });                                                          
                }
                
                cmd.ExecuteNonQuery();
            }
            catch { MessageBox.Show("Error to executing Mysql Command"); }
        }


        public void ConnectionClose() //Метод для зарытия соединения
        {
            try
            {
                if (conn.State != ConnectionState.Closed)
                {
                    conn.Close(); //Закрываем соединение                   
                }
            }
            catch { MessageBox.Show("Error to close MySql Connection"); }
        }
           
    }
}
