using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.OleDb;
using System.Windows;

namespace DataProcessTest
{
    //Класс для работы с Access
    class AccessExecuter 
    {

        private string filePath;


        public AccessExecuter(string filePath) //В конуструкторе получаем путь к файлу
         {
            this.filePath = filePath;            
         }


        OleDbConnection conn = null; //Объявляем переменную нашего Connection



        public void ConnectionOpen() //Метод для открытия соединения
        {
            try
            {
                string connetionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + filePath + ";";//Строка подключения к BD
                conn = new OleDbConnection(connetionString);// Создаем объект

                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();// Открываем соединиение
                }
            }
            catch {MessageBox.Show("Error to estsablish the OLEDB Connection with Access database"); }
        }

        public void ExecSQL(string sqlCommand) //Метод для выполнения SQL команды
        {
            try
            {
                OleDbCommand cmd = new OleDbCommand(sqlCommand, conn); //Создаем команду которой будем выполнять запрос // sqlCommand = "SELECT * FROM users" например
                cmd.ExecuteNonQuery();
            }
            catch { MessageBox.Show("Error to executing OLEDB Command with Access database"); }
        }

        //Метод для выполнения SQL команды - перегруженный метод для параметризированных запросов. Принимаем коллекцию ключ значение содержащую название параметра (например @name)и значение параметра (например "John Иванович Smith")
        public void ExecSQL(string sqlCommand, Dictionary<string, string> AccessOLEDBParametersList) //Метод для выполнения SQL команды
        {
            try
            {
                OleDbCommand cmd = new OleDbCommand(sqlCommand, conn); //Создаем команду которой будем выполнять запрос // sqlCommand = "SELECT * FROM users" например
                cmd.CommandType = CommandType.Text;


                //Перебираем коллекцию параметров и добавляем каждый 
                foreach (KeyValuePair<string, string> OLEDBParameter in AccessOLEDBParametersList)
                {
                    //аргументы - название параметра, тип данных в поле SQL, длинна строки. Value - соответсвенно значение.
                    cmd.Parameters.Add(new OleDbParameter(OLEDBParameter.Key, OleDbType.VarChar, 255) { Value = OLEDBParameter.Value });
                }

                cmd.ExecuteNonQuery();
            }
            catch { MessageBox.Show("Error to executing OLEDB Command with Access database"); }
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
            catch { MessageBox.Show("Error to close OLEDB Connection with Access database"); }
        }
         
                       
            
    }
}
