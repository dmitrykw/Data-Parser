using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.ComponentModel;
using System.IO;


namespace DataProcessTest
{
    class FillTable
    {
        public static DataTable FillFromCSV(string FilePath)
        {

            DataTable dt = new DataTable();


            //Читаем файл построчно и обрабатываем каждую строчку    
            const Int32 BufferSize = 512;

            using (var fileStream = File.OpenRead(FilePath))
            {
                using (StreamReader sr = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
                {

                    string Line;
                    bool IsFirstLine = true;
                    int progress = 0;

                    //Создаем объект для работы с БД
                    DB db = new DB();

                    //Две переменных для оптимизации прогреса - чтобы обновляеть интерфейс не на каждой итерации цикла, а раз в N раз
                    int SkipsCounter = 0;
                    int Lineaggregator = 0;

                    FileInfo fi = new FileInfo(FilePath);
                    int FileSize = (int)fi.Length;
                    int devider = 5; //Переменная указывает в дальнейшем коде раз в какое число бы будем обновляем прогресс бар. Чтобы обращаться к интерфейсу как можно реже и не обновлять его слишком часто при загрузке очень больших файлов.
                    if (FileSize > 1048576 & FileSize < 1073741824) //Если файл больше Мегабайта но меньше гигабайта
                    { devider = 100; }
                    else if (FileSize > 1073741824) // Если файл больше гигабайта
                    { devider = 10000; }


                    while (!sr.EndOfStream) //В этом цикле читаем построчно файл
                    {
                        Line = sr.ReadLine();

                        if (IsFirstLine == true) //Проверяем что эта первая строчка (сторчка заголовков)
                        {
                            //Разбиваем текст на массив слов
                            string[] SplittedColumns = Line.Split(new char[] { ';' });
                            //Удаляем пустые элементы массива
                            string[] Columns = SplittedColumns.Where(x => x != "").ToArray();


                            //Проверяем заголовки на корректность и если что корректируем
                            for (int n = 0; n < Columns.Count(); n++)
                            {
                                Columns[n] = db.CheckWord(Columns[n]);
                            }

                            //Формируем Datatable

                            dt.CaseSensitive = true; //Включаем регистрозависимость

                            foreach (string Column in Columns)
                            {
                                dt.Columns.Add(Column, typeof(string)); //Задаем названия столбцов и типы даннных
                            }

                            IsFirstLine = false;

                        }
                        else //Если нет, добавляем в строку
                        {



                            // Разбиваем текст на массив слов
                            string[] SplittedWords = Line.Split(new char[] { ';' });
                            //Удаляем пустые элементы массива
                            string[] Words = SplittedWords.Where(x => x != "").ToArray();



                            DataRow row = dt.NewRow(); //Добавляем новую строку в datatable


                            for (int i = 0; i < Words.Count(); i++)
                            {
                                row[i] = Words[i];
                            }

                            dt.Rows.Add(row);

                            row = null;
                        }

                        //Инкрементируем прогрессбар
                        if (MainWindow.backgroundWorker != null) //Проверяем что воркер существует
                        {
                            if (SkipsCounter <= devider) //Если счетчик пропусков меньше либо равен заданному значению числа пропусков
                            {
                                Lineaggregator += Line.Length; //Инкрементируем Lineaggregator текущим значение длинны обрабатываемой строки
                                SkipsCounter++; //Инкрементируем счетчик
                            }
                            else //Если счетчик достиг большего значения
                            {
                                System.Threading.Thread.Sleep(1); //Задержка для освобождения UI
                                Lineaggregator += Line.Length; //Инкрементируем Lineaggregator текущим значение длинны обрабатываемой строки
                                progress += Lineaggregator; //Добавляем к прогресу длинну текущей агрегатора линий
                                MainWindow.backgroundWorker.ReportProgress(progress);
                                //Обнуляем агрегатор линий и счетчик пропусков
                                Lineaggregator = 0;
                                SkipsCounter = 0;
                            }

                            //Проверяем статус отмены
                            if (MainWindow.backgroundWorker.CancellationPending)
                            {
                                return null;   //Если запрошена отмена, возвращаем null.                         
                            }
                        }
                    }


                    dt.AcceptChanges();


                }
            }
            return dt;
        }

        public static DataTable FillFromAccess(string FileName)
        {
            DataTable dt = new DataTable();

            return dt;
        }

        public static DataTable FillFromSQL(string FileName)
        {
            DataTable dt = new DataTable();

            return dt;
        }

    }
}
