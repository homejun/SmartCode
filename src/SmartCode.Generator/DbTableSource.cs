using Microsoft.Extensions.Logging;
using SmartCode.Configuration;
using SmartCode.Db;
using SmartCode.Generator.Entity;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SmartCode.Generator
{
    public class DbTableSource : DbSource
    {
        public DbTableSource(
             Project project
             , ILoggerFactory loggerFactory
             , IPluginManager pluginManager
             ) : base(project, loggerFactory, pluginManager)
        {
        }

        public override string Name => "DbTable";
        public IEnumerable<Table> Tables { get; private set; }

        public override async Task InitData()
        {
            var dbTableRepository = new DbTableRepository(Project.DataSource, LoggerFactory);
            DbRepository = dbTableRepository;
            Tables = await dbTableRepository.QueryTable();
            var dbTypeConvert = PluginManager.Resolve<IDbTypeConverter>();
            foreach (var table in Tables)
            {
                foreach (var col in table.Columns)
                {
                    if ((DbRepository.DbProvider == Db.DbProvider.SQLite) && Project.Language == "CSharp")
                    {
                        if (col.DbType.IndexOf("(") > -1)
                        {
                            string[] arr = col.DbType.Split('(');
                            col.DbType = arr[0];
                        }
                        else
                            if (col.IsPrimaryKey == true)  //从 SQLite 的 2.3.4 版本开始，如果你将一个表中的一个字段声明为 INTEGER PRIMARY KEY，那么无论你何时向该表的该字段插入一个 NULL 值，这个 NULL 值将自动被更换为比表中该字段所有行的最大值大 1 的整数；如果表为空，那么将被更换为 1。
                        {
                            if (col.DbType == "integer")
                            {
                                col.AutoIncrement = true;
                            }

                        }
                    }
                    if ((DbRepository.DbProvider == Db.DbProvider.MySql || DbRepository.DbProvider == Db.DbProvider.MariaDB)
                        && col.DbType == "char"
                        && col.DataLength == 36
                        && Project.Language == "CSharp")
                    {
                        col.LanguageType = "Guid";
                    }
                    else
                    {
                        col.LanguageType = dbTypeConvert.LanguageType(DbRepository.DbProvider, Project.Language, col.DbType);
                    }
                }
            }
        }
    }
}
