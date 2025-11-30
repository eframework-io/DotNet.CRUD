// Copyright (c) 2025 EFramework Innovation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.Linq;
using EFramework.DotNet.CRUD;
using EFramework.DotNet.Utility;
using NUnit.Framework;
using SqlSugar;

/// <summary>
/// TestXOrm 是 XOrm 的单元测试。
/// </summary>
public class TestXOrm
{
    [SqlSugar.Tenant("myalias")]
    [SugarTable("my_model")]
    public class MyModel
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnName = "id")]
        public int ID { get; set; }

        [SugarColumn(ColumnName = "int_val", IsNullable = true)]
        public int? IntVal { get; set; }

        [SugarColumn(ColumnName = "float_val", IsNullable = true)]
        public float? FloatVal { get; set; }

        [SugarColumn(ColumnName = "string_val", IsNullable = true)]
        public string StringVal { get; set; }

        [SugarColumn(ColumnName = "bool_val", IsNullable = true)]
        public bool? BoolVal { get; set; }
    }

    [SetUp]
    public void Setup()
    {
        var preferences = new XPrefs.IBase();
        preferences.Set(XOrm.Preferences.Snowflake, 10000);
        var testConfig = new XPrefs.IBase();
        testConfig.Set(XOrm.Preferences.Addr, "server=localhost;port=3306;database=mysql;uid=root;pwd=123456;");
        preferences.Set($"{XOrm.Preferences.Source}/MySQL/myalias", testConfig);
        XOrm.Initialize(preferences);
        Assert.That(SnowFlakeSingle.WorkId, Is.EqualTo(10000));
        var client = new SqlSugarClient(XOrm.Sources);
        try { client.DbMaintenance.TruncateTable("my_model"); } catch { }
        client.CodeFirst.InitTables<MyModel>();

        XOrm.Watch();
        for (var i = 0; i < 1000; i++)
        {
            var model = new MyModel()
            {
                IntVal = i + 1,
                FloatVal = i + 1f,
                StringVal = $"model{i + 1}",
                BoolVal = i % 2 == 0
            };
            Assert.That(XOrm.Insert<MyModel>(model), Is.EqualTo(1), "数据插入应当成功。");
        }
    }

    [TearDown]
    public void Reset()
    {
        XOrm.Defer();
        var client = new SqlSugarClient(XOrm.Sources);
        try { client.DbMaintenance.DropTable("my_model"); } catch { }
    }

    [Test]
    public void Initialize()
    {
        #region Single
        {
            var preferences = new XPrefs.IBase();
            var config = new XPrefs.IBase();
            config.Set(XOrm.Preferences.Addr, "server=localhost;port=3306;database=mysql;uid=root;pwd=123456;");
            preferences.Set($"{XOrm.Preferences.Source}/MySQL/myalias", config);
            XOrm.Initialize(preferences);
            Assert.That(XOrm.Sources, Has.Count.EqualTo(1));
            var first = XOrm.Sources.First();
            Assert.That(first.ConfigId, Is.EqualTo("myalias"));
            Assert.That(first.DbType, Is.EqualTo(DbType.MySql));
            Assert.That(first.ConnectionString, Is.EqualTo("server=localhost;port=3306;database=mysql;uid=root;pwd=123456;"));
        }
        #endregion

        #region Multiple
        {
            var preferences = new XPrefs.IBase();
            var config1 = new XPrefs.IBase();
            config1.Set(XOrm.Preferences.Addr, "server=localhost;port=3306;database=mysql;uid=root;pwd=123456;");
            preferences.Set($"{XOrm.Preferences.Source}/MySQL/myalias1", config1);
            var config2 = new XPrefs.IBase();
            config2.Set(XOrm.Preferences.Addr, "server=localhost;port=3306;database=mysql;uid=root;pwd=123456;");
            preferences.Set($"{XOrm.Preferences.Source}/MySQL/myalias2", config2);
            XOrm.Initialize(preferences);
            Assert.That(XOrm.Sources, Has.Count.EqualTo(2));
            var first = XOrm.Sources.First();
            Assert.That(first.ConfigId, Is.EqualTo("myalias1"));
            Assert.That(first.DbType, Is.EqualTo(DbType.MySql));
            Assert.That(first.ConnectionString, Is.EqualTo("server=localhost;port=3306;database=mysql;uid=root;pwd=123456;"));
            var last = XOrm.Sources.Last();
            Assert.That(last.ConfigId, Is.EqualTo("myalias2"));
            Assert.That(last.DbType, Is.EqualTo(DbType.MySql));
            Assert.That(last.ConnectionString, Is.EqualTo("server=localhost;port=3306;database=mysql;uid=root;pwd=123456;"));
        }
        #endregion

        #region DSN
        {
            var preferences = new XPrefs.IBase();
            var config = new XPrefs.IBase();
            config.Set(XOrm.Preferences.Addr, "root:123456@tcp(127.0.0.1:3306)/mysql?charset=utf8mb4&loc=Local");
            preferences.Set($"{XOrm.Preferences.Source}/MySQL/myalias", config);
            XOrm.Initialize(preferences);
            Assert.That(XOrm.Sources, Has.Count.EqualTo(1));
            var first = XOrm.Sources.First();
            Assert.That(first.ConfigId, Is.EqualTo("myalias"));
            Assert.That(first.DbType, Is.EqualTo(DbType.MySql));
            Assert.That(first.ConnectionString, Is.EqualTo("server=127.0.0.1;port=3306;database=mysql;uid=root;pwd=123456;"));
        }
        #endregion
    }

    [Test]
    public void Insert()
    {
        XOrm.Contexts.TryGetValue(Environment.CurrentManagedThreadId, out var context);
        Assert.That(context, Is.Not.Null, "上下文应当存在。");
        Assert.That(context.Costs.Count(e => e.Sql.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase)), Is.EqualTo(1000), "插入次数应当为1000。");
    }

    [Test]
    public void Query()
    {
        #region 条件查询
        {
            var model1 = XOrm.Query<MyModel>(queryable => queryable.Where(e => e.ID == 1)).Where(e => e.StringVal == "model1").First();
            Assert.That(model1, Is.Not.Null, "条件查询结果不应为空。");

            var model2 = XOrm.Query<MyModel>().Where(e => e.FloatVal == -1).First();
            Assert.That(model2, Is.Null, "条件查询结果应当为空。");
        }
        #endregion

        #region 全量查询
        {
            var models = XOrm.Query<MyModel>().ToList();
            Assert.That(models, Has.Count.EqualTo(1000), "查询应当返回1000条数据。");
            var first = models.First();
            var last = models.Last();
            Assert.That(first.ID, Is.EqualTo(1), "查询的数据字段应当和插入的数据字段一致。");
            Assert.That(last.ID, Is.EqualTo(1000), "查询的数据字段应当和插入的数据字段一致。");
            Assert.That(first.IntVal, Is.EqualTo(1), "查询的数据字段应当和插入的数据字段一致。");
            Assert.That(last.IntVal, Is.EqualTo(1000), "查询的数据字段应当和插入的数据字段一致。");
            Assert.That(first.FloatVal, Is.EqualTo(1.0f), "查询的数据字段应当和插入的数据字段一致。");
            Assert.That(last.FloatVal, Is.EqualTo(1000.0f), "查询的数据字段应当和插入的数据字段一致。");
            Assert.That(first.StringVal, Is.EqualTo("model1"), "查询的数据字段应当和插入的数据字段一致。");
            Assert.That(last.StringVal, Is.EqualTo("model1000"), "查询的数据字段应当和插入的数据字段一致。");
            Assert.That(first.BoolVal, Is.EqualTo(true), "查询的数据字段应当和插入的数据字段一致。");
            Assert.That(last.BoolVal, Is.EqualTo(false), "查询的数据字段应当和插入的数据字段一致。");
        }
        #endregion

        XOrm.Contexts.TryGetValue(Environment.CurrentManagedThreadId, out var context);
        Assert.That(context, Is.Not.Null, "上下文应当存在。");
        Assert.That(context.Costs.Count(e => e.Sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)), Is.EqualTo(3), "查询次数应当为3。");
    }

    [Test]
    public void Update()
    {
        #region 条件更新
        {
            var model1 = XOrm.Query<MyModel>().Where(e => e.ID == 1).First();
            model1.IntVal = -1;
            model1.FloatVal = -1.0f;
            var affect = XOrm.Update<MyModel>(model1, updateable => updateable.UpdateColumns(e => new { e.IntVal, e.FloatVal }));
            Assert.That(affect, Is.EqualTo(1), "更新应当成功。");

            affect = XOrm.Update<MyModel>(onUpdate => onUpdate.SetColumns(it => it.IntVal == -2).Where(it => it.ID == model1.ID));
            Assert.That(affect, Is.EqualTo(1), "更新应当成功。");

            var model1Updated = XOrm.Query<MyModel>().Where(e => e.ID == model1.ID).First();
            Assert.That(model1Updated.ID, Is.EqualTo(model1.ID), "更新后的数据ID应当不变。");
            Assert.That(model1Updated.IntVal, Is.EqualTo(-2), "更新后的数据IntVal应当为更新后的值。");
            Assert.That(model1Updated.FloatVal, Is.EqualTo(-1.0f), "更新后的数据FloatVal应当为更新后的值。");
            Assert.That(model1Updated.StringVal, Is.EqualTo(model1.StringVal), "更新后的数据StringVal应当未被更新。");
            Assert.That(model1Updated.BoolVal, Is.EqualTo(model1.BoolVal), "更新后的数据BoolVal应当未被更新。");
        }
        #endregion

        #region 全量更新
        {
            var model2 = new MyModel() { ID = 2, FloatVal = -1.0f };
            var affect = XOrm.Update<MyModel>(model2);
            Assert.That(affect, Is.EqualTo(1), "更新应当成功。");

            var model2Updated = XOrm.Query<MyModel>().Where(e => e.ID == model2.ID).First();
            Assert.That(model2Updated.ID, Is.EqualTo(model2.ID), "更新后的数据ID应当不变。");
            Assert.That(model2Updated.IntVal, Is.Null, "更新后的数据IntVal应当为初始值。");
            Assert.That(model2Updated.FloatVal, Is.EqualTo(-1.0f), "更新后的数据FloatVal应当为更新后的值。");
            Assert.That(model2Updated.StringVal, Is.Null, "更新后的数据StringVal应当为初始值。");
            Assert.That(model2Updated.BoolVal, Is.Null, "更新后的数据BoolVal应当为初始值。");
        }
        #endregion

        XOrm.Contexts.TryGetValue(Environment.CurrentManagedThreadId, out var context);
        Assert.That(context, Is.Not.Null, "上下文应当存在。");
        Assert.That(context.Costs.Count(e => e.Sql.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase)), Is.EqualTo(3), "更新次数应当为3。");
    }

    [Test]
    public void Delete()
    {
        #region 实例删除
        {
            var model1 = XOrm.Query<MyModel>().Where(e => e.ID == 1).First();
            var affect = XOrm.Delete<MyModel>(model1);
            Assert.That(affect, Is.EqualTo(1), "删除应当成功。");
        }
        #endregion

        #region 模版删除
        {
            var affect = XOrm.Delete<MyModel>(onDelete: deleteable => deleteable.Where(e => e.ID == 2));
            Assert.That(affect, Is.EqualTo(1), "删除应当成功。");
        }
        #endregion

        #region 主键删除
        {
            var affect = XOrm.Delete<MyModel>(3);
            Assert.That(affect, Is.EqualTo(1), "删除应当成功。");
        }
        #endregion

        #region 全量删除
        {
            var affect = XOrm.Delete<MyModel>();
            Assert.That(affect, Is.EqualTo(997), "删除应当成功。");
            Assert.That(XOrm.Query<MyModel>().ToList(), Has.Count.EqualTo(0), "删除应当成功。");
        }
        #endregion

        XOrm.Contexts.TryGetValue(Environment.CurrentManagedThreadId, out var context);
        Assert.That(context, Is.Not.Null, "上下文应当存在。");
        Assert.That(context.Costs.Count(e => e.Sql.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase)), Is.EqualTo(4), "删除次数应当为4。");
    }
}