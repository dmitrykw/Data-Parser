using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataProcessTest
{
    class SQLConnectionParams
    {
        public string tablename { get; set; }
        public string hostname { get; set; }
        public string database { get; set; }
        public string user { get; set; }
        public string passwd { get; set; }

        //Не хочу использовать здесь конструктор, так как в данной ситуации это будет не наглядно        
       // public SQLConnectionParams(string tablename, string hostname, string database, string user, string passwd)
       // {
       //     this.tablename = tablename;
       //    this.hostname = hostname;
       //     this.database = database;
       //     this.user = user;
       //     this.passwd = passwd;
       // }

    }
}
