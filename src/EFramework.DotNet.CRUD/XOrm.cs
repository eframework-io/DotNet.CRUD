// Copyright (c) 2025 EFramework Innovation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using EFramework.DotNet.Utility;
using SqlSugar;

namespace EFramework.DotNet.CRUD
{
    /// <summary>
    /// XOrm 拓展了 SqlSugar 的 ORM 功能，同时实现了基于上下文的事务机制，提高了数据操作的效率。
    /// </summary>
    /// <remarks>
    /// 功能特性
    /// - 多源配置：通过首选项中的配置自动初始化数据库连接
    /// - 事务操作：基于上下文的事务机制，支持缓存和并发控制
    /// 
    /// 使用手册
    /// 1. 初始化
    /// 
    /// 1.1 单数据库
    /// 
    /// 使用标准的 ADO.NET 连接字符串格式配置数据库：
    /// 
    /// <code>
    /// var preferences = new XPrefs.IBase();
    /// var config = new XPrefs.IBase();
    /// config.Set(XOrm.Preferences.Addr, "server=localhost;port=3306;database=mysql;uid=root;pwd=123456;");
    /// preferences.Set("XOrm/Source/MySQL/myalias", config);
    /// XOrm.Initialize(preferences);
    /// </code>
    /// 
    /// 1.2 多数据库
    /// 
    /// 支持配置多个数据库连接：
    /// 
    /// <code>
    /// var preferences = new XPrefs.IBase();
    /// var config1 = new XPrefs.IBase();
    /// config1.Set(XOrm.Preferences.Addr, "server=localhost;port=3306;database=mysql;uid=root;pwd=123456;");
    /// preferences.Set("XOrm/Source/MySQL/myalias1", config1);
    /// var config2 = new XPrefs.IBase();
    /// config2.Set(XOrm.Preferences.Addr, "server=localhost;port=3306;database=mysql;uid=root;pwd=123456;");
    /// preferences.Set("XOrm/Source/MySQL/myalias2", config2);
    /// XOrm.Initialize(preferences);
    /// </code>
    /// 
    /// 1.3 DSN 格式
    /// 
    /// 支持 DSN 格式的连接字符串，会自动转换为 ADO.NET 格式：
    /// 
    /// <code>
    /// var preferences = new XPrefs.IBase();
    /// var config = new XPrefs.IBase();
    /// config.Set(XOrm.Preferences.Addr, "root:123456@tcp(127.0.0.1:3306)/mysql?charset=utf8mb4&amp;loc=Local");
    /// preferences.Set("XOrm/Source/MySQL/myalias", config);
    /// XOrm.Initialize(preferences);
    /// // 会自动转换为：server=127.0.0.1;port=3306;database=mysql;uid=root;pwd=123456;
    /// </code>
    /// 
    /// 2. 上下文
    /// 
    /// 2.1 开始
    /// 
    /// 使用 Watch 开始一个上下文，所有后续操作将在同一个事务中执行：
    /// 
    /// <code>
    /// XOrm.Watch();
    /// </code>
    /// 
    /// 2.2 结束
    /// 
    /// 使用 Defer 结束上下文并提交事务：
    /// 
    /// <code>
    /// XOrm.Defer();
    /// </code>
    /// 
    /// 上下文会自动记录所有 CRUD 操作的 SQL 语句和耗时信息。
    /// 
    /// 3. 插入数据
    /// 
    /// 3.1 单个对象
    /// 
    /// <code>
    /// var model = new MyModel()
    /// {
    ///     IntVal = 1,
    ///     FloatVal = 1.0f,
    ///     StringVal = "model1",
    ///     BoolVal = true
    /// };
    /// var affect = XOrm.Insert&lt;MyModel&gt;(model);
    /// </code>
    /// 
    /// 3.2 对象列表
    /// 
    /// <code>
    /// var models = new List&lt;MyModel&gt;()
    /// {
    ///     new MyModel() { IntVal = 1, StringVal = "model1" },
    ///     new MyModel() { IntVal = 2, StringVal = "model2" }
    /// };
    /// var affect = XOrm.Insert&lt;MyModel&gt;(models);
    /// </code>
    /// 
    /// 3.3 自定义操作
    /// 
    /// <code>
    /// var affect = XOrm.Insert&lt;MyModel&gt;(model, insertable =&gt; 
    /// {
    ///     insertable.IgnoreColumns(e =&gt; new { e.ID });
    /// });
    /// </code>
    /// 
    /// 4. 查询数据
    /// 
    /// 4.1 条件查询
    /// 
    /// <code>
    /// var model1 = XOrm.Query&lt;MyModel&gt;(queryable =&gt; 
    ///     queryable.Where(e =&gt; e.ID == 1)
    /// ).Where(e =&gt; e.StringVal == "model1").First();
    /// </code>
    /// 
    /// 4.2 全量查询
    /// 
    /// <code>
    /// var models = XOrm.Query&lt;MyModel&gt;().ToList();
    /// </code>
    /// 
    /// 4.3 链式查询
    /// 
    /// <code>
    /// var models = XOrm.Query&lt;MyModel&gt;()
    ///     .Where(e =&gt; e.IntVal &gt; 100)
    ///     .OrderBy(e =&gt; e.ID)
    ///     .Take(10)
    ///     .ToList();
    /// </code>
    /// 
    /// 5. 更新数据
    /// 
    /// 5.1 条件更新
    /// 
    /// 更新指定字段：
    /// 
    /// <code>
    /// var model = XOrm.Query&lt;MyModel&gt;().Where(e =&gt; e.ID == 1).First();
    /// model.IntVal = -1;
    /// model.FloatVal = -1.0f;
    /// var affect = XOrm.Update&lt;MyModel&gt;(model, updateable =&gt;
    ///     updateable.UpdateColumns(e =&gt; new { e.IntVal, e.FloatVal })
    /// );
    /// </code>
    /// 
    /// 5.2 全量更新
    /// 
    /// 更新所有字段：
    /// 
    /// <code>
    /// var model = new MyModel() { ID = 2, FloatVal = -1.0f };
    /// var affect = XOrm.Update&lt;MyModel&gt;(model);
    /// </code>
    /// 
    /// 5.3 表达式更新
    /// 
    /// <code>
    /// var affect = XOrm.Update&lt;MyModel&gt;(
    ///     e =&gt; e.ID == 1,
    ///     updateable =&gt; updateable.SetColumns(e =&gt; new MyModel() 
    ///     { 
    ///         IntVal = 100,
    ///         StringVal = "updated"
    ///     })
    /// );
    /// </code>
    /// 
    /// 6. 删除数据
    /// 
    /// 6.1 实例删除
    /// 
    /// <code>
    /// var model = XOrm.Query&lt;MyModel&gt;().Where(e =&gt; e.ID == 1).First();
    /// var affect = XOrm.Delete&lt;MyModel&gt;(model);
    /// </code>
    /// 
    /// 6.2 条件删除
    /// 
    /// <code>
    /// var affect = XOrm.Delete&lt;MyModel&gt;(onDelete: deleteable =&gt;
    ///     deleteable.Where(e =&gt; e.ID == 2)
    /// );
    /// </code>
    /// 
    /// 6.3 主键删除
    /// 
    /// <code>
    /// var affect = XOrm.Delete&lt;MyModel&gt;(3);
    /// </code>
    /// 
    /// 6.4 全量删除
    /// 
    /// <code>
    /// var affect = XOrm.Delete&lt;MyModel&gt;();
    /// </code>
    /// 
    /// 更多信息请参考模块文档。
    /// </remarks>
    public partial class XOrm { }

    #region 结构体
    public partial class XOrm
    {
        /// <summary>
        /// Preferences 是 XOrm 的配置，用于存储当前的配置。
        /// </summary>
        internal class Preferences
        {
            /// <summary>
            /// Addr 是数据库的连接地址。
            /// </summary>
            internal const string Addr = "Addr";
        }

        /// <summary>
        /// Context 是 XOrm 的上下文。
        /// </summary>
        internal class Context
        {
            /// <summary>
            /// Cost 是上下文中的 CRUD 的耗时记录。
            /// </summary>
            internal class Cost
            {
                /// <summary>
                /// Sql 是 CRUD 操作的语句。
                /// </summary>
                internal string Sql;

                /// <summary>
                /// Executing 是 CRUD 操作的开始执行的时间。
                /// </summary>
                internal long Executing;

                /// <summary>
                /// Executed 是 CRUD 操作的执行完成的时间。
                /// </summary>
                internal long Executed;

                /// <summary>
                /// Reset 重置相关数据。
                /// </summary>
                internal void Reset()
                {
                    Sql = null;
                    Executing = 0;
                    Executed = 0;
                }
            }

            /// <summary>
            /// Initial 是上下文开始的时间。
            /// </summary>
            internal long Initial;

            /// <summary>
            /// Costs 是上下文中的 CRUD 操作耗时记录。
            /// </summary>
            internal List<Cost> Costs = new();

            /// <summary>
            /// Client 是上下文中的 SqlSugarClient 客户端。
            /// </summary>
            internal SqlSugarClient Client;

            /// <summary>
            /// Reset 重置上下文。
            /// </summary>
            internal void Reset()
            {
                Initial = 0;
                if (Costs.Count > 0)
                {
                    foreach (var cost in Costs)
                    {
                        cost.Reset();
                        XPool.Object<Context.Cost>.Put(cost);
                    }
                    Costs.Clear();
                }
                Client = null;
            }
        }
    }
    #endregion

    #region 初始化
    public partial class XOrm
    {
        /// <summary>
        /// Sources 是数据库连接配置的列表，用于存储数据库连接配置。
        /// </summary>
        public static readonly List<ConnectionConfig> Sources = new();

        /// <summary>
        /// InitializeLock 是初始化锁，用于防止多个线程同时初始化。
        /// </summary>
        internal static readonly object InitializeLock = new();

        /// <summary>
        /// XOrm 的静态构造函数，用于初始化 XOrm。
        /// </summary>
        static XOrm() { Initialize(XPrefs.Asset); }

        /// <summary>
        /// Initialize 初始化 XOrm。
        /// </summary>
        /// <param name="preferences">配置实例</param>
        internal static void Initialize(XPrefs.IBase preferences)
        {
            if (preferences == null) throw new ArgumentNullException(nameof(preferences));

            static string normalizeAddr(string addr, DbType type)
            {
                if (addr.Contains("server=") || addr.Contains("Server=")) return addr; // ADO.NET 风格的连接地址
                if (type == DbType.MySql)
                {
                    var pattern = @"^(?<user>[^:]+)(:(?<pass>[^@]+))?@tcp\((?<host>[^:]+)(:(?<port>\d+))?\)/(?<db>[^\?]+)"; // DSN 风格的连接地址
                    var match = Regex.Match(addr, pattern);
                    if (match.Success)
                    {
                        var user = match.Groups["user"].Value;
                        var pass = match.Groups["pass"].Value;
                        var host = match.Groups["host"].Value;
                        var port = match.Groups["port"].Success ? match.Groups["port"].Value : "3306";
                        var db = match.Groups["db"].Value;
                        return $"server={host};port={port};database={db};uid={user};pwd={pass};";
                    }
                }
                return addr;
            }

            lock (InitializeLock)
            {
                Sources.Clear();

                foreach (var kvp in preferences)
                {
                    if (!kvp.Key.StartsWith("XOrm/Source")) continue;
                    var parts = kvp.Key.Split('/');
                    if (parts.Length < 4) throw new Exception("XOrm.Initialize: invalid preference key ${kvp.Key}.");
                    var type = parts[2];
                    var alias = parts[3];
                    var source = new ConnectionConfig() { ConfigId = alias };
                    var types = Enum.GetValues(typeof(DbType)).Cast<DbType>();
                    if (!types.Any<DbType>(dbType => dbType.ToString().Contains(type, StringComparison.OrdinalIgnoreCase))) throw new Exception($"XOrm.Initialize: unknown database type '{type}' for alias '{alias}'.");
                    source.DbType = types.FirstOrDefault(dbType => dbType.ToString().Contains(type, StringComparison.OrdinalIgnoreCase));
                    var config = preferences.Get<XPrefs.IBase>(kvp.Key);
                    var addr = config.GetString(Preferences.Addr);
                    if (string.IsNullOrWhiteSpace(addr)) throw new Exception($"XOrm.Initialize: null or empty address for alias '{alias}'.");
                    addr = addr.Eval(XEnv.Instance);
                    source.ConnectionString = normalizeAddr(addr, source.DbType);
                    // 自动管理链接，避免空闲超时
                    // https://www.donet5.com/home/Doc?typeId=2362
                    // MySQL 可以配置超时设置进行测试
                    // docker exec -i mysql bash -c 'echo -e "[mysqld]\nwait_timeout=10\ninteractive_timeout=10" > /etc/mysql/conf.d/timeout.cnf'
                    // docker restart mysql
                    // docker exec -i mysql mysql -uroot -p123456 -e "SHOW GLOBAL VARIABLES LIKE 'wait_timeout';"
                    // docker exec -i mysql bash -c 'rm -f /etc/mysql/conf.d/timeout.cnf'
                    source.IsAutoCloseConnection = true;
                    Sources.Add(source);

                    XLog.Notice($"XOrm.Initialize: parsed source '{alias}' of database type '{source.DbType}'.");
                }
            }
        }
    }
    #endregion

    #region 上下文
    public partial class XOrm
    {
        /// <summary>
        /// Contexts 用于存储正在执行的上下文。
        /// </summary>
        internal static readonly ConcurrentDictionary<int, Context> Contexts = new();

        /// <summary>
        /// DeferringContexts 用于存储正在提交状态的上下文。
        /// </summary>
        internal static readonly ConcurrentDictionary<int, Context> DeferringContexts = new();

        /// <summary>
        /// Looms 是业务线程的 SqlSugarClient 客户端字典。
        /// </summary>
        internal static readonly ConcurrentDictionary<int, SqlSugarClient> Looms = new();

        /// <summary>
        /// OnLogExecuting 是 SqlSugarClient 的 Aop.OnLogExecuting 事件的回调。
        /// </summary>
        /// <param name="sql">SQL 语句</param>
        internal static void OnLogExecuting(string sql, SugarParameter[] _)
        {
            Contexts.TryGetValue(Environment.CurrentManagedThreadId, out var context);
            if (context == null) DeferringContexts.TryGetValue(Environment.CurrentManagedThreadId, out context);
            if (context == null) return;
            var cost = XPool.Object<Context.Cost>.Get();
            cost.Sql = sql;
            cost.Executing = XTime.GetMicrosecond();
            context.Costs.Add(cost);
        }

        /// <summary>
        /// OnLogExecuted 是 SqlSugarClient 的 Aop.OnLogExecuted 事件的回调。
        /// </summary>
        internal static void OnLogExecuted(string _, SugarParameter[] __)
        {
            Contexts.TryGetValue(Environment.CurrentManagedThreadId, out var context);
            if (context == null) DeferringContexts.TryGetValue(Environment.CurrentManagedThreadId, out context);
            if (context == null) return;
            if (context.Costs.Count > 0) context.Costs[^1].Executed = XTime.GetMicrosecond();
        }

        /// <summary>
        /// Watch 开始一个上下文。
        /// </summary>
        public static void Watch()
        {
            Contexts.GetOrAdd(Environment.CurrentManagedThreadId, _ =>
            {
                var context = XPool.Object<Context>.Get();
                context.Initial = XTime.GetMicrosecond();
                var loomID = XLoom.ID();
                if (loomID >= 0 && loomID < XLoom.Count)
                {
                    if (!Looms.TryGetValue(loomID, out var client))
                    {
                        lock (Looms)
                        {
                            if (!Looms.TryGetValue(loomID, out client))
                            {
                                client = new SqlSugarClient(Sources);
                                client.BeginTran();
                                client.Aop.OnLogExecuting = OnLogExecuting;
                                client.Aop.OnLogExecuted = OnLogExecuted;
                                Looms.TryAdd(loomID, client);
                            }
                        }
                    }
                    context.Client = client;
                }
                else
                {
                    context.Client = new SqlSugarClient(Sources);
                    context.Client.BeginTran();
                    context.Client.Aop.OnLogExecuting = OnLogExecuting;
                    context.Client.Aop.OnLogExecuted = OnLogExecuted;
                }
                XLog.Info("XOrm.Watch: context has been started.");
                return context;
            });
        }

        /// <summary>
        /// Defer 结束一个上下文。
        /// </summary>
        public static void Defer()
        {
            var microseconds = XTime.GetMicrosecond();
            Contexts.TryRemove(Environment.CurrentManagedThreadId, out var context);
            if (context == null) XLog.Error("XOrm.Defer: context not found.");
            else
            {
                DeferringContexts.TryAdd(Environment.CurrentManagedThreadId, context);  // 确保 Defer 操作的一致性
                try
                {
                    context.Client.CommitTran();

                    var crudLog = new StringBuilder();
                    var insertCount = 0;
                    long insertCost = 0;
                    var queryCount = 0;
                    long queryCost = 0;
                    var updateCount = 0;
                    long updateCost = 0;
                    var deleteCount = 0;
                    long deleteCost = 0;
                    foreach (var kvp in context.Costs)
                    {
                        if (kvp.Sql.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase)) { insertCount++; insertCost += kvp.Executed - kvp.Executing; }
                        if (kvp.Sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)) { queryCount++; queryCost += kvp.Executed - kvp.Executing; }
                        if (kvp.Sql.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase)) { updateCount++; updateCost += kvp.Executed - kvp.Executing; }
                        if (kvp.Sql.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase)) { deleteCount++; deleteCost += kvp.Executed - kvp.Executing; }
                    }
                    if (insertCount > 0) crudLog.AppendFormat("[Insert({0}):{1:F2}ms] ", insertCount, insertCost / 1e3);
                    if (queryCount > 0) crudLog.AppendFormat("[Query({0}):{1:F2}ms] ", queryCount, queryCost / 1e3);
                    if (updateCount > 0) crudLog.AppendFormat("[Update({0}):{1:F2}ms] ", updateCount, updateCost / 1e3);
                    if (deleteCount > 0) crudLog.AppendFormat("[Delete({0}):{1:F2}ms] ", deleteCount, deleteCost / 1e3);

                    var nowTime = XTime.GetMicrosecond();
                    var selfCost = nowTime - microseconds;
                    var totalCost = nowTime - context.Initial;
                    var otherCost = totalCost - selfCost - insertCost - queryCost - updateCost - deleteCost;
                    XLog.Info("XOrm.Defer: context has been deferred, total cost {0:F2}ms for {1}[Self:{2:F2}ms] [Other:{3:F2}ms].", totalCost / 1e3, crudLog.ToString(), selfCost / 1e3, otherCost / 1e3);
                }
                catch { throw; }
                finally
                {
                    var loomID = XLoom.ID();
                    if (loomID == -1 || loomID >= XLoom.Count) context.Client.Close();
                    context.Reset(); DeferringContexts.TryRemove(Environment.CurrentManagedThreadId, out var _); XPool.Object<Context>.Put(context);
                }
            }
        }

        /// <summary>
        /// Current 获取当前上下文中的 SqlSugarClient 客户端。
        /// </summary>
        public static SqlSugarClient Current
        {
            get
            {
                Contexts.TryGetValue(Environment.CurrentManagedThreadId, out var context);
                if (context != null) return context.Client;
                else return new SqlSugarClient(Sources);
            }
        }

        /// <summary>
        /// Insert 插入数据。
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="columnDictionary">字段字典</param>
        /// <param name="onInsert">插入操作</param>
        /// <returns>影响行数</returns>
        public static int Insert<T>(Dictionary<string, object> columnDictionary, Action<IInsertable<T>> onInsert = null) where T : class, new()
        {
            var insertable = Current.Insertable<T>(columnDictionary);
            onInsert?.Invoke(insertable);
            return insertable.ExecuteCommand();
        }

        /// <summary>
        /// Insert 插入数据。
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="columnDictionary">字段字典</param>
        /// <param name="snowflakeId">唯一标识</param>
        /// <param name="onInsert">插入操作</param>
        public static void Insert<T>(Dictionary<string, object> columnDictionary, out long snowflakeId, Action<IInsertable<T>> onInsert = null) where T : class, new()
        {
            var insertable = Current.Insertable<T>(columnDictionary);
            onInsert?.Invoke(insertable);
            snowflakeId = insertable.ExecuteReturnSnowflakeId();
        }

        /// <summary>
        /// Insert 插入数据。
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="insertDynamicObject">动态对象</param>
        /// <param name="onInsert">插入操作</param>
        /// <returns>影响行数</returns>
        public static int Insert<T>(dynamic insertDynamicObject, Action<IInsertable<T>> onInsert = null) where T : class, new()
        {
            var insertable = Current.Insertable<T>(insertDynamicObject);
            onInsert?.Invoke(insertable);
            return insertable.ExecuteCommand();
        }

        /// <summary>
        /// Insert 插入数据。
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="insertDynamicObject">动态对象</param>
        /// <param name="snowflakeId">唯一标识</param>
        /// <param name="onInsert">插入操作</param>
        public static void Insert<T>(dynamic insertDynamicObject, out long snowflakeId, Action<IInsertable<T>> onInsert = null) where T : class, new()
        {
            var insertable = Current.Insertable<T>(insertDynamicObject);
            onInsert?.Invoke(insertable);
            snowflakeId = insertable.ExecuteReturnSnowflakeId();
        }

        /// <summary>
        /// Insert 插入数据。
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="insertObj">实体对象</param>
        /// <param name="onInsert">插入操作</param>
        /// <returns>影响行数</returns>
        public static int Insert<T>(T insertObj, Action<IInsertable<T>> onInsert = null) where T : class, new()
        {
            var insertable = Current.Insertable<T>(insertObj);
            onInsert?.Invoke(insertable);
            return insertable.ExecuteCommand();
        }

        /// <summary>
        /// Insert 插入数据。
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="insertObj">实体对象</param>
        /// <param name="snowflakeId">唯一标识</param>
        /// <param name="onInsert">插入操作</param>
        public static void Insert<T>(T insertObj, out long snowflakeId, Action<IInsertable<T>> onInsert = null) where T : class, new()
        {
            var insertable = Current.Insertable<T>(insertObj);
            onInsert?.Invoke(insertable);
            snowflakeId = insertable.ExecuteReturnSnowflakeId();
        }

        /// <summary>
        /// Insert 插入数据。
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="insertObjs">对象列表</param>
        /// <param name="onInsert">插入操作</param>
        /// <returns>影响行数</returns>
        public static int Insert<T>(List<T> insertObjs, Action<IInsertable<T>> onInsert = null) where T : class, new()
        {
            var insertable = Current.Insertable<T>(insertObjs);
            onInsert?.Invoke(insertable);
            return insertable.ExecuteCommand();
        }

        /// <summary>
        /// Insert 插入数据。
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="insertObjs">对象列表</param>
        /// <param name="snowflakeIds">标识列表</param>
        /// <param name="onInsert">插入操作</param>
        public static void Insert<T>(List<T> insertObjs, out List<long> snowflakeIds, Action<IInsertable<T>> onInsert = null) where T : class, new()
        {
            var insertable = Current.Insertable<T>(insertObjs);
            onInsert?.Invoke(insertable);
            snowflakeIds = insertable.ExecuteReturnSnowflakeIdList();
        }

        /// <summary>
        /// Insert 插入数据。
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="insertObjs">对象数组</param>
        /// <param name="onInsert">插入操作</param>
        /// <returns>影响行数</returns>
        public static int Insert<T>(T[] insertObjs, Action<IInsertable<T>> onInsert = null) where T : class, new()
        {
            var insertable = Current.Insertable<T>(insertObjs);
            onInsert?.Invoke(insertable);
            return insertable.ExecuteCommand();
        }

        /// <summary>
        /// Insert 插入数据。
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="insertObjs">对象数组</param>
        /// <param name="snowflakeIds">标识列表</param>
        /// <param name="onInsert">插入操作</param>
        public static void Insert<T>(T[] insertObjs, out List<long> snowflakeIds, Action<IInsertable<T>> onInsert = null) where T : class, new()
        {
            var insertable = Current.Insertable<T>(insertObjs);
            onInsert?.Invoke(insertable);
            snowflakeIds = insertable.ExecuteReturnSnowflakeIdList();
        }

        /// <summary>
        /// Query 查询数据。
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="onQuery">查询操作</param>
        /// <returns>查询结果</returns>
        public static ISugarQueryable<T> Query<T>(Action<ISugarQueryable<T>> onQuery = null) where T : class, new()
        {
            var queryable = Current.Queryable<T>();
            onQuery?.Invoke(queryable);
            return queryable;
        }

        /// <summary>
        /// Query 查询数据。
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="queryable">查询对象</param>
        /// <param name="onQuery">查询操作</param>
        /// <returns>查询结果</returns>
        public static ISugarQueryable<T> Query<T>(ISugarQueryable<T> queryable, Action<ISugarQueryable<T>> onQuery = null) where T : class, new()
        {
            queryable = Current.Queryable<T>(queryable);
            onQuery?.Invoke(queryable);
            return queryable;
        }

        /// <summary>
        /// Query 查询数据。
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="queryable">查询对象</param>
        /// <param name="shortName">简短名称</param>
        /// <param name="onQuery">查询操作</param>
        /// <returns>查询结果</returns>
        public static ISugarQueryable<T> Query<T>(ISugarQueryable<T> queryable, string shortName, Action<ISugarQueryable<T>> onQuery = null) where T : class, new()
        {
            queryable = Current.Queryable<T>(queryable, shortName);
            onQuery?.Invoke(queryable);
            return queryable;
        }

        /// <summary>
        /// Update 更新数据。
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="onUpdate">更新操作</param>
        /// <returns>影响行数</returns>
        public static int Update<T>(Action<IUpdateable<T>> onUpdate = null) where T : class, new()
        {
            var updateable = Current.Updateable<T>();
            onUpdate?.Invoke(updateable);
            return updateable.ExecuteCommand();
        }

        /// <summary>
        /// Update 更新数据。
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="columnDictionary">字段字典</param>
        /// <param name="onUpdate">更新操作</param>
        /// <returns>影响行数</returns>
        public static int Update<T>(Dictionary<string, object> columnDictionary, Action<IUpdateable<T>> onUpdate = null) where T : class, new()
        {
            var updateable = Current.Updateable<T>(columnDictionary);
            onUpdate?.Invoke(updateable);
            return updateable.ExecuteCommand();
        }

        /// <summary>
        /// Update 更新数据。
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="updateDynamicObject">动态对象</param>
        /// <param name="onUpdate">更新操作</param>
        /// <returns>影响行数</returns>
        public static int Update<T>(dynamic updateDynamicObject, Action<IUpdateable<T>> onUpdate = null) where T : class, new()
        {
            var updateable = Current.Updateable<T>(updateDynamicObject);
            onUpdate?.Invoke(updateable);
            return updateable.ExecuteCommand();
        }

        /// <summary>
        /// Update 更新数据。
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="columns">列表达式</param>
        /// <param name="onUpdate">更新操作</param>
        /// <returns>影响行数</returns>
        public static int Update<T>(Expression<Func<T, bool>> columns, Action<IUpdateable<T>> onUpdate = null) where T : class, new()
        {
            var updateable = Current.Updateable<T>(columns);
            onUpdate?.Invoke(updateable);
            return updateable.ExecuteCommand();
        }

        /// <summary>
        /// Update 更新数据。
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="columns">列表达式</param>
        /// <param name="onUpdate">更新操作</param>
        /// <returns>影响行数</returns>
        public static int Update<T>(Expression<Func<T, T>> columns, Action<IUpdateable<T>> onUpdate = null) where T : class, new()
        {
            var updateable = Current.Updateable<T>(columns);
            onUpdate?.Invoke(updateable);
            return updateable.ExecuteCommand();
        }

        /// <summary>
        /// Update 更新数据。
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="updateObjs">对象列表</param>
        /// <param name="onUpdate">更新操作</param>
        /// <returns>影响行数</returns>
        public static int Update<T>(List<T> updateObjs, Action<IUpdateable<T>> onUpdate = null) where T : class, new()
        {
            var updateable = Current.Updateable<T>(updateObjs);
            onUpdate?.Invoke(updateable);
            return updateable.ExecuteCommand();
        }

        /// <summary>
        /// Update 更新数据。
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="updateObj">实体对象</param>
        /// <param name="onUpdate">更新操作</param>
        /// <returns>影响行数</returns>
        public static int Update<T>(T updateObj, Action<IUpdateable<T>> onUpdate = null) where T : class, new()
        {
            var updateable = Current.Updateable<T>(updateObj);
            onUpdate?.Invoke(updateable);
            return updateable.ExecuteCommand();
        }

        /// <summary>
        /// Update 更新数据。
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="updateObjs">对象数组</param>
        /// <param name="onUpdate">更新操作</param>
        /// <returns>影响行数</returns>
        public static int Update<T>(T[] updateObjs, Action<IUpdateable<T>> onUpdate = null) where T : class, new()
        {
            var updateable = Current.Updateable<T>(updateObjs);
            onUpdate?.Invoke(updateable);
            return updateable.ExecuteCommand();
        }

        /// <summary>
        /// Delete 删除数据。
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="onDelete">删除操作</param>
        /// <returns>影响行数</returns>
        public static int Delete<T>(Action<IDeleteable<T>> onDelete = null) where T : class, new()
        {
            var deleteable = Current.Deleteable<T>();
            onDelete?.Invoke(deleteable);
            return deleteable.ExecuteCommand();
        }

        /// <summary>
        /// Delete 删除数据。
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="primaryKeyValue">主键数据</param>
        /// <param name="onDelete">删除操作</param>
        /// <returns>影响行数</returns>
        public static int Delete<T>(dynamic primaryKeyValue, Action<IDeleteable<T>> onDelete = null) where T : class, new()
        {
            var deleteable = Current.Deleteable<T>(primaryKeyValue);
            onDelete?.Invoke(deleteable);
            return deleteable.ExecuteCommand();
        }

        /// <summary>
        /// Delete 删除数据。
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="primaryKeyValues">主键数据</param>
        /// <param name="onDelete">删除操作</param>
        /// <returns>影响行数</returns>
        public static int Delete<T>(dynamic[] primaryKeyValues, Action<IDeleteable<T>> onDelete = null) where T : class, new()
        {
            var deleteable = Current.Deleteable<T>(primaryKeyValues);
            onDelete?.Invoke(deleteable);
            return deleteable.ExecuteCommand();
        }

        /// <summary>
        /// Delete 删除数据。
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="expression">条件表达</param>
        /// <param name="onDelete">删除操作</param>
        /// <returns>影响行数</returns>
        public static int Delete<T>(Expression<Func<T, bool>> expression, Action<IDeleteable<T>> onDelete = null) where T : class, new()
        {
            var deleteable = Current.Deleteable<T>(expression);
            onDelete?.Invoke(deleteable);
            return deleteable.ExecuteCommand();
        }

        /// <summary>
        /// Delete 删除数据。
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="pkValue">主键数据</param>
        /// <param name="onDelete">删除操作</param>
        /// <returns>影响行数</returns>
        public static int Delete<T>(List<dynamic> pkValue, Action<IDeleteable<T>> onDelete = null) where T : class, new()
        {
            var deleteable = Current.Deleteable<T>(pkValue);
            onDelete?.Invoke(deleteable);
            return deleteable.ExecuteCommand();
        }

        /// <summary>
        /// Delete 删除数据。
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="deleteObjs">对象列表</param>
        /// <param name="onDelete">删除操作</param>
        /// <returns>影响行数</returns>
        public static int Delete<T>(List<T> deleteObjs, Action<IDeleteable<T>> onDelete = null) where T : class, new()
        {
            var deleteable = Current.Deleteable<T>(deleteObjs);
            onDelete?.Invoke(deleteable);
            return deleteable.ExecuteCommand();
        }

        /// <summary>
        /// Delete 删除数据。
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="deleteObj">实体对象</param>
        /// <param name="onDelete">删除操作</param>
        /// <returns>影响行数</returns>
        public static int Delete<T>(T deleteObj, Action<IDeleteable<T>> onDelete = null) where T : class, new()
        {
            var deleteable = Current.Deleteable<T>(deleteObj);
            onDelete?.Invoke(deleteable);
            return deleteable.ExecuteCommand();
        }
    }
    #endregion
}