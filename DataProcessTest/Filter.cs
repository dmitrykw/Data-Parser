using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace DataProcessTest
{
    class Filter
    {

        public async Task<DataTable> FilterTableByWordAsync(DataTable inputDataTable , string InputWord)
        {
            return await Task.Run(() =>
            {
                DataTable Filtered_dt = new DataTable(); // Создаем новый дататейбл для отфильтрованных данных

            foreach (var Column in inputDataTable.Columns) //Заполняем в нем Columns из старого
            {
                Filtered_dt.Columns.Add(Column.ToString());
            }

            foreach (DataRow row in inputDataTable.Rows) //Перебираем строки
            {
                foreach (var Column in inputDataTable.Columns) //В них перебираем ячейки
                {


                    string ColumnName = Column.ToString(); //Получаем имя ячейки чтобы использовать его в как индекс строки
                   

                    if (InputWord.ToLower() == row[ColumnName].ToString().ToLower())  //Сравниваем содержимое ячейки с текстбоксом
                    {


                        Filtered_dt.Rows.Add(row.ItemArray); //Если оно совпало добавляем текущую строку в дататейбл

                        Filtered_dt.AcceptChanges();

                    }


                }
            }

                 return Filtered_dt;

            });

        }

    }
}
