# Data-Parser
Parsing CSV Files, making tables, export/import database

Приложение умеет загружать произвольные данные из csv файла. С разделителем ';' . С любым кол-вом столбцов и строк.

В качестве заголовков использует первую строку.

Формирует Datatable в качестве источника данных для DataGrid

Умеет выгружать и загружать обратно данные в MSAccess и MySql, создавая новую таблицу с соответствующими заголовками или дописывая в существующую.

Так же приложение при формировании заголовков таблиц фильтрует заголовки из csv файла на предмет их соответствия зарезервированым словам и символам баз данных, запрещенных для использования в заголовках и корректирует их при необходимости.

Таблица имеет фильтр, который проверяет соответсвтие текста в текстбоксе любой ячейке таблицы, и фильтрует таблицу на предмет строки содержащей соответствующую ячейку.

Экспортировать в базу данных так же можно отфильтрованные данные.
