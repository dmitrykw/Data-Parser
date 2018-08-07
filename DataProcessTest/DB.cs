using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.OleDb;
using MySql.Data.MySqlClient;
using System.Windows;
using System.Collections;


namespace DataProcessTest
{

    class DB
    {

        public static event Action<long> ProcessChanged; //Создаем событие для измения прогресса из потока
        private static bool _cancelled = false; //Объявляем переменную для отслеживания события отмены работы потока        

        public void Cancel() // Создаем метод для отмены операции
        {
            _cancelled = true;
        }



        //Метод извлечения из базы данных - принимает название таблицы в Access и путь к файлу, возвращает datatable
        public async Task<DataTable> GetDatatableFromAccessAsync(string TableName, string filePath)
        {
            return await Task.Run(() =>
            {

                string connetionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + filePath + ";";//Строка подключения к BD

            try
            {
                OleDbConnection conn = new OleDbConnection(connetionString);// Создаем объект
                DataTable results = new DataTable(); //Создаем datatable в который запишем результат
                string sqlCommand = "SELECT * FROM " + TableName;
                OleDbCommand cmd = new OleDbCommand(sqlCommand, conn); //Создаем команду которой будем выполнять запрос // sqlCommand = "SELECT * FROM users" например

                conn.Open(); // Открываем соединиение

                OleDbDataAdapter adapter = new OleDbDataAdapter(cmd); // Создаем data adapter и записываем в него результат выполнеиния команды
                adapter.Fill(results); //Заполняет datatable results содержимым dataadapter а

                conn.Close(); //Закрываем соединение

                //Удаляем первый столбец - там ID шник
                results.Columns.RemoveAt(0);


                    int progress = 0;
                    //Инкрементируем прогрессбар
                    System.Threading.Thread.Sleep(1); //Задержка, чтобы не перегружать UI
                    progress += 0;
                    ProcessChanged(progress);   //Вызыывем событие ProcessChanged и передаем ему текущий прогресс
                    if (_cancelled)// Если флаг отмены true то прервать операцию
                    {
                        _cancelled = false;
                        ProcessChanged(0);   //Вызыывем событие ProcessChanged и передаем ему текущий прогресс
                        DataTable emptyDT = new DataTable();
                        return emptyDT;
                    }

                    return results; //Возвращаем резульат в функцию
            }
            catch
            {               
                DataTable results = new DataTable();
                return results;
            }
           });
        }


        
        //Метод извлечения из базы данных MySQL 
        public async Task<DataTable> GetDatatableFromMySQLAsync(SQLConnectionParams SqlConnParams)
        {
            return await Task.Run(() =>
            {
                string conString = @"Data Source=" + SqlConnParams.hostname + ";port=3306;Initial Catalog=" + SqlConnParams.database + ";User Id=" + SqlConnParams.user + ";password=" + SqlConnParams.passwd + "";
                try
                {
                    MySqlConnection conn = new MySqlConnection(conString);

                    MySqlCommand cmd = new MySqlCommand("SELECT * FROM " + SqlConnParams.tablename, conn);

                    cmd.CommandType = CommandType.Text;
                    MySqlDataAdapter sda = new MySqlDataAdapter(cmd);

                    DataTable results = new DataTable();


                    sda.Fill(results);

                    //Удаляем первый столбец - там ID шник
                    results.Columns.RemoveAt(0);


                    int progress = 0;
                    //Инкрементируем прогрессбар
                    System.Threading.Thread.Sleep(1); //Задержка, чтобы не перегружать UI
                    progress += 0;
                    ProcessChanged(progress);   //Вызыывем событие ProcessChanged и передаем ему текущий прогресс
                    if (_cancelled)// Если флаг отмены true то прервать операцию
                    {
                        _cancelled = false;
                        ProcessChanged(0);   //Вызыывем событие ProcessChanged и передаем ему текущий прогресс
                        DataTable emptyDT = new DataTable();
                        return emptyDT;
                    }

                    return results;
                }
                catch
                {
                    DataTable results = new DataTable();
                    return results;
                }


            });
        }

        

        //Метод сохранения в базу данных - принимает TQSL команду и путь к файлу
        public async Task<int> SaveDataTableToAccessAsync(DataTable DataTable, string TableName, string filePath)
            {
            return await Task.Run(() =>
            {
                //Cоздаем экземпляр класса для работы с Access
                AccessExecuter AceessExec = new AccessExecuter(filePath);
                AceessExec.ConnectionOpen(); //Окрываем соединеие

                //====ФОРМИРУЕМ ЗАГОЛОВКИ================
                //Формируем SQL строку CREATE TABLE
                int ColumnsCount = DataTable.Columns.Count;

            string SQLstringTail = "";
            for (int i = 0; i < ColumnsCount; i++)
            {
                string ColumnName = DataTable.Columns[i].ColumnName;

                ColumnName = CheckWord(ColumnName); //Проверяем корректность слов и редактируем проблемыне, вырезаем пробелы и спецсимволы, чтобы БД не ругалась.

                SQLstringTail = SQLstringTail + ", " + ColumnName + " Text(255)";
            }
            string SQLstring = "CREATE TABLE " + TableName + " (ID COUNTER CONSTRAINT PrimaryKey PRIMARY KEY" + SQLstringTail + ")";


            AceessExec.ExecSQL(SQLstring);//Выполняем команду SQL
                                             //КОНЕЦ====ФОРМИРУЕМ ЗАГОЛОВКИ================

                //========ФОРМИРУЕМ ЗНАЧЕННИЯ===========

                //Формируем SQL строку INSERT


                //Переменная для оптимизации прогреса - чтобы обновляеть интерфейс не на каждой итерации цикла, а раз в N раз
                int SkipsCounter = 0;                

                
          
                int devider = 5; //Переменная указывает в дальнейшем коде раз в какое число бы будем обновляем прогресс бар. Чтобы обращаться к интерфейсу как можно реже и не обновлять его слишком часто при загрузке очень большого кол-ва строк.
                if (DataTable.Rows.Count > 500 & DataTable.Rows.Count < 15000) //Если строк больше 500 но меньше 15000
                { devider = 10; }
                else if (DataTable.Rows.Count > 15000) // Если файл больше 15000
                { devider = 20; }
               




                int progress = 0;
            foreach (DataRow row in DataTable.Rows)//Перибераем строки
            {

                SQLstring = "INSERT INTO " + TableName + "(";
                string SQLstringColumnsTail = "";

                foreach (var Column in DataTable.Columns) //Перебираем колонки, чтобы сформировать список полей для SQL строки
                {
                    string ColumnName = Column.ToString();
                    ColumnName = CheckWord(ColumnName); //Проверяем слова

                    SQLstringColumnsTail = SQLstringColumnsTail + ColumnName + ", ";
                }
                SQLstringColumnsTail = SQLstringColumnsTail.Remove(SQLstringColumnsTail.Length - 2, 2);
                SQLstring = SQLstring + SQLstringColumnsTail + ") values("; 

                string SQLstringValuesTail = "";


                    // Для парамертризации sql запросов создаем коллекцию параметров типа ключ значение.
                     //Эта коллекция будет содержать список ключ значение соответвующее параметру со знаком @ в строке INSERT и значению реально требующемуся для записи в базу данных, тоесть содержимому ячейки из Datatable
                     Dictionary<string, string> AccessOLEDBParametersList = new Dictionary<string, string>();



                foreach (var Column in DataTable.Columns) //Перебираем колонки, чтобы сформировать значений для SQL строки (значения колоной используем в качестве индекса для Row)
                {
                    string ColumnName = Column.ToString();

                        // В качестве значения добавляем название колонки со знаком @, чтобы конечная срока SQL выглядела примерно так : INSERT INTO tablename (Column1, Column2) VALUES (@Column1, @Column2)
                        SQLstringValuesTail = SQLstringValuesTail + "@" + ColumnName + ", ";


                        //Добавим к коллекции параметров название колонки ( @Column1 ) в качестве ключа Key , и содержимое ячейеки в качестве значения Value row[ColumnName].ToString()
                        AccessOLEDBParametersList.Add("@" + ColumnName, row[ColumnName].ToString());
                

                    }
                SQLstringValuesTail = SQLstringValuesTail.Remove(SQLstringValuesTail.Length - 2, 2);
                SQLstring = SQLstring + SQLstringValuesTail + ")";

                    //Выполняем команду SQL
                    AceessExec.ExecSQL(SQLstring, AccessOLEDBParametersList);//Выполняем команду SQL





                    //Инкрементируем прогрессбар

                    if (SkipsCounter <= devider) //Если счетчик пропусков меньше либо равен заданному значению числа пропусков
                    {                        
                        SkipsCounter++; //Инкрементируем счетчик
                    }
                    else //Если счетчик достиг большего значения
                    {
                        System.Threading.Thread.Sleep(1); //Задержка для освобождения UI
                        SkipsCounter++; //Инкрементируем счетчик                        
                        progress += SkipsCounter; //Добавляем к прогресу длинну текущей агрегатора линий
                        ProcessChanged(progress);   //Вызыывем событие ProcessChanged и передаем ему текущий прогресс
                        //Счетчик пропусков               
                        SkipsCounter = 0;
                    }


                 

                    if (_cancelled)// Если флаг отмены true то прервать операцию
                    {
                        _cancelled = false;
                        ProcessChanged(0);   //Вызыывем событие ProcessChanged и передаем ему текущий прогресс
                        AceessExec.ConnectionClose();//Закрываем соединение
                        return 0;
                    }


                }

                AceessExec.ConnectionClose();//Закрываем соединение
                //КОНЕЦ========ФОРМИРУЕМ ЗНАЧЕННИЯ===========
               
                return progress + SkipsCounter;
            });

            }




        public async Task<int> SaveDataTableToMySQLAsync(DataTable DataTable, SQLConnectionParams SqlConnParams)
        {
            return await Task.Run(() =>
            {

                //Cоздаем экземпляр класса для работы с Mysql
                MySqlExecuter MySqlExec = new MySqlExecuter(SqlConnParams);
                MySqlExec.ConnectionOpen(); //Окрываем соединеие

                MySqlExec.ExecSQL("SET AUTOCOMMIT=0;");  //Включаем в базе данных ручной коммит транзакций
                MySqlExec.ExecSQL("START TRANSACTION;"); // Начинаем транзакцию

                //====ФОРМИРУЕМ ЗАГОЛОВКИ================
                //Формируем SQL строку CREATE TABLE
                int ColumnsCount = DataTable.Columns.Count;

            string SQLstringTail = "";
            for (int i = 0; i < ColumnsCount; i++)
            {
                string ColumnName = DataTable.Columns[i].ColumnName;

                ColumnName = CheckWord(ColumnName); //Проверяем корректность слов и редактируем проблемыне, вырезаем пробелы и спецсимволы, чтобы БД не ругалась.

                SQLstringTail = SQLstringTail + ", " + ColumnName + " Text(255)";
            }
            string SQLstring = "CREATE TABLE IF NOT EXISTS " + SqlConnParams.tablename + " (id int(11) NOT NULL PRIMARY KEY auto_increment" + SQLstringTail + ")";


                //Выполняем команду SQL
                MySqlExec.ExecSQL(SQLstring); // Выполняем команду CREATE TABLE
                MySqlExec.ExecSQL("COMMIT;"); // Завершаем транзакцию
                //КОНЕЦ====ФОРМИРУЕМ ЗАГОЛОВКИ================



                //========ФОРМИРУЕМ ЗНАЧЕННИЯ===========
                MySqlExec.ExecSQL("START TRANSACTION;"); //Открываем новую транзакцию
                //Формируем SQL строку INSERT

                //Переменная для оптимизации прогреса - чтобы обновляеть интерфейс не на каждой итерации цикла, а раз в N раз
                int SkipsCounter = 0;

                int devider = 5; //Переменная указывает в дальнейшем коде раз в какое число бы будем обновляем прогресс бар. Чтобы обращаться к интерфейсу как можно реже и не обновлять его слишком часто при загрузке очень большого кол-ва строк.
                if (DataTable.Rows.Count > 500 & DataTable.Rows.Count < 15000) //Если строк больше 500 но меньше 15000
                { devider = 20; }
                else if (DataTable.Rows.Count > 15000) // Если файл больше 15000
                { devider = 100; }
             


                int progress = 0;
            foreach (DataRow row in DataTable.Rows)//Перибераем строки
            {             

                SQLstring = "INSERT INTO " + SqlConnParams.tablename + "(";
                string SQLstringColumnsTail = "";

                foreach (var Column in DataTable.Columns) //Перебираем колонки, чтобы сформировать список полей для SQL строки
                {
                    string ColumnName = Column.ToString();
                    ColumnName = CheckWord(ColumnName); //Проверяем слова

                    SQLstringColumnsTail = SQLstringColumnsTail + ColumnName + ", ";
                }
                SQLstringColumnsTail = SQLstringColumnsTail.Remove(SQLstringColumnsTail.Length - 2, 2);
                SQLstring = SQLstring + SQLstringColumnsTail + ") values("; 

                string SQLstringValuesTail = "";


                    //Для парамертризации sql запросов создаем коллекцию параметров типа ключ значение. 
                    //Эта коллекция будет содержать список ключ значение соответвующее параметру со знаком @ в строке INSERT и значению реально требующемуся для записи в базу данных, тоесть содержимому ячейки из Datatable
                    Dictionary<string, string> MySQLParametersList = new Dictionary<string, string>();


                foreach (var Column in DataTable.Columns) //Перебираем колонки, чтобы сформировать значений для SQL строки (значения колоной используем в качестве индекса для Row)
                {
                    string ColumnName = Column.ToString();

                        // В качестве значения добавляем название колонки со знаком @, чтобы конечная срока SQL выглядела примерно так : INSERT INTO tablename (Column1, Column2) VALUES (@Column1, @Column2)
                        SQLstringValuesTail = SQLstringValuesTail  + "@" + ColumnName + ", ";

                        //Добавим к коллекции параметров название колонки ( @Column1 ) в качестве ключа Key , и содержимое ячейеки в качестве значения Value row[ColumnName].ToString()
                        MySQLParametersList.Add("@" + ColumnName, row[ColumnName].ToString());

                    }
                SQLstringValuesTail = SQLstringValuesTail.Remove(SQLstringValuesTail.Length - 2, 2);
                SQLstring = SQLstring + SQLstringValuesTail + ")";

                   
                    //Выполняем команду SQL
                    MySqlExec.ExecSQL(SQLstring, MySQLParametersList); //Выполняем команду INSERT

                    //Инкрементируем прогрессбар
                    
                    if (SkipsCounter <= devider) //Если счетчик пропусков меньше либо равен заданному значению числа пропусков
                    {
                        SkipsCounter++; //Инкрементируем счетчик
                    }
                    else //Если счетчик достиг большего значения
                    {
                        System.Threading.Thread.Sleep(1); //Задержка для освобождения UI
                        SkipsCounter++; //Инкрементируем счетчик                        
                        progress += SkipsCounter; //Добавляем к прогресу длинну текущей агрегатора линий
                        ProcessChanged(progress);   //Вызыывем событие ProcessChanged и передаем ему текущий прогресс
                        //Счетчик пропусков               
                        SkipsCounter = 0;
                    }

                    
                    if (_cancelled)// Если флаг отмены true то прервать операцию
                    {
                        _cancelled = false;
                        ProcessChanged(0);   //Вызыывем событие ProcessChanged и передаем ему текущий прогресс
                        MySqlExec.ConnectionClose();//Закрываем соединение
                        return 0;
                    }

                }

                MySqlExec.ExecSQL("COMMIT;"); //Завершаем транзакцию

                MySqlExec.ConnectionClose();//Закрываем соединение
                                            //КОНЕЦ========ФОРМИРУЕМ ЗНАЧЕННИЯ===========


                return progress + SkipsCounter;

            });

        }



        
        public async Task<int> SaveToExistingTableAsync(DataTable DataTable, SQLConnectionParams SqlConnParams)
        {
            return await Task.Run(() =>
            {

                //Cоздаем экземпляр класса для работы с Mysql
                MySqlExecuter MySqlExec = new MySqlExecuter(SqlConnParams);
                MySqlExec.ConnectionOpen(); //Окрываем соединеие

                MySqlExec.ExecSQL("SET AUTOCOMMIT=0;");  //Включаем в базе данных ручной коммит транзакций
                MySqlExec.ExecSQL("START TRANSACTION;"); // Начинаем транзакцию

                string SQLstring = "";

                //========ФОРМИРУЕМ ЗНАЧЕННИЯ===========

                //Формируем SQL строку INSERT

                //Переменная для оптимизации прогреса - чтобы обновляеть интерфейс не на каждой итерации цикла, а раз в N раз
                int SkipsCounter = 0;

                int devider = 5; //Переменная указывает в дальнейшем коде раз в какое число бы будем обновляем прогресс бар. Чтобы обращаться к интерфейсу как можно реже и не обновлять его слишком часто при загрузке очень большого кол-ва строк.
                if (DataTable.Rows.Count > 500 & DataTable.Rows.Count < 15000) //Если строк больше 500 но меньше 15000
                { devider = 20; }
                else if (DataTable.Rows.Count > 15000) // Если файл больше 15000
                { devider = 100; }
              




                int progress = 0;
            foreach (DataRow row in DataTable.Rows)//Перибераем строки
            {

                SQLstring = "INSERT INTO " + SqlConnParams.tablename + "(";
                string SQLstringColumnsTail = "";

                foreach (var Column in DataTable.Columns) //Перебираем колонки, чтобы сформировать список полей для SQL строки
                {
                    string ColumnName = Column.ToString();
                    ColumnName = CheckWord(ColumnName); //Проверяем слова

                    SQLstringColumnsTail = SQLstringColumnsTail + ColumnName + ", ";
                }
                SQLstringColumnsTail = SQLstringColumnsTail.Remove(SQLstringColumnsTail.Length - 2, 2);
                SQLstring = SQLstring + SQLstringColumnsTail + ") values(";

                string SQLstringValuesTail = "";

                    //Для парамертризации sql запросов создаем коллекцию параметров типа ключ значение. 
                    //Эта коллекция будет содержать список ключ значение соответвующее параметру со знаком @ в строке INSERT и значению реально требующемуся для записи в базу данных, тоесть содержимому ячейки из Datatable
                    Dictionary<string, string> MySQLParametersList = new Dictionary<string, string> ();                    

                foreach (var Column in DataTable.Columns) //Перебираем колонки, чтобы сформировать значений для SQL строки (значения колоной используем в качестве индекса для Row)
                {
                    string ColumnName = Column.ToString();

                        // В качестве значения добавляем название колонки со знаком @, чтобы конечная срока SQL выглядела примерно так : INSERT INTO tablename (Column1, Column2) VALUES (@Column1, @Column2)
                        SQLstringValuesTail = SQLstringValuesTail + "@" + ColumnName + ", ";

                        //Добавим к коллекции параметров название колонки ( @Column1 ) в качестве ключа Key , и содержимое ячейеки в качестве значения Value row[ColumnName].ToString()
                        MySQLParametersList.Add("@" + ColumnName, row[ColumnName].ToString());
                }
                SQLstringValuesTail = SQLstringValuesTail.Remove(SQLstringValuesTail.Length - 2, 2);
                SQLstring = SQLstring + SQLstringValuesTail + ")";


                    //Выполняем команду SQL - передаем строку SQL и список параметров для параметризированного запроса
                    MySqlExec.ExecSQL(SQLstring, MySQLParametersList);



                    //Инкрементируем прогрессбар

                    if (SkipsCounter <= devider) //Если счетчик пропусков меньше либо равен заданному значению числа пропусков
                    {
                        SkipsCounter++; //Инкрементируем счетчик
                    }
                    else //Если счетчик достиг большего значения
                    {
                        System.Threading.Thread.Sleep(1); //Задержка для освобождения UI
                        SkipsCounter++; //Инкрементируем счетчик                        
                        progress += SkipsCounter; //Добавляем к прогресу длинну текущей агрегатора линий
                        ProcessChanged(progress);   //Вызыывем событие ProcessChanged и передаем ему текущий прогресс
                        //Счетчик пропусков               
                        SkipsCounter = 0;
                    }


                    


                    if (_cancelled)// Если флаг отмены true то прервать операцию
                    {
                        _cancelled = false;
                        ProcessChanged(0);   //Вызыывем событие ProcessChanged и передаем ему текущий прогресс
                        MySqlExec.ConnectionClose();//Закрываем соединение
                        return 0;
                    }
                }

                MySqlExec.ExecSQL("COMMIT;"); // Завершаем транзакцию


                MySqlExec.ConnectionClose();//Закрываем соединение
              //КОНЕЦ========ФОРМИРУЕМ ЗНАЧЕННИЯ===========


            
                return progress + SkipsCounter;

            });


        }

      


        //Проверка слов на наличие зарезервированых знаков и ошибок, которые не позволят использовать эти слова в полях БД
        public string CheckWord(string Word)
        {
            string[] RestrictedWords = new string[] { "-A", "ADD", "ALL", "Alphanumeric", "ALTER", "AND", "ANY", "Application", "AS", "ASC", "Assistant", "AUTOINCREMENT", "Avg", "-B", "BETWEEN", "BINARY", "BIT", "BOOLEAN", "BY", "BYTE", "-C", "CHAR, CHARACTER", "COLUMN", "CompactDatabase", "CONSTRAINT", "Container", "Count", "COUNTER", "CREATE", "CreateDatabase", "CreateField", "CreateGroup", "CreateIndex", "CreateObject", "CreateProperty", "CreateRelation", "CreateTableDef", "CreateUser", "CreateWorkspace", "CURRENCY", "CurrentUser", "-D", "DATABASE", "DATE", "DATETIME", "DELETE", "DESC", "Description", "DISALLOW", "DISTINCT", "DISTINCTROW", "Document", "DOUBLE", "DROP", "-E", "Echo", "Else", "End", "Eqv", "Error", "EXISTS", "Exit", "-F", "FALSE", "Field, Fields", "FillCache", "FLOAT, FLOAT4, FLOAT8", "FOREIGN", "Form, Forms", "FROM", "Full", "FUNCTION", "-G", "GENERAL", "GetObject", "GetOption", "GotoPage", "GROUP", "GROUP BY", "GUID", "-H", "HAVING", "-I", "Idle", "IEEEDOUBLE, IEEESINGLE", "If", "IGNORE", "Imp", "IN", "INDEX", "Index, Indexes", "INNER", "INSERT", "InsertText", "INT, INTEGER, INTEGER1, INTEGER2, INTEGER4", "INTO", "IS", "-J", "JOIN", "-K", "KEY", "-L", "LastModified", "LEFT", "Level", "Like", "LOGICAL, LOGICAL1", "LONG, LONGBINARY, LONGTEXT", "-M", "Macro", "Match", "Max, Min, Mod", "MEMO", "Module", "MONEY", "Move", "-N", "NAME", "NewPassword", "NO", "Not", "Note", "NULL", "NUMBER, NUMERIC", "-O", "Object", "OLEOBJECT", "OFF", "ON", "OpenRecordset", "OPTION", "OR", "ORDER", "Orientation", "Outer", "OWNERACCESS", "-P", "Parameter", "PARAMETERS", "Partial", "PERCENT", "PIVOT", "PRIMARY", "PROCEDURE", "Property", "-Q", "Queries", "Query", "Quit", "-R", "REAL", "Recalc", "Recordset", "REFERENCES", "Refresh", "RefreshLink", "RegisterDatabase", "Relation", "Repaint", "RepairDatabase", "Report", "Reports", "Requery", "RIGHT", "-S", "SCREEN", "SECTION", "SELECT", "SET", "SetFocus", "SetOption", "SHORT", "SINGLE", "SMALLINT", "SOME", "SQL", "StDev, StDevP", "STRING", "Sum", "-T", "TABLE", "TableDef, TableDefs", "TableID", "TEXT", "TIME, TIMESTAMP", "TOP", "TRANSFORM", "TRUE", "Type", "-U", "UNION", "UNIQUE", "UPDATE", "USER", "-V", "VALUE", "VALUES", "Var, VarP", "VARBINARY, VARCHAR", "-W", "WHERE", "WITH", "Workspace", "-X", "Xor", "-Y", "Year", "YES", "YESNO", "-A", "ABSOLUTE", "ACTION", "ADD", "ADMINDB", "ALL", "ALLOCATE", "ALPHANUMERIC", "ALTER", "AND", "ANY", "ARE", "AS", "ASC", "ASSERTION", "AT", "AUTHORIZATION", "AUTOINCREMENT", "AVG", "-B", "BAND", "BEGIN", "BETWEEN", "BINARY", "BIT", "BIT_LENGTH", "BNOT", "BOR", "BOTH", "BXOR", "BY", "BYTE", "-C", "CASCADE", "CASCADED", "CASE", "CAST", "CATALOG", "CHAR", "CHARACTER", "CHAR_LENGTH", "CHARACTER_LENGTH", "CHECK", "CLOSE", "COALESCE", "COLLATE", "COLLATION", "COLUMN", "COMMIT", "COMP", "COMPRESSION", "CONNECT", "CONNECTION", "CONSTRAINT", "CONSTRAINTS", "CONTAINER", "CONTINUE", "CONVERT", "CORRESPONDING", "COUNT", "COUNTER", "CREATE", "CREATEDB", "CROSS", "CURRENCY", "CURRENT", "CURRENT_DATE", "CURRENT_TIME", "CURRENT_TIMESTAMP", "CURRENT_USER", "CURSOR", "-D", "DATABASE", "DATE", "DATETIME", "DAY", "DEALLOCATE", "DEC", "DECIMAL", "DECLARE", "DEFAULT", "DEFERRABLE", "DEFERRED", "DELETE", "DESC", "DESCRIBE", "DESCRIPTOR", "DIAGNOSTICS", "DISALLOW", "DISCONNECT", "DISTINCT", "DOMAIN", "DOUBLE", "DROP", "-E", "ELSE", "END", "END-EXEC", "ESCAPE", "EXCEPT", "EXCEPTION", "EXCLUSIVECONNECT", "EXEC", "EXECUTE", "EXISTS", "EXTERNAL", "EXTRACT", "-F", "FALSE", "FETCH", "FIRST", "FLOAT", "FLOAT4", "FLOAT8", "FOR", "FOREIGN", "FOUND", "FROM", "FULL", "-G", "GENERAL", "GET", "GLOBAL", "GO", "GOTO", "GRANT", "GROUP", "GUID", "-H", "HAVING", "HOUR", "-I", "IDENTITY", "IEEEDOUBLE", "IEEESINGLE", "IGNORE", "IMAGE", "IMMEDIATE", "ININDEX", "INDICATOR", "INHERITABLE", "INITIALLY", "INNER", "INPUT", "INSENSITIVE", "INSERT", "INT", "INTEGER", "INTEGER1", "INTEGER2", "INTEGER4", "INTERSECT", "INTERVAL", "INTO", "IS", "ISOLATION", "-J", "JOIN", "-K", "KEY", "-L", "LANGUAGE", "LAST", "LEADING", "LEFT", "LEVEL", "LIKE", "LOCAL", "LOGICAL", "LOGICAL1", "LONG", "LONGBINARY", "LONGCHAR", "LONGTEXT", "LOWER", "-M", "MATCH", "MAX", "MEMO", "MIN", "MINUTE", "MODULE", "MONEY", "MONTH", "-N", "NAMES", "NATIONAL", "NATURAL", "NCHAR", "NEXT", "NO", "NOT", "NOTE", "NULL", "NULLIF", "NUMBER", "NUMERIC", "-O", "OBJECT", "OCTET_LENGTH", "OFOLEOBJECT", "ONONLY", "OPEN", "OPTION", "ORORDER", "OUTER", "OUTPUT", "OVERLAPS", "OWNERACCESS", "-P", "PAD", "PARAMETERS", "PARTIAL", "PASSWORD", "PERCENT", "PIVOT", "POSITION", "PRECISION", "PREPARE", "PRESERVE", "PRIMARY", "PRIOR", "PRIVILEGES", "PROC", "PROCEDURE", "PUBLIC", "-Q", "-R", "READ", "REAL", "REFERENCES", "RELATIVE", "RESTRICT", "REVOKE", "RIGHT", "ROLLBACK", "ROWS", "-S", "SCHEMA", "SCROLL", "SECOND", "SECTION", "SELECT", "SELECTSCHEMA", "SELECTSECURITY", "SESSION", "SESSION_USER", "SET", "SHORT", "SINGLE", "SIZE", "SMALLINT", "SOME", "SPACE", "SQL", "SQLCODE", "SQLERROR", "SQLSTATE", "STRING", "SUBSTRING", "SUM", "SYSTEM_USER", "-T", "TABLE", "TABLEID", "TEMPORARY", "TEXT", "THEN", "TIME", "TIMESTAMP", "TIMEZONE_HOUR", "TIMEZONE_MINUTE", "TO", "TOP", "TRAILING", "TRANSACTION", "TRANSFORM", "TRANSLATE", "TRANSLATION", "TRIM", "TRUE", "-U", "UNION", "UNIQUE", "UNIQUEIDENTIFIER", "UNKNOWN", "UPDATE", "UPDATEIDENTITY", "UPDATEOWNER", "UPDATESECURITY", "UPPER", "USAGE", "USER", "USING", "-V", "VALUE", "VALUES", "VARBINARY", "VARCHAR", "VARYING", "VIEW", "-W", "WHEN", "WHENEVER", "WHERE", "WITH", "WORK", "WRITE", "-X", "-Y", "YEAR", "YESNO", "-Z", "ZONE" };
            char[] RestrictedSymbols = new char[] { '.', '/', '*', ';', ':', '!', '#', '&', '-', '?', '\"', '\'', '$', '%' };
            string output = "";

            foreach (char symbol in RestrictedSymbols)
            {
                output = output.Replace(symbol, ' '); //Вырезаем символы из списка
            }
            output = Word.Replace(" ", ""); //Вырезаем пробелы           

            foreach (string RestrictedWord in RestrictedWords) //Если находим запрещенные слова приписываем к ним My
            {

                if (output.ToLower() == RestrictedWord.ToLower()) //Переведем всё в нижний регистр чтобы корректно сравнить
                {
                    output = "My" + output;
                }
                
            }


            return output;
        }
    }
}
